using UnityEngine;

public class DogWalkLoop_MoveStartStop : MonoBehaviour
{
    [Header("Audio (clip already assigned)")]
    public AudioSource source;

    [Header("What moves")]
    public Transform movementRoot; // leave empty = this transform

    [Header("Tuning")]
    public float startSpeed = 0.06f; // m/s to START sound
    public float stopSpeed  = 0.03f; // m/s to STOP sound (lower than startSpeed)
    public float stopDelay  = 0.10f; // seconds of "not moving" before stopping

    Vector3 lastPos;
    float stillTimer;

    void Awake()
    {
        if (!movementRoot) movementRoot = transform;
        if (!source) source = GetComponentInChildren<AudioSource>();

        if (!source)
        {
            Debug.LogError("DogWalkLoop_MoveStartStop: No AudioSource found on dog/child.", this);
            enabled = false;
            return;
        }

        source.playOnAwake = false;
        source.loop = true; // IMPORTANT: your dog walk clip should be loopable
        lastPos = movementRoot.position;
    }

    void Update()
    {
        Vector3 delta = movementRoot.position - lastPos;
        delta.y = 0f;

        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = movementRoot.position;

        // START immediately when moving
        if (speed >= startSpeed)
        {
            stillTimer = 0f;
            if (!source.isPlaying && source.clip != null)
                source.Play();
            return;
        }

        // STOP after a short delay to avoid flicker
        if (source.isPlaying)
        {
            if (speed <= stopSpeed) stillTimer += Time.deltaTime;
            else stillTimer = 0f;

            if (stillTimer >= stopDelay)
            {
                source.Stop();
                stillTimer = 0f;
            }
        }
    }
}
