using UnityEngine;

[DefaultExecutionOrder(10000)] // run very late
public class CameraFreeze : MonoBehaviour
{
    public bool freeze = false;
    public bool lockLocalPosition = false; // turn ON if headbob/sway moves camera position
    public bool lockLocalRotation = true;  // keep ON to stop looking around

    Vector3 savedLocalPos;
    Quaternion savedLocalRot;

    void OnEnable()
    {
        savedLocalPos = transform.localPosition;
        savedLocalRot = transform.localRotation;
    }

    public void Capture()
    {
        savedLocalPos = transform.localPosition;
        savedLocalRot = transform.localRotation;
    }

    void LateUpdate()
    {
        if (!freeze) return;

        if (lockLocalPosition) transform.localPosition = savedLocalPos;
        if (lockLocalRotation) transform.localRotation = savedLocalRot;
    }

    // If something (like Cinemachine) still overrides in LateUpdate, this catches it right before render
    void OnPreCull()
    {
        if (!freeze) return;

        if (lockLocalPosition) transform.localPosition = savedLocalPos;
        if (lockLocalRotation) transform.localRotation = savedLocalRot;
    }
}
