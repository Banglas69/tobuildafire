using UnityEngine;

public class StickCollectible : MonoBehaviour
{
    // Prevent double-register if something calls pickup twice
    public bool IsCollected { get; private set; }

    public void MarkCollected()
    {
        IsCollected = true;
    }
}
