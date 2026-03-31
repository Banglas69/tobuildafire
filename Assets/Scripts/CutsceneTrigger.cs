using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class CutsceneTrigger : MonoBehaviour
{
    // public WatchCutsceneController cutscene;
    public PlayableDirector cutscene;
    public string playerTag = "Player";
    bool played;

    void OnTriggerEnter(Collider other)
    {
        if (played) return;
        if (!other.CompareTag(playerTag)) return;

        played = true;
        // cutscene.PlayWatchCutscene();
        cutscene.Play();
        Msg_Manager.InvokeMsg_Event();
    }
}
