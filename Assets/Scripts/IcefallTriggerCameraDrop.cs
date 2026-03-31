using System.Collections;
using UnityEngine;

public class IcefallTriggerDrop : MonoBehaviour
{
    [Header("Player detection")]
    public string playerTag = "Player";

    [Header("What to move (VisualRig)")]
    [Tooltip("Assign the player's VisualRig (child that contains camera + model).")]
    public Transform visualRig;

    [Tooltip("Optional: if VisualRig isn't assigned, we try to find this child name on the player.")]
    public string visualRigChildName = "VisualRig";

    [Header("Drop Settings (local space)")]
    public float dropAmount = 1.0f;        // local Y units down
    public float dropDuration = 0.18f;     // swift but smooth
    public float holdDuration = 1.25f;     // stay down
    public float returnDuration = 0.22f;   // return smoothly

    [Header("Input Lock")]
    [Tooltip("Drag your movement + look scripts here (scripts that read input).")]
    public MonoBehaviour[] inputScriptsToDisable;

    [Header("Sound")]
    public AudioSource sfxSource;          // can be on this trigger, or assign one on player/camera
    public AudioClip sfxClip;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("After Icefall")]
    [Tooltip("Drag the Heat component from the player here (recommended).")]
    public MonoBehaviour heatComponent;    // drag your Heat script component here
    public bool enableHeatAfterIcefall = true;

    [Header("Behaviour")]
    public bool playOnce = true;

    bool running;
    bool hasPlayed;
    Vector3 originalLocalPos;
    Coroutine routine;

    void Awake()
    {
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (running) return;
        if (playOnce && hasPlayed) return;
        if (!other.CompareTag(playerTag)) return;

        // Find VisualRig if not assigned
        if (!visualRig)
        {
            var t = other.transform.Find(visualRigChildName);
            if (t) visualRig = t;
        }

        if (!visualRig)
        {
            Debug.LogError(
                $"IcefallTriggerVisualRigDrop: VisualRig not assigned and child '{visualRigChildName}' not found under player. " +
                $"Create a child named '{visualRigChildName}' under the player and put Camera/Model under it, then assign it.",
                this);
            return;
        }

        // Optional auto-find Heat if not assigned (fallback only)
        if (enableHeatAfterIcefall && !heatComponent)
            heatComponent = other.GetComponent("Heat") as MonoBehaviour;

        hasPlayed = true;
        routine = StartCoroutine(DropHoldReturn());
    }

    IEnumerator DropHoldReturn()
    {
        running = true;

        // Disable inputs
        SetInputsEnabled(false);

        // Play SFX
        if (sfxSource && sfxClip)
            sfxSource.PlayOneShot(sfxClip, sfxVolume);

        // Store original local position
        originalLocalPos = visualRig.localPosition;
        Vector3 downPos = originalLocalPos + Vector3.down * dropAmount;

        // Drop
        yield return MoveLocal(visualRig, originalLocalPos, downPos, dropDuration);

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Return
        yield return MoveLocal(visualRig, downPos, originalLocalPos, returnDuration);

        // Enable Heat after icefall completes
        if (enableHeatAfterIcefall && heatComponent)
            heatComponent.enabled = true;

        // Re-enable inputs
        SetInputsEnabled(true);

        running = false;
        routine = null;
    }

    IEnumerator MoveLocal(Transform t, Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            t.localPosition = to;
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float u = Mathf.Clamp01(time / duration);
            float s = u * u * (3f - 2f * u); // SmoothStep
            t.localPosition = Vector3.LerpUnclamped(from, to, s);
            yield return null;
        }

        t.localPosition = to;
    }

    void SetInputsEnabled(bool enabled)
    {
        if (inputScriptsToDisable == null) return;
        foreach (var s in inputScriptsToDisable)
            if (s) s.enabled = enabled;
    }

    void OnDisable()
    {
        // Safety: restore if trigger gets disabled mid-effect
        if (routine != null) StopCoroutine(routine);

        if (visualRig)
            visualRig.localPosition = originalLocalPos;

        if (enableHeatAfterIcefall && heatComponent)
            heatComponent.enabled = true;

        SetInputsEnabled(true);

        running = false;
        routine = null;
    }
}
