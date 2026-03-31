using System;
public static class Msg_Manager
{
    private static event Action Msg_Event;
    public static void SubscribeToMsg_Event(Action observer) => Msg_Event += observer;
    public static void UnsubscribeFromMsg_Event(Action observer) => Msg_Event -= observer;
    public static void InvokeMsg_Event() => Msg_Event?.Invoke();
}
