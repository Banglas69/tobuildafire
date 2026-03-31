using UnityEngine;
public class Msg_Observer : MonoBehaviour
{   
    void SomeAction() 
    {
    Debug.Log("some action is happening");
    }
    void OnEnable() => Msg_Manager.SubscribeToMsg_Event(SomeAction);
    void OnDisable() => Msg_Manager.UnsubscribeFromMsg_Event(SomeAction);
}
