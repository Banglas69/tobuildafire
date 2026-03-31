using UnityEngine;

[ExecuteAlways]
public class FSFreezeMaterialController : MonoBehaviour
{
    [Header("Assign the SAME material used by your Full Screen Pass / Renderer Feature")]
    [SerializeField] private Material targetMaterial;

    [Header("Shader Graph property references (must match Blackboard > Reference)")]
    [SerializeField] private string enabledRef      = "_Enabled";
    [SerializeField] private string pulseSpeedRef   = "_PulseSpeed";
    [SerializeField] private string maskNoiseRef    = "_MaskNoise";
    [SerializeField] private string maskSizeRef     = "_MaskSize";
    [SerializeField] private string maskContrastRef = "_MaskContrast";
    [SerializeField] private string alphaRef        = "_Alpha";
    [SerializeField] private string colorRef        = "_Color";

    [Header("Values")]
    public bool Enabled = true;
    [Min(0f)] public float PulseSpeed = 1f;
    [Range(0f, 1f)] public float MaskNoise = 0.5f;
    [Range(0f, 1f)] public float MaskSize = 0.5f;
    [Min(0f)] public float MaskContrast = 1f;
    [Range(0f, 1f)] public float Alpha = 1f;
    public Color Color = Color.cyan;

    int _enabledId, _pulseSpeedId, _maskNoiseId, _maskSizeId, _maskContrastId, _alphaId, _colorId;

    void OnEnable()
    {
        CacheIds();
        Apply();
    }

    void OnValidate()
    {
        CacheIds();
        Apply();
    }

    void Update()
    {
        // If you want runtime-only updates, you can gate this behind Application.isPlaying
        Apply();
    }

    void CacheIds()
    {
        _enabledId      = Shader.PropertyToID(enabledRef);
        _pulseSpeedId   = Shader.PropertyToID(pulseSpeedRef);
        _maskNoiseId    = Shader.PropertyToID(maskNoiseRef);
        _maskSizeId     = Shader.PropertyToID(maskSizeRef);
        _maskContrastId = Shader.PropertyToID(maskContrastRef);
        _alphaId        = Shader.PropertyToID(alphaRef);
        _colorId        = Shader.PropertyToID(colorRef);
    }

    public void Apply()
    {
        if (targetMaterial == null) return;

        // Helpful warnings if your Reference names don’t match:
        WarnIfMissing(targetMaterial, _enabledId, enabledRef);
        WarnIfMissing(targetMaterial, _pulseSpeedId, pulseSpeedRef);
        WarnIfMissing(targetMaterial, _maskNoiseId, maskNoiseRef);
        WarnIfMissing(targetMaterial, _maskSizeId, maskSizeRef);
        WarnIfMissing(targetMaterial, _maskContrastId, maskContrastRef);
        WarnIfMissing(targetMaterial, _alphaId, alphaRef);
        WarnIfMissing(targetMaterial, _colorId, colorRef);

        // Boolean in Shader Graph = float 0/1
        targetMaterial.SetFloat(_enabledId, Enabled ? 1f : 0f);

        targetMaterial.SetFloat(_pulseSpeedId, PulseSpeed);
        targetMaterial.SetFloat(_maskNoiseId, MaskNoise);
        targetMaterial.SetFloat(_maskSizeId, MaskSize);
        targetMaterial.SetFloat(_maskContrastId, MaskContrast);
        targetMaterial.SetFloat(_alphaId, Alpha);
        targetMaterial.SetColor(_colorId, Color);
    }

    static void WarnIfMissing(Material mat, int id, string refName)
    {
        if (!mat.HasProperty(id))
            Debug.LogWarning($"Material '{mat.name}' does not have property Reference '{refName}'. " +
                             $"Check Shader Graph Blackboard > Reference names.", mat);
    }

    // Optional convenience methods for UI buttons/sliders
    public void SetEnabled(bool value) => Enabled = value;
    public void SetAlpha(float value) => Alpha = value;
}
