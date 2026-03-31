using UnityEngine;

[DisallowMultipleComponent]
public class ColdCameraShiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Heat heat;

    [Tooltip("Apply shiver to this transform. Recommended: a dedicated pivot parent of the Camera.")]
    [SerializeField] private Transform targetTransform;

    [Tooltip("Looping AudioSource for shiver sound.")]
    [SerializeField] private AudioSource shiverAudio;

    [Header("Heat Thresholds")]
    [SerializeField] private float startShiverHeat = 40f;
    [SerializeField] private float fullShiverHeat = 0f;

    [Header("Intensity Smoothing")]
    [Tooltip("How fast intensity matches heat (same speed up/down). Lower = slower.")]
    [SerializeField] private float intensityLerpSpeed = 2.0f;

    [Header("Sharp Jitter Settings")]
    [Tooltip("How often the jitter target changes (Hz). Higher = more 'tremor'.")]
    [SerializeField] private float jitterFrequency = 8.0f;

    [Tooltip("How fast the camera snaps to the new jitter target. Higher = sharper.")]
    [SerializeField] private float jitterSnapSpeed = 25.0f;

    [Header("Position Offsets (meters at full shiver)")]
    [SerializeField] private float maxPosX = 0.006f;
    [SerializeField] private float maxPosY = 0.006f;
    [SerializeField] private float maxPosZ = 0.000f;

    [Header("Rotation Offsets (degrees at full shiver)")]
    [Tooltip("Pitch (look up/down)")]
    [SerializeField] private float maxPitch = 0.25f;

    [Tooltip("Yaw (look left/right)")]
    [SerializeField] private float maxYaw = 0.25f;

    [Tooltip("Roll (tilt head)")]
    [SerializeField] private float maxRoll = 0.15f;

    [Header("Audio")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAudioVolume = 0.25f;

    [SerializeField] private float audioFadeTime = 0.35f;

    // Base pose
    private Vector3 _baseLocalPos;
    private Quaternion _baseLocalRot;

    // Intensity
    private float _currentIntensity;

    // Audio fade
    private float _audioCurrentVolume;

    // Jitter state (unit space -1..1)
    private Vector3 _posTargetUnit, _posCurrentUnit;
    private Vector3 _rotTargetUnit, _rotCurrentUnit;

    // Timing
    private int _lastStep = int.MinValue;

    // Seeds
    private float _seedPX, _seedPY, _seedPZ;
    private float _seedRX, _seedRY, _seedRZ;

    void Awake()
    {
        if (targetTransform == null) targetTransform = transform;

        _baseLocalPos = targetTransform.localPosition;
        _baseLocalRot = targetTransform.localRotation;

        // Deterministic seeds for each channel
        _seedPX = Random.Range(0f, 1000f);
        _seedPY = Random.Range(0f, 1000f);
        _seedPZ = Random.Range(0f, 1000f);
        _seedRX = Random.Range(0f, 1000f);
        _seedRY = Random.Range(0f, 1000f);
        _seedRZ = Random.Range(0f, 1000f);

        if (shiverAudio != null)
        {
            shiverAudio.playOnAwake = false;
            shiverAudio.loop = true;
            shiverAudio.volume = 0f;
            _audioCurrentVolume = 0f;
        }
    }

    void OnDisable()
    {
        if (targetTransform != null)
        {
            targetTransform.localPosition = _baseLocalPos;
            targetTransform.localRotation = _baseLocalRot;
        }

        if (shiverAudio != null)
        {
            shiverAudio.volume = 0f;
            if (shiverAudio.isPlaying) shiverAudio.Stop();
        }
    }

    void LateUpdate()
    {
        if (heat == null || targetTransform == null) return;

        // Target intensity from heat
        float targetIntensity = 0f;
        if (heat.currentHeat <= startShiverHeat)
        {
            targetIntensity = Mathf.InverseLerp(startShiverHeat, fullShiverHeat, heat.currentHeat);
            targetIntensity = Mathf.Clamp01(targetIntensity);
        }

        // Symmetric smoothing
        _currentIntensity = Mathf.MoveTowards(
            _currentIntensity,
            targetIntensity,
            intensityLerpSpeed * Time.deltaTime
        );

        UpdateJitterTargets();
        ApplyShiver(_currentIntensity);
        UpdateAudio(_currentIntensity);
    }

    void UpdateJitterTargets()
    {
        float freq = Mathf.Max(0.01f, jitterFrequency);
        int step = Mathf.FloorToInt(Time.time * freq);

        if (step == _lastStep) return;
        _lastStep = step;

        // New sample-and-hold targets in [-1..1]
        _posTargetUnit = new Vector3(
            HashToMinus1To1(step, _seedPX),
            HashToMinus1To1(step, _seedPY),
            HashToMinus1To1(step, _seedPZ)
        );

        _rotTargetUnit = new Vector3(
            HashToMinus1To1(step, _seedRX),
            HashToMinus1To1(step, _seedRY),
            HashToMinus1To1(step, _seedRZ)
        );
    }

    void ApplyShiver(float intensity)
    {
        if (intensity <= 0.0001f)
        {
            // Reset to clean base pose and reset jitter accumulators
            targetTransform.localPosition = _baseLocalPos;
            targetTransform.localRotation = _baseLocalRot;

            _posCurrentUnit = Vector3.zero;
            _rotCurrentUnit = Vector3.zero;
            return;
        }

        // Sharpening: snap current jitter toward target
        float snap = Mathf.Max(0.01f, jitterSnapSpeed);
        _posCurrentUnit = Vector3.MoveTowards(_posCurrentUnit, _posTargetUnit, snap * Time.deltaTime);
        _rotCurrentUnit = Vector3.MoveTowards(_rotCurrentUnit, _rotTargetUnit, snap * Time.deltaTime);

        // Scale to world units / degrees
        Vector3 posOffset = new Vector3(
            _posCurrentUnit.x * maxPosX,
            _posCurrentUnit.y * maxPosY,
            _posCurrentUnit.z * maxPosZ
        ) * intensity;

        // Pitch, Yaw, Roll (all independent)
        Vector3 rotOffset = new Vector3(
            _rotCurrentUnit.x * maxPitch, // X = pitch
            _rotCurrentUnit.y * maxYaw,   // Y = yaw
            _rotCurrentUnit.z * maxRoll   // Z = roll
        ) * intensity;

        targetTransform.localPosition = _baseLocalPos + posOffset;
        targetTransform.localRotation = _baseLocalRot * Quaternion.Euler(rotOffset);
    }

    void UpdateAudio(float intensity)
    {
        if (shiverAudio == null) return;

        float targetVol = maxAudioVolume * Mathf.Clamp01(intensity);

        if (targetVol > 0.0001f && !shiverAudio.isPlaying)
        {
            shiverAudio.volume = 0f;
            _audioCurrentVolume = 0f;
            shiverAudio.Play();
        }

        float fadeSpeed = (audioFadeTime <= 0.0001f) ? 9999f : (1f / audioFadeTime);
        _audioCurrentVolume = Mathf.MoveTowards(_audioCurrentVolume, targetVol, fadeSpeed * Time.deltaTime);
        shiverAudio.volume = _audioCurrentVolume;

        if (_audioCurrentVolume <= 0.0001f && shiverAudio.isPlaying && targetVol <= 0.0001f)
            shiverAudio.Stop();
    }

    // Deterministic hash: int step + seed -> 0..1 -> remap to -1..1
    static float HashToMinus1To1(int step, float seed)
    {
        // Simple pseudo-random hash
        float x = Mathf.Sin((step + 1) * 12.9898f + seed * 78.233f) * 43758.5453f;
        float v = x - Mathf.Floor(x); // frac 0..1
        return (v * 2f) - 1f;
    }
}
