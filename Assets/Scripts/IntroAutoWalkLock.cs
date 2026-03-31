using UnityEngine;

public class IntroAutoWalkLock : MonoBehaviour
{
    [Header("Auto-walk")]
    public float moveSpeed = 2f;
    public Transform movementRoot;      // the object that moves (default: this)
    public Transform forwardReference;  // usually your camera or player forward (default: Camera.main)

    [Header("Stop trigger")]
    public string triggerTag = "WatchTrigger";

    [Header("Disable these while auto-walking")]
    public MonoBehaviour[] disableThese; // drag your movement + look + headbob scripts here

    [Header("Freeze camera")]
    public Transform cameraPivotToFreeze; // drag CameraPivot (or Main Camera if you don't have pivot)
    public bool freezeLocalPosition = false; // turn ON if headbob moves camera position too

    [Header("When we reach the trigger")]
    public WatchCutsceneController watchCutscene; // your existing cutscene script

    [Header("Start automatically")]
    public bool startOnStart = true;

    bool autoWalking;
    Vector3 savedLocalPos;
    Quaternion savedLocalRot;

    void Start()
    {
        if (!movementRoot) movementRoot = transform;
        if (!forwardReference) forwardReference = Camera.main ? Camera.main.transform : transform;

        if (startOnStart)
            BeginAutoWalk();
    }

    public void BeginAutoWalk()
    {
        autoWalking = true;

        // Disable inputs now (in Start timing) and keep them disabled while auto-walking
        ForceDisableInputs(true);

        // Capture camera pose to freeze
        if (cameraPivotToFreeze)
        {
            savedLocalPos = cameraPivotToFreeze.localPosition;
            savedLocalRot = cameraPivotToFreeze.localRotation;
        }
    }

    void Update()
    {
        if (!autoWalking) return;

        // IMPORTANT: keep scripts disabled even if something re-enables them
        ForceDisableInputs(true);

        // Move forward (XZ only)
        Vector3 fwd = forwardReference.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = movementRoot.forward;
        fwd.Normalize();

        movementRoot.position += fwd * (moveSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (!autoWalking) return;

        // Freeze camera after all other scripts run
        if (cameraPivotToFreeze)
        {
            if (freezeLocalPosition) cameraPivotToFreeze.localPosition = savedLocalPos;
            cameraPivotToFreeze.localRotation = savedLocalRot;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!autoWalking) return;
        if (!other.CompareTag(triggerTag)) return;

        autoWalking = false;

        // Do NOT re-enable inputs here; your watch cutscene script will handle re-enabling at the end
        if (watchCutscene) watchCutscene.PlayWatchCutscene();
    }

    void ForceDisableInputs(bool disable)
    {
        if (disableThese == null) return;

        for (int i = 0; i < disableThese.Length; i++)
        {
            var s = disableThese[i];
            if (!s) continue;
            s.enabled = !disable ? true : false;
        }
    }
}
