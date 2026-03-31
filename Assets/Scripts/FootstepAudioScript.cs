using UnityEngine;

public class FootstepAudioScript : MonoBehaviour
{
    public void PlaySound()
    {
        SoundManager.PlaySound(SoundType.FOOTSTEP);
    }
}

