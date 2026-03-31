using UnityEngine;

public class WatchCutsceneTrigger : MonoBehaviour
{
    public WatchCutsceneController cutscene;
    public string playerTag = "Player";
    bool played;

    void OnTriggerEnter(Collider other)
    {
        if (played) return;
        if (!other.CompareTag(playerTag)) return;

        played = true;
        cutscene.PlayWatchCutscene();
        Msg_Manager.InvokeMsg_Event();
    }
}
