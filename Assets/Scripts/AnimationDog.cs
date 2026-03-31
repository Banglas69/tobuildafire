using UnityEngine;

public class DogAnimFromMovement : MonoBehaviour
{
    public Animator animator;
    public Transform movementRoot;
    public float walkMetersPerSec = 1.5f;
    public float runMetersPerSec = 4.0f;
    public float damp = 0.08f;

    Vector3 lastPos;
    int speedHash;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!movementRoot) movementRoot = transform;

        lastPos = movementRoot.position;
        speedHash = Animator.StringToHash("Speed");
    }

    void Update()
    {
        Vector3 delta = movementRoot.position - lastPos;
        delta.y = 0f;

        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = movementRoot.position;

        float a;
        if (speed <= walkMetersPerSec)
            a = speed / Mathf.Max(walkMetersPerSec, 0.001f);
        else
            a = 1f + Mathf.Clamp01((speed - walkMetersPerSec) /
                                   Mathf.Max(runMetersPerSec - walkMetersPerSec, 0.001f));

        animator.SetFloat(speedHash, a, damp, Time.deltaTime);
    }
}
