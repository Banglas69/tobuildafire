using UnityEngine;
public class Msg_Trigger : MonoBehaviour
{
    void Update() => Msg_Manager.InvokeMsg_Event();
}