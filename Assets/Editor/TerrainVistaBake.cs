#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class TerrainVistaBake
{
    [MenuItem("Tools/Terrain/Bake Vista Texture (Top-Down)")]
    public static void BakeVistaTexture()
    {
        var go = Selection.activeGameObject;
        var terrain = go != null ? go.GetComponent<Terrain>() : null;
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("Vista Bake", "Select a GameObject with a Terrain component.", "OK");
            return;
        }

        // --- Settings (tweak these) ---
        int resolution = 4096; // 2048 ok, 4096 nicer, 8192 if you really need it
        float padding = 2f;    // extra world units around edges
        bool includeTrees = true; // if you want trees baked into the texture
        // ------------------------------

        // Make a temporary camera
        var camGO = new GameObject("~VistaBakeCam");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        // Culling: bake terrain + (optionally) trees
        // Terrain trees are drawn by the Terrain renderer, so "includeTrees" mainly matters if you have separate tree objects.
        cam.cullingMask = includeTrees ? ~0 : (1 << LayerMask.NameToLayer("Default"));

        // Position camera above terrain, looking down
        var td = terrain.terrainData;
        Vector3 tPos = terrain.transform.position;
        Vector3 size = td.size;

        Vector3 center = tPos + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
        float camHeight = tPos.y + size.y + 100f; // high enough to see everything

        camGO.transform.position = new Vector3(center.x, camHeight, center.z);
        camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Ortho size is half of terrain depth (Z) in world units (plus padding)
        cam.orthographicSize = (size.z * 0.5f) + padding;

        // Match aspect ratio to terrain X:Z so it doesn't stretch
        float aspect = (size.x + padding * 2f) / (size.z + padding * 2f);
        cam.aspect = aspect;

        // Render to RT
        var rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        cam.targetTexture = rt;

        cam.Render();

        // Read pixels
        RenderTexture.active = rt;
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();

        // Save to Assets
        string folder = "Assets/TerrainVistaBakes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "TerrainVistaBakes");

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{terrain.name}_Vista_{resolution}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());

        // Cleanup
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGO);

        AssetDatabase.ImportAsset(path);
        EditorUtility.DisplayDialog("Vista Bake", $"Saved:\n{path}", "OK");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
#endif
