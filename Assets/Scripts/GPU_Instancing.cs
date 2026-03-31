using UnityEngine;
using System.Collections.Generic;

public class GPU_Instancing : MonoBehaviour
{
    [Header("Tree Settings")]
    [Tooltip("The mesh to render for each tree")]
    public Mesh treeMesh;
    
    [Tooltip("The material to use (must have GPU Instancing enabled)")]
    public Material treeMaterial;
    
    [Header("Generation Settings")]
    [Tooltip("Number of trees to generate")]
    public int treeCount = 1000;
    
    [Tooltip("Area to spread trees across")]
    public Vector2 spawnArea = new Vector2(100f, 100f);
    
    [Tooltip("Minimum and maximum scale for trees")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.5f);
    
    [Tooltip("Random rotation on Y axis")]
    public bool randomRotation = true;
    
    [Tooltip("Adjust height to terrain")]
    public bool alignToTerrain = true;
    
    [Tooltip("Terrain reference (if using terrain alignment)")]
    public Terrain terrain;
    
    [Header("Performance")]
    [Tooltip("Maximum instances per draw call (max 1023)")]
    public int batchSize = 1023;
    
    [Tooltip("Enable frustum culling")]
    public bool enableCulling = true;
    
    [Tooltip("Camera for culling (uses main camera if null)")]
    public Camera cullingCamera;
    
    private List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
    private Bounds[] instanceBounds;
    private MaterialPropertyBlock propertyBlock;
    
    void Start()
    {
        if (treeMesh == null || treeMaterial == null)
        {
            Debug.LogError("GPU_Instancing: Tree mesh and material must be assigned!");
            return;
        }
        
        if (!treeMaterial.enableInstancing)
        {
            Debug.LogWarning("GPU_Instancing: Material does not have GPU Instancing enabled. Enabling it now.");
            treeMaterial.enableInstancing = true;
        }
        
        if (cullingCamera == null)
        {
            cullingCamera = Camera.main;
        }
        
        propertyBlock = new MaterialPropertyBlock();
        GenerateTrees();
    }
    
    void GenerateTrees()
    {
        List<Matrix4x4> allMatrices = new List<Matrix4x4>();
        List<Bounds> allBounds = new List<Bounds>();
        
        for (int i = 0; i < treeCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
                0,
                Random.Range(-spawnArea.y / 2, spawnArea.y / 2)
            );
            
            // Adjust to terrain height if enabled
            if (alignToTerrain && terrain != null)
            {
                position.y = terrain.SampleHeight(position + transform.position);
            }
            
            // Add parent object position
            position += transform.position;
            
            // Random rotation
            Quaternion rotation = randomRotation ? 
                Quaternion.Euler(0, Random.Range(0f, 360f), 0) : 
                Quaternion.identity;
            
            // Random scale
            float scale = Random.Range(scaleRange.x, scaleRange.y);
            Vector3 scaleVector = new Vector3(scale, scale, scale);
            
            // Create transformation matrix
            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scaleVector);
            allMatrices.Add(matrix);
            
            // Create bounds for culling
            Bounds bounds = new Bounds(position, treeMesh.bounds.size * scale);
            allBounds.Add(bounds);
        }
        
        // Split into batches
        instanceBounds = allBounds.ToArray();
        int batchCount = Mathf.CeilToInt((float)allMatrices.Count / batchSize);
        
        for (int i = 0; i < batchCount; i++)
        {
            int start = i * batchSize;
            int count = Mathf.Min(batchSize, allMatrices.Count - start);
            Matrix4x4[] batch = new Matrix4x4[count];
            
            for (int j = 0; j < count; j++)
            {
                batch[j] = allMatrices[start + j];
            }
            
            batches.Add(batch);
        }
        
        Debug.Log($"GPU_Instancing: Generated {treeCount} trees in {batchCount} batches");
    }
    
    void Update()
    {
        if (treeMesh == null || treeMaterial == null)
            return;
        
        // Render all batches
        int instanceIndex = 0;
        foreach (Matrix4x4[] batch in batches)
        {
            if (enableCulling && cullingCamera != null)
            {
                // Simple frustum culling per batch
                List<Matrix4x4> visibleInstances = new List<Matrix4x4>();
                
                for (int i = 0; i < batch.Length; i++)
                {
                    if (instanceIndex < instanceBounds.Length)
                    {
                        if (GeometryUtility.TestPlanesAABB(
                            GeometryUtility.CalculateFrustumPlanes(cullingCamera),
                            instanceBounds[instanceIndex]))
                        {
                            visibleInstances.Add(batch[i]);
                        }
                    }
                    instanceIndex++;
                }
                
                if (visibleInstances.Count > 0)
                {
                    Graphics.DrawMeshInstanced(
                        treeMesh,
                        0,
                        treeMaterial,
                        visibleInstances.ToArray()
                    );
                }
            }
            else
            {
                // Render without culling
                Graphics.DrawMeshInstanced(
                    treeMesh,
                    0,
                    treeMaterial,
                    batch
                );
            }
        }
    }
    
    // Public method to regenerate trees at runtime
    public void RegenerateTrees()
    {
        batches.Clear();
        GenerateTrees();
    }
    
    // Visualize spawn area in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnArea.x, 0.1f, spawnArea.y));
    }
}
