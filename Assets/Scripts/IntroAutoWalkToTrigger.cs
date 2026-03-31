using UnityEngine;

public class IntroAutoWalkToTrigger : MonoBehaviour
{
    [Header("Where to move")]
    public Transform movementRoot;          // the object that should move (usually PlayerRoot)
    public Transform forwardReference;      // usually your camera or player; defines "forward"
    public float moveSpeed = 2.0f;

    [Header("Stop condition")]
    public string triggerTag = "WatchTrigger";

    [Header("Disable these during auto-walk (movement + look scripts)")]
    public MonoBehaviour[] inputScriptsToDisable;

    [Header("What happens when we reach the trigger")]
    public WatchCutsceneController watchCutscene; // your existing cutscene script

    [Header("Options")]
    public bool startOnAwake = true;

    [Header("Freeze camera while auto-walking")]
    public Transform cameraPivot;        // assign CameraPivot here
    public bool freezeCameraRotation = true;

    public CameraFreeze cameraFreeze;


    Quaternion camStartLocalRot;


    CharacterController cc;
    bool autoWalking;

    void Awake()
    {
        if (!movementRoot) movementRoot = transform;
        if (!forwardReference) forwardReference = Camera.main ? Camera.main.transform : transform;

        cc = movementRoot.GetComponent<CharacterController>();

        if (startOnAwake)
            StartAutoWalk();
    }

    public void StartAutoWalk()
    {
        autoWalking = true;
        SetInputsEnabled(false);

        if (cameraFreeze)
        {
            cameraFreeze.Capture();
            cameraFreeze.freeze = true;
        }
    }


    public void StopAutoWalk()
{
    autoWalking = false;
    SetInputsEnabled(true);

    if (cameraFreeze)
        cameraFreeze.freeze = false;
}


    void Update()
    {
        if (!autoWalking) return;

        if (freezeCameraRotation && cameraPivot)
            cameraPivot.localRotation = camStartLocalRot;

        // Move forward on XZ plane
        Vector3 fwd = forwardReference.forward;
        fwd.y = 0f;
        fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : movementRoot.forward;

        Vector3 delta = fwd * (moveSpeed * Time.deltaTime);

        // Use CharacterController if present (best for collisions), otherwise move transform
        if (cc)
            cc.Move(delta);
        else
            movementRoot.position += delta;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!autoWalking) return;
        if (!other.CompareTag(triggerTag)) return;

        StopAutoWalk();

        if (watchCutscene)
            watchCutscene.PlayWatchCutscene();
        else
            Debug.LogWarning("IntroAutoWalkToTrigger: watchCutscene reference not set.", this);
    }

    void SetInputsEnabled(bool enabled)
    {
        foreach (var s in inputScriptsToDisable)
            if (s) s.enabled = enabled;
    }
}
