using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class AutoOutline : MonoBehaviour
{
    [Header("Outline Look")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField, Range(1.001f, 1.10f)] private float outlineScale = 1.03f;

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 8f; // higher = faster

    private GameObject outlineRoot;
    private Renderer[] outlineRenderers;

    private Coroutine fadeRoutine;
    private float currentAlpha = 0f;

    private void Awake()
    {
        BuildOutline();

        // Start fully invisible + NOT rendering
        SetAlpha(0f);
        outlineRoot.SetActive(false);
    }

    private void BuildOutline()
    {
        if (outlineMaterial == null)
        {
            Debug.LogError($"[{nameof(AutoOutline)}] No outline material assigned on {name}.", this);
            return;
        }

        outlineRoot = new GameObject("__Outline");
        outlineRoot.transform.SetParent(transform, false);
        outlineRoot.transform.localScale = Vector3.one * outlineScale;

        var meshRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);

        foreach (var mr in meshRenderers)
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var child = new GameObject("OutlineMesh");
            child.transform.SetParent(outlineRoot.transform, false);
            child.transform.position = mr.transform.position;
            child.transform.rotation = mr.transform.rotation;
            child.transform.localScale = mr.transform.lossyScale;

            var newMf = child.AddComponent<MeshFilter>();
            newMf.sharedMesh = mf.sharedMesh;

            var newMr = child.AddComponent<MeshRenderer>();

            // Important: instance material per object so fading doesn't affect others
            newMr.sharedMaterial = new Material(outlineMaterial);

            newMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            newMr.receiveShadows = false;
        }

        outlineRenderers = outlineRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    public void FadeIn()
    {
        if (outlineRoot == null) return;

        outlineRoot.SetActive(true);
        StartFade(1f);
    }

    public void FadeOut()
    {
        if (outlineRoot == null) return;

        StartFade(0f, disableAtEnd: true);
    }

    private void StartFade(float targetAlpha, bool disableAtEnd = false)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, disableAtEnd));
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool disableAtEnd)
    {
        while (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            SetAlpha(currentAlpha);
            yield return null;
        }

        // If we're fully faded out, stop rendering entirely
        if (disableAtEnd && Mathf.Approximately(currentAlpha, 0f))
        {
            outlineRoot.SetActive(false);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (outlineRenderers == null) return;

        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            var r = outlineRenderers[i];
            if (r == null) continue;

            var c = r.material.color;
            c.a = alpha;
            r.material.color = c;
        }
    }
}
