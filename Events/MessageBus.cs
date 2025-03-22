using System;
using System.Collections.Generic;
using UnityEngine;

public class MessageBus : MonoBehaviour
{
    private static MessageBus instance;
    private Dictionary<GameEventType, Action<GameEvent>> listeners = new Dictionary<GameEventType, Action<GameEvent>>();

    public static MessageBus Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("MessageBus");
                instance = go.AddComponent<MessageBus>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public void Subscribe(GameEventType eventType, Action<GameEvent> listener)
    {
        if (!listeners.ContainsKey(eventType))
        {
            listeners[eventType] = null;
        }
        listeners[eventType] += listener;
    }

    public void Unsubscribe(GameEventType eventType, Action<GameEvent> listener)
    {
        if (listeners.ContainsKey(eventType))
        {
            listeners[eventType] -= listener;
        }
    }

    public void Publish(GameEvent gameEvent)
    {
        if (listeners.ContainsKey(gameEvent.Type))
        {
            listeners[gameEvent.Type]?.Invoke(gameEvent);
        }
    }
}