#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class TerrainToMeshExporter : EditorWindow
{
    private Terrain terrain;
    private int resolution = 256; // vertices per side
    private bool addMeshCollider = true;

    [MenuItem("Tools/Terrain/Export Terrain To Mesh")]
    public static void ShowWindow()
    {
        GetWindow<TerrainToMeshExporter>("Terrain To Mesh");
    }

    private void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 32, 1024);
        addMeshCollider = EditorGUILayout.Toggle("Add MeshCollider", addMeshCollider);

        EditorGUILayout.Space();

        if (GUILayout.Button("Export"))
        {
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("Missing Terrain", "Assign a Terrain first.", "OK");
                return;
            }

            Export();
        }
    }

    private void Export()
    {
        TerrainData td = terrain.terrainData;
        int res = Mathf.Max(2, resolution);

        float width = td.size.x;
        float length = td.size.z;
        float height = td.size.y;

        Vector3[] verts = new Vector3[res * res];
        Vector2[] uvs = new Vector2[res * res];
        int[] tris = new int[(res - 1) * (res - 1) * 6];

        // Sample heights using normalized coords
        int t = 0;
        for (int z = 0; z < res; z++)
        {
            float v = z / (float)(res - 1);
            for (int x = 0; x < res; x++)
            {
                float u = x / (float)(res - 1);

                float y01 = td.GetInterpolatedHeight(u, v) / height; // normalize
                float y = y01 * height;

                int i = z * res + x;
                verts[i] = new Vector3(u * width, y, v * length);
                uvs[i] = new Vector2(u, v);

                if (x < res - 1 && z < res - 1)
                {
                    int i0 = i;
                    int i1 = i + 1;
                    int i2 = i + res;
                    int i3 = i + res + 1;

                    // two triangles per quad
                    tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                    tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{terrain.name}_Mesh_{res}";
        mesh.indexFormat = (verts.Length > 65535)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Save mesh asset
        string folder = "Assets/TerrainMeshExports";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "TerrainMeshExports");

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mesh.name}.asset");
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Create a new GameObject using the mesh
        GameObject go = new GameObject(mesh.name);
        go.transform.position = terrain.transform.position;
        go.transform.rotation = terrain.transform.rotation;
        go.transform.localScale = Vector3.one;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        // You will assign your snow material here manually

        if (addMeshCollider)
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }

        Selection.activeGameObject = go;

        Debug.Log($"Terrain exported to mesh: {path}");
    }
}
#endif
