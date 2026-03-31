#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

public static class TerrainToLowResMesh
{
    [MenuItem("Tools/Terrain/Convert Selected Terrain To Low-Res Mesh")]
    public static void ConvertSelectedTerrain()
    {
        var terrain = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Terrain>() : null;
        if (!terrain)
        {
            EditorUtility.DisplayDialog("Terrain → Mesh", "Select a GameObject with a Terrain component.", "OK");
            return;
        }

        TerrainData td = terrain.terrainData;
        if (!td)
        {
            EditorUtility.DisplayDialog("Terrain → Mesh", "Selected Terrain has no TerrainData.", "OK");
            return;
        }

        // Adjust this: higher = lower poly (e.g. 4, 8, 16)
        int step = 32;

        int hmRes = td.heightmapResolution;           // includes +1 border
        int samples = hmRes;                          // heights array is [hmRes, hmRes]
        float[,] heights = td.GetHeights(0, 0, samples, samples);

        int vertsX = ((hmRes - 1) / step) + 1;
        int vertsZ = ((hmRes - 1) / step) + 1;

        Vector3 size = td.size;

        var verts = new Vector3[vertsX * vertsZ];
        var uvs = new Vector2[vertsX * vertsZ];

        for (int z = 0; z < vertsZ; z++)
        {
            for (int x = 0; x < vertsX; x++)
            {
                int hx = Mathf.Min(x * step, hmRes - 1);
                int hz = Mathf.Min(z * step, hmRes - 1);

                float nx = hx / (float)(hmRes - 1);
                float nz = hz / (float)(hmRes - 1);

                float h = heights[hz, hx]; // note: [y,x] in height array
                int i = z * vertsX + x;

                verts[i] = new Vector3(nx * size.x, h * size.y, nz * size.z);
                uvs[i] = new Vector2(nx, nz);
            }
        }

        int quadsX = vertsX - 1;
        int quadsZ = vertsZ - 1;
        int[] tris = new int[quadsX * quadsZ * 6];

        int t = 0;
        for (int z = 0; z < quadsZ; z++)
        {
            for (int x = 0; x < quadsX; x++)
            {
                int i0 = z * vertsX + x;
                int i1 = i0 + 1;
                int i2 = i0 + vertsX;
                int i3 = i2 + 1;

                // two triangles (i0, i2, i1) and (i1, i2, i3)
                tris[t++] = i0;
                tris[t++] = i2;
                tris[t++] = i1;

                tris[t++] = i1;
                tris[t++] = i2;
                tris[t++] = i3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = (verts.Length > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.name = terrain.name + "_LowResMesh";
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string folder = "Assets/TerrainMeshExports";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "TerrainMeshExports");

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, mesh.name + ".asset"));
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();

        // Create a mesh object next to the terrain
        GameObject go = new GameObject(mesh.name);
        go.transform.position = terrain.transform.position;
        go.transform.rotation = terrain.transform.rotation;
        go.transform.localScale = terrain.transform.localScale;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")); // or Standard if built-in

        // Visual-only suggestions:
        // - no collider
        // - mark static
        go.isStatic = true;

        EditorUtility.DisplayDialog("Terrain → Mesh", $"Created mesh asset:\n{assetPath}\n\nNew GameObject: {go.name}", "OK");
        Selection.activeGameObject = go;
    }
}
#endif
