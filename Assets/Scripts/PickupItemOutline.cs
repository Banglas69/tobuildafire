using UnityEngine;

[RequireComponent(typeof(AutoOutline))]
public class PickupItem : MonoBehaviour
{
    private AutoOutline outline;

    private void Awake()
    {
        outline = GetComponent<AutoOutline>();
    }

    public void SetHighlight(bool state)
    {
        if (state)
            outline.FadeIn();
        else
            outline.FadeOut();
    }

    public void Pickup()
    {
        Destroy(gameObject);
    }
}
