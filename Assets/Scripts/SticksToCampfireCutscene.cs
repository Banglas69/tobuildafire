using System.Collections;
using UnityEngine;

public class SticksToCampfireCutscene : MonoBehaviour
{
    [Header("Stick Goal")]
    [SerializeField] private int sticksRequired = 5;
    [SerializeField] private int sticksCollected = 0;

    [Header("Fade UI")]
    [Tooltip("CanvasGroup on a full-screen black Image. Alpha should start at 0.")]
    [SerializeField] private CanvasGroup fadeGroup;

    [SerializeField] private float fadeToBlackTime = 0.6f;
    [SerializeField] private float fadeFromBlackTime = 0.6f;

    [Header("Black Hold")]
    [Tooltip("How long the screen stays fully black between fade-out and fade-in.")]
    [SerializeField] private float blackHoldSeconds = 4f;

    [Header("Black Hold Audio")]
    [Tooltip("AudioSource used for the 'during black screen' audio. Can be on any GameObject.")]
    [SerializeField] private AudioSource blackScreenAudio;

    [Tooltip("Fade in/out time for the black screen audio.")]
    [SerializeField] private float blackAudioFadeTime = 0.35f;

    [Header("Fireplace Activation")]
    [Tooltip("Assign the FIREPLACE prefab instance in the scene (initially inactive). It will be activated during the black screen.")]
    [SerializeField] private GameObject fireplacePrefab;

    [Header("Post-Cutscene Fire Timeline")]
    [Tooltip("The visible flame particle system.")]
    [SerializeField] private ParticleSystem fireParticles;

    [Tooltip("Optional. Assign the flame transform if you want the particles to visually shrink while fading.")]
    [SerializeField] private Transform fireParticlesTransform;

    [Tooltip("Optional tagged warmth object. Good for a child object tagged 'WarmthSource'.")]
    [SerializeField] private GameObject warmthSourceObject;

    [Tooltip("Optional heat/warmth scripts to toggle on/off with the campfire timeline.")]
    [SerializeField] private Behaviour[] warmthBehaviours;

    [Tooltip("How long the fire stays fully active after the cutscene ends.")]
    [SerializeField] private float warmthFullDuration = 5f;

    [Tooltip("How long the flames take to die out after the warm period. 5 + 2 = 7 seconds total.")]
    [SerializeField] private float flameFadeDuration = 2f;

    [Tooltip("Clear remaining particles when the fade finishes.")]
    [SerializeField] private bool clearParticlesAtEnd = true;

    [Header("Player Root + Control")]
    [SerializeField] private Transform playerRoot;

    [Tooltip("Disable these scripts during cutscene (movement, look, pickup raycaster, etc.).")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("Campfire Pose")]
    [Tooltip("Where the player should be moved for the campfire cutscene (position AND rotation).")]
    [SerializeField] private Transform campfireCutsceneSpawn;

    [Tooltip("Optional: if set, player will face this target (yaw only) instead of using spawn rotation.")]
    [SerializeField] private Transform campfireLookTarget;

    [Header("Optional: Camera Pitch (for FPS rigs)")]
    [SerializeField] private Transform cameraPitchTransform;
    [SerializeField] private float campfirePitchDegrees = 0f;

    [Header("Cutscene Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string triggerName = "Campfire";
    [SerializeField] private string stateName = "Campfire";
    [SerializeField] private int layerIndex = 0;
    [SerializeField] private float maxCutsceneTime = 8f;

    private bool _playing;
    private Vector3 _savedPitchLocalEuler;
    private float _audioRuntimeVolume = 0f;

    private float _baseRateOverTime;
    private float _baseRateOverDistance;
    private Vector3 _baseFlameScale = Vector3.one;
    private bool _cachedFireDefaults;

    private void Awake()
    {
        CacheFireDefaults();
    }

    public void RegisterStickPickup()
    {
        if (_playing) return;

        sticksCollected++;
        if (sticksCollected >= sticksRequired)
            StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        _playing = true;

        if (cameraPitchTransform != null)
            _savedPitchLocalEuler = cameraPitchTransform.localEulerAngles;

        SetControlsEnabled(false);

        // 1) Fade to black
        yield return Fade(1f, fadeToBlackTime);

        // 2) Start black-screen audio (fade in)
        if (blackScreenAudio != null)
        {
            blackScreenAudio.loop = true;
            if (!blackScreenAudio.isPlaying) blackScreenAudio.Play();
            yield return FadeAudio(blackScreenAudio, 1f, blackAudioFadeTime);
        }

        // 3) Activate fireplace visuals during black screen
        if (fireplacePrefab != null && !fireplacePrefab.activeSelf)
            fireplacePrefab.SetActive(true);

        EnsureFireVisualsAreOn();
        SetWarmthActive(false); // warmth only starts after the cutscene ends

        // 4) Teleport to campfire pose during black screen
        var (pos, rot) = GetCampfirePose();
        TeleportPlayer(pos, rot);

        if (cameraPitchTransform != null)
        {
            Vector3 e = cameraPitchTransform.localEulerAngles;
            e.x = campfirePitchDegrees;
            cameraPitchTransform.localEulerAngles = e;
        }

        // 5) Hold on full black
        if (blackHoldSeconds > 0f)
            yield return new WaitForSeconds(blackHoldSeconds);

        // 6) Start animation
        if (characterAnimator != null)
        {
            characterAnimator.ResetTrigger(triggerName);
            characterAnimator.SetTrigger(triggerName);
        }

        // 7) Fade back in from black
        yield return Fade(0f, fadeFromBlackTime);

        // 8) Stop black-screen audio
        if (blackScreenAudio != null)
        {
            yield return FadeAudio(blackScreenAudio, 0f, blackAudioFadeTime);
            blackScreenAudio.Stop();
        }

        // 9) Wait for animation to finish (or timeout)
        float t = 0f;
        while (t < maxCutsceneTime)
        {
            t += Time.deltaTime;

            if (characterAnimator == null) break;
            if (IsStateFinished(characterAnimator, layerIndex, stateName)) break;

            yield return null;
        }

        // Restore pitch
        if (cameraPitchTransform != null)
            cameraPitchTransform.localEulerAngles = _savedPitchLocalEuler;

        // Return control at the campfire location
        SetControlsEnabled(true);

        // Start the temporary warmth / flame death timeline
        StartCoroutine(PostCutsceneFireSequence());

        _playing = false;
    }

    private IEnumerator PostCutsceneFireSequence()
    {
        EnsureFireVisualsAreOn();
        SetWarmthActive(true);

        if (warmthFullDuration > 0f)
            yield return new WaitForSeconds(warmthFullDuration);

        yield return FadeFlamesOut(flameFadeDuration);

        SetWarmthActive(false);
    }

    private void CacheFireDefaults()
    {
        if (_cachedFireDefaults || fireParticles == null) return;

        var emission = fireParticles.emission;
        _baseRateOverTime = emission.rateOverTimeMultiplier;
        _baseRateOverDistance = emission.rateOverDistanceMultiplier;

        Transform flameT = fireParticlesTransform != null ? fireParticlesTransform : fireParticles.transform;
        _baseFlameScale = flameT.localScale;

        _cachedFireDefaults = true;
    }

    private void EnsureFireVisualsAreOn()
    {
        if (fireplacePrefab != null && !fireplacePrefab.activeSelf)
            fireplacePrefab.SetActive(true);

        if (fireParticles == null) return;

        CacheFireDefaults();

        var emission = fireParticles.emission;
        emission.rateOverTimeMultiplier = _baseRateOverTime;
        emission.rateOverDistanceMultiplier = _baseRateOverDistance;

        Transform flameT = fireParticlesTransform != null ? fireParticlesTransform : fireParticles.transform;
        flameT.localScale = _baseFlameScale;

        fireParticles.Clear(true);
        fireParticles.Play(true);
    }

    private IEnumerator FadeFlamesOut(float duration)
    {
        if (fireParticles == null)
        {
            if (duration > 0f)
                yield return new WaitForSeconds(duration);
            yield break;
        }

        CacheFireDefaults();

        var emission = fireParticles.emission;
        Transform flameT = fireParticlesTransform != null ? fireParticlesTransform : fireParticles.transform;

        float startRateOverTime = emission.rateOverTimeMultiplier;
        float startRateOverDistance = emission.rateOverDistanceMultiplier;
        Vector3 startScale = flameT.localScale;

        if (duration <= 0.0001f)
        {
            emission.rateOverTimeMultiplier = 0f;
            emission.rateOverDistanceMultiplier = 0f;
            flameT.localScale = Vector3.zero;
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (clearParticlesAtEnd)
                fireParticles.Clear(true);

            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            emission.rateOverTimeMultiplier = Mathf.Lerp(startRateOverTime, 0f, k);
            emission.rateOverDistanceMultiplier = Mathf.Lerp(startRateOverDistance, 0f, k);
            flameT.localScale = Vector3.Lerp(startScale, Vector3.zero, k);

            yield return null;
        }

        emission.rateOverTimeMultiplier = 0f;
        emission.rateOverDistanceMultiplier = 0f;
        flameT.localScale = Vector3.zero;

        fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (clearParticlesAtEnd)
            fireParticles.Clear(true);
    }

    private void SetWarmthActive(bool active)
    {
        if (warmthSourceObject != null)
            warmthSourceObject.SetActive(active);

        if (warmthBehaviours != null)
        {
            foreach (var behaviour in warmthBehaviours)
            {
                if (behaviour != null)
                    behaviour.enabled = active;
            }
        }
    }

    private (Vector3 pos, Quaternion rot) GetCampfirePose()
    {
        if (campfireCutsceneSpawn == null)
            return (playerRoot != null ? playerRoot.position : Vector3.zero,
                    playerRoot != null ? playerRoot.rotation : Quaternion.identity);

        Vector3 pos = campfireCutsceneSpawn.position;

        if (campfireLookTarget == null)
            return (pos, campfireCutsceneSpawn.rotation);

        Vector3 dir = campfireLookTarget.position - pos;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return (pos, campfireCutsceneSpawn.rotation);

        return (pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
    }

    private void TeleportPlayer(Vector3 pos, Quaternion rot)
    {
        if (playerRoot == null) return;

        var cc = playerRoot.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        var rb = playerRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerRoot.SetPositionAndRotation(pos, rot);

        if (cc != null) cc.enabled = true;
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (fadeGroup == null) yield break;

        fadeGroup.blocksRaycasts = true;
        float start = fadeGroup.alpha;

        if (duration <= 0.0001f)
        {
            fadeGroup.alpha = targetAlpha;
            fadeGroup.blocksRaycasts = targetAlpha > 0.001f;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            fadeGroup.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
        fadeGroup.blocksRaycasts = targetAlpha > 0.001f;
    }

    private IEnumerator FadeAudio(AudioSource src, float targetVolume01, float duration)
    {
        if (src == null) yield break;

        float maxVol = Mathf.Clamp01(src.volume <= 0.0001f ? 1f : src.volume);
        float start = _audioRuntimeVolume;
        float target = Mathf.Clamp01(targetVolume01) * maxVol;

        if (duration <= 0.0001f)
        {
            _audioRuntimeVolume = target;
            src.volume = _audioRuntimeVolume;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            _audioRuntimeVolume = Mathf.Lerp(start, target, k);
            src.volume = _audioRuntimeVolume;
            yield return null;
        }

        _audioRuntimeVolume = target;
        src.volume = _audioRuntimeVolume;
    }

    private void SetControlsEnabled(bool enabled)
    {
        if (scriptsToDisable == null) return;

        foreach (var s in scriptsToDisable)
        {
            if (s != null)
                s.enabled = enabled;
        }
    }

    private static bool IsStateFinished(Animator anim, int layer, string state)
    {
        var st = anim.GetCurrentAnimatorStateInfo(layer);

        if (!st.IsName(state) && anim.IsInTransition(layer))
            st = anim.GetNextAnimatorStateInfo(layer);

        return st.IsName(state) && st.normalizedTime >= 1f && !anim.IsInTransition(layer);
    }
}