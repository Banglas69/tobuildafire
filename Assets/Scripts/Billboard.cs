using UnityEngine;

public class BillboardY : MonoBehaviour
{
    Camera cam;

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        Vector3 lookPos = cam.transform.position;
        lookPos.y = transform.position.y; // rotate only around Y
        transform.LookAt(lookPos);
    }
}
