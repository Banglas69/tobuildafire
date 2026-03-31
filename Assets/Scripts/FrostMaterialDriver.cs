using UnityEngine;

[ExecuteAlways]
public class FrostMaterialDriver : MonoBehaviour
{
    [Header("Assign the FS_Freeze material used by the overlay")]
    [SerializeField] private Material targetMaterial;

    [Header("Two-phase progression")]
    [Tooltip("Portion of FrostAmount [0..1] used to reduce Mask Size (1->0). After this, contrast starts dropping.")]
    [Range(0.01f, 0.99f)]
    [SerializeField] private float sizePhase = 0.5f;

    [Tooltip("Minimum Mask Contrast at full freeze.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float minContrast = 0.039f;

    [Header("Optional: keep the overlay 'Enabled' input forced on")]
    [SerializeField] private bool forceEnabled = true;

    // Heat.cs drives this: frostEffect.FrostAmount = Lerp(...). :contentReference[oaicite:2]{index=2}
    [SerializeField, Range(0f, 1f)]
    private float frostAmount = 0f;

    public float FrostAmount
    {
        get => frostAmount;
        set
        {
            frostAmount = Mathf.Clamp01(value);
            Apply();
        }
    }

    // Resolved property IDs
    private int _enabledId = -1;
    private int _maskSizeId = -1;
    private int _maskContrastId = -1;

    // Prevent log spam
    private bool _loggedMissing;

    void OnEnable()
    {
        ResolvePropertyIds();
        Apply();
    }

    void OnValidate()
    {
        ResolvePropertyIds();
        Apply();
    }

    void Update()
    {
        // In edit mode, keep preview responsive when frostAmount is tweaked in Inspector
        if (!Application.isPlaying)
            Apply();
    }

    private void ResolvePropertyIds()
    {
        _loggedMissing = false;

        if (targetMaterial == null)
        {
            _enabledId = _maskSizeId = _maskContrastId = -1;
            return;
        }

        // Try several common reference-name variants (Shader Graph often defaults to display name with spaces)
        _enabledId      = ResolveId(targetMaterial, "_Enabled", "Enabled", "enabled");
        _maskSizeId     = ResolveId(targetMaterial, "_MaskSize", "Mask Size", "MaskSize", "_Mask_Size");
        _maskContrastId = ResolveId(targetMaterial, "_MaskContrast", "Mask Contrast", "MaskContrast", "_Mask_Contrast");
    }

    private static int ResolveId(Material mat, params string[] candidates)
    {
        foreach (var name in candidates)
        {
            int id = Shader.PropertyToID(name);
            if (mat.HasProperty(id))
                return id;
        }
        return -1;
    }

    private void Apply()
    {
        if (targetMaterial == null) return;

        // One-time warning if we still can't find properties
        if ((_maskSizeId == -1 || _maskContrastId == -1) && !_loggedMissing)
        {
            _loggedMissing = true;
            Debug.LogWarning(
                $"FrostMaterialDriver: Could not resolve required properties on material '{targetMaterial.name}'.\n" +
                $"Found MaskSize? {(_maskSizeId != -1)} | Found MaskContrast? {(_maskContrastId != -1)}\n" +
                $"Fix by setting Shader Graph Blackboard > Reference names, or update candidate lists in this script.",
                this
            );
        }

        // Keep overlay enabled if desired (bool is a float 0/1 in Shader Graph)
        if (forceEnabled && _enabledId != -1)
            targetMaterial.SetFloat(_enabledId, 1f);

        // Convert FrostAmount into the exact sequential behavior you requested:
        // Phase 1: MaskSize 1 -> 0
        // Phase 2: MaskContrast 1 -> minContrast (after MaskSize hits 0)
        float maskSize;
        float maskContrast;

        if (frostAmount <= sizePhase)
        {
            float t = frostAmount / Mathf.Max(0.0001f, sizePhase); // 0..1
            maskSize = Mathf.Lerp(1f, 0f, t);
            maskContrast = 1f;
        }
        else
        {
            float t = (frostAmount - sizePhase) / Mathf.Max(0.0001f, 1f - sizePhase); // 0..1
            maskSize = 0f;
            maskContrast = Mathf.Lerp(1f, minContrast, t);
        }

        // Round to 3 decimals as requested
        maskSize = Round3(maskSize);
        maskContrast = Round3(maskContrast);

        if (_maskSizeId != -1)     targetMaterial.SetFloat(_maskSizeId, maskSize);
        if (_maskContrastId != -1) targetMaterial.SetFloat(_maskContrastId, maskContrast);
    }

    private static float Round3(float v) => Mathf.Round(v * 1000f) / 1000f;
}
