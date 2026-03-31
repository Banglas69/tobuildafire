using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DogCompanionOrbit : MonoBehaviour
{
    public Transform player;

    [Header("Follow / Orbit")]
    public float orbitRadius = 3.0f;          // how far from player the dog tries to stay
    public float orbitSpeed = 90f;            // degrees/sec around the player
    public float followSpeed = 4.5f;          // max move speed
    public float acceleration = 12f;          // how quickly it reaches followSpeed
    public float stoppingDistance = 0.6f;     // how close to the target point counts as "arrived"
    public float catchUpMultiplier = 1.6f;    // speed boost if player gets far away
    public float catchUpDistance = 7f;

    [Header("Rotation")]
    public float turnSpeedDegPerSec = 540f;   // how fast the dog turns to face movement

    [Header("Wander")]
    public Vector2 followTimeRange = new Vector2(6f, 14f);   // time orbit-following before wandering
    public Vector2 wanderTimeRange = new Vector2(2.5f, 6f);  // time wandering
    public float wanderRadius = 6f;                           // wander destination around the player
    public float returnDistance = 4f;                         // if wander drifts too far, return early

    [Header("Grounding")]
    public LayerMask groundMask = ~0;
    public float groundRayHeight = 3f;
    public float groundRayDistance = 10f;

    [Header("Debug")]
    public bool drawGizmos = true;

    Rigidbody rb;
    float orbitAngleDeg;
    float stateTimer;
    Vector3 wanderTarget;

    enum State { OrbitFollow, Wander, ReturnToOrbit }
    State state = State.OrbitFollow;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Good defaults to prevent physics spin-outs
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.freezeRotation = true; // we control rotation manually

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        orbitAngleDeg = Random.Range(0f, 360f);
        ResetFollowTimer();
    }

    void ResetFollowTimer()
    {
        stateTimer = Random.Range(followTimeRange.x, followTimeRange.y);
    }

    void ResetWanderTimer()
    {
        stateTimer = Random.Range(wanderTimeRange.x, wanderTimeRange.y);
    }

    void Update()
    {
        if (!player) return;

        stateTimer -= Time.deltaTime;

        // Switch logic
        if (state == State.OrbitFollow)
        {
            if (stateTimer <= 0f)
            {
                // Pick a wander destination near the player
                wanderTarget = SampleGroundNear(player.position, wanderRadius);
                state = State.Wander;
                ResetWanderTimer();
            }
        }
        else if (state == State.Wander)
        {
            // If wandered too far, come back sooner
            float distToPlayer = HorizontalDistance(transform.position, player.position);
            if (distToPlayer > Mathf.Max(returnDistance, wanderRadius * 1.2f) || stateTimer <= 0f)
            {
                state = State.ReturnToOrbit;
            }
        }
        else if (state == State.ReturnToOrbit)
        {
            // Once we're back near orbit radius, resume orbit-follow
            float distToPlayer = HorizontalDistance(transform.position, player.position);
            if (distToPlayer <= orbitRadius + 1.0f)
            {
                state = State.OrbitFollow;
                ResetFollowTimer();
            }
        }
    }

    void FixedUpdate()
    {
        if (!player) return;

        Vector3 targetPos;

        if (state == State.OrbitFollow)
        {
            orbitAngleDeg += orbitSpeed * Time.fixedDeltaTime;

            // Orbit target point around the player
            Vector3 offset = Quaternion.Euler(0f, orbitAngleDeg, 0f) * Vector3.forward * orbitRadius;
            targetPos = player.position + offset;

            // Keep target on ground
            targetPos = SnapToGround(targetPos);
        }
        else if (state == State.Wander)
        {
            targetPos = wanderTarget;
        }
        else // ReturnToOrbit
        {
            // Return to a point behind/side of player rather than directly on top
            Vector3 behind = -player.forward * orbitRadius;
            targetPos = SnapToGround(player.position + behind);
        }

        MoveTowards(targetPos);
    }

    void MoveTowards(Vector3 targetPos)
    {
        Vector3 pos = rb.position;

        // Horizontal-only movement
        Vector3 toTarget = targetPos - pos;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        Vector3 desiredDir = (dist > 0.001f) ? (toTarget / dist) : Vector3.zero;

        // Speed: normal or catch-up if far away
        float speed = followSpeed;
        float distToPlayer = HorizontalDistance(pos, player.position);
        if (distToPlayer > catchUpDistance) speed *= catchUpMultiplier;

        // If close enough to target, slow down
        float targetSpeed = (dist <= stoppingDistance) ? 0f : speed;

        // Smooth accelerate
        Vector3 currentVel = rb.linearVelocity;
        Vector3 currentHorizVel = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 desiredVel = desiredDir * targetSpeed;
        Vector3 newHorizVel = Vector3.MoveTowards(currentHorizVel, desiredVel, acceleration * Time.fixedDeltaTime);

        // Apply velocity while preserving vertical physics (or keep y at 0 if you prefer)
        rb.linearVelocity = new Vector3(newHorizVel.x, rb.linearVelocity.y, newHorizVel.z);

        // Rotate to face movement direction (only if moving)
        if (newHorizVel.sqrMagnitude > 0.05f)
        {
            Quaternion targetRot = Quaternion.LookRotation(newHorizVel.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeedDegPerSec * Time.fixedDeltaTime));
        }
    }

    Vector3 SampleGroundNear(Vector3 center, float radius)
    {
        Vector2 r = Random.insideUnitCircle * radius;
        Vector3 p = center + new Vector3(r.x, 0f, r.y);
        return SnapToGround(p);
    }

    Vector3 SnapToGround(Vector3 p)
    {
        Vector3 rayStart = p + Vector3.up * groundRayHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        return p; // fallback if no ground hit
    }

    float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !player) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, orbitRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position, wanderRadius);

        if (state == State.Wander)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(wanderTarget + Vector3.up * 0.1f, 0.2f);
        }
    }
}
