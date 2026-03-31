using UnityEngine;

public class WalkLoop_FadeOutOnStop : MonoBehaviour
{
    [Header("Audio (clip already assigned)")]
    public AudioSource source;

    [Header("What moves")]
    public Transform movementRoot; // leave empty = this transform

    [Header("Start/Stop detection")]
    public float startSpeed = 0.08f;  // m/s to START
    public float stopSpeed  = 0.04f;  // m/s to consider STOPPING
    public float stopDelay  = 0.12f;  // seconds below stopSpeed before fading out

    [Header("Fade")]
    public float fadeOutTime = 0.18f; // seconds to fade out

    Vector3 lastPos;
    float stillTimer;
    float baseVolume;
    Coroutine fadeRoutine;

    void Awake()
    {
        if (!movementRoot) movementRoot = transform;
        if (!source) source = GetComponentInChildren<AudioSource>();

        if (!source)
        {
            Debug.LogError("WalkLoop_FadeOutOnStop: No AudioSource found on player/child.", this);
            enabled = false;
            return;
        }

        source.playOnAwake = false;
        source.loop = true;

        baseVolume = source.volume;
        lastPos = movementRoot.position;
    }

    void Update()
    {
        Vector3 delta = movementRoot.position - lastPos;
        delta.y = 0f;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = movementRoot.position;

        // START
        if (speed >= startSpeed)
        {
            stillTimer = 0f;

            // cancel fade-out if we start moving again
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
                source.volume = baseVolume;
            }

            if (!source.isPlaying && source.clip != null)
            {
                source.volume = baseVolume;
                source.Play();
            }
            return;
        }

        // STOP (with delay + fade)
        if (source.isPlaying)
        {
            if (speed <= stopSpeed) stillTimer += Time.deltaTime;
            else stillTimer = 0f;

            if (stillTimer >= stopDelay && fadeRoutine == null)
            {
                fadeRoutine = StartCoroutine(FadeOutAndStop());
            }
        }
    }

    System.Collections.IEnumerator FadeOutAndStop()
    {
        float t = 0f;
        float startVol = source.volume;

        // If fadeOutTime is 0, stop immediately
        if (fadeOutTime <= 0f)
        {
            source.Stop();
            source.volume = baseVolume;
            fadeRoutine = null;
            yield break;
        }

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / fadeOutTime);
            yield return null;
        }

        source.Stop();
        source.volume = baseVolume;
        stillTimer = 0f;
        fadeRoutine = null;
    }
}
