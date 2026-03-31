using UnityEngine;

public class WatchCutsceneController : MonoBehaviour
{
    System.Collections.IEnumerator TriggerCameraAfterDelay()
{
    yield return new WaitForSeconds(cameraDelay);

    if (cameraAnimator)
    {
        cameraAnimator.ResetTrigger(triggerName);
        cameraAnimator.SetTrigger(triggerName);
    }
}

    public Animator characterAnimator;
    public Animator cameraAnimator;

    [Header("Camera timing")]
public float cameraDelay = 0.15f; // seconds

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip watchSfx;
    [Range(0f, 1f)] public float watchSfxVolume = 1f;

    [Header("Voice Over")]
public AudioSource voiceSource;
public AudioClip voiceLine;
[Range(0f, 1f)] public float voiceVolume = 1f;


    public MonoBehaviour[] inputScriptsToDisable;

    public string triggerName = "LookWatch";
    public string watchStateName = "LookAtWatch";
    public int characterLayerIndex = 0;

    public float maxCutsceneTime = 3.0f; // failsafe

    bool playing;
    float timer;

    void Awake()
    {
        if (!characterAnimator) characterAnimator = GetComponentInChildren<Animator>(true);
    }

    public void PlayWatchCutscene()
{
    if (voiceSource && voiceLine)
{
    voiceSource.PlayOneShot(voiceLine, voiceVolume);
}

    if (playing || !characterAnimator) return;

    playing = true;
    timer = 0f;
    SetInputsEnabled(false);

    // play SFX if you added it
    if (sfxSource && watchSfx) sfxSource.PlayOneShot(watchSfx, watchSfxVolume);

    // start character animation immediately
    characterAnimator.ResetTrigger(triggerName);
    characterAnimator.SetTrigger(triggerName);

    // start camera animation slightly later
    if (cameraAnimator)
        StartCoroutine(TriggerCameraAfterDelay());
}


    void Update()
    {
        if (!playing || !characterAnimator) return;

        timer += Time.deltaTime;

        if (IsStateFinished(characterAnimator, characterLayerIndex, watchStateName) || timer >= maxCutsceneTime)
        {
            SetInputsEnabled(true);
            playing = false;
        }
    }

    bool IsStateFinished(Animator anim, int layer, string stateName)
    {
        // Handle transitions (sometimes the state is "Next" while blending)
        var st = anim.GetCurrentAnimatorStateInfo(layer);
        if (!st.IsName(stateName) && anim.IsInTransition(layer))
            st = anim.GetNextAnimatorStateInfo(layer);

        return st.IsName(stateName) && st.normalizedTime >= 1f && !anim.IsInTransition(layer);
    }

    void SetInputsEnabled(bool enabled)
    {
        foreach (var s in inputScriptsToDisable)
            if (s) s.enabled = enabled;
    }
}
