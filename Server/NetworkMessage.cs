using UnityEngine;
using System;

[Serializable]
public enum MessageType
{
    PlayerJoined,
    PlayerInput,
    GameState,
    ShipUpgrade,
    WeaponUpgrade,
    AbilityUse,
    PlayerLeft,
    Error
}

[Serializable]
public class NetworkMessage
{
    public string type;  // Public for JSON serialization
    public string playerId;
    public string data;

    public NetworkMessage(MessageType messageType, string playerId = null, string data = null)
    {
        type = messageType.ToString();
        this.playerId = playerId;
        this.data = data;
    }

    public MessageType Type 
    {
        get => string.IsNullOrEmpty(type) ? MessageType.Error : Enum.Parse<MessageType>(type);
    }
}

[Serializable]
public class PlayerInputData
{
    public float horizontal;
    public float vertical;
    public float timestamp;
}

[Serializable]
public class GameStateData
{
    public ShipState[] ships;
    public long serverTime;
}

[Serializable]
public class ShipState
{
    public string playerId;
    public Vector2Position position;
    public float rotation;
    public string shipClass;
    public bool isEnemy;
    public string[] activeWeapons;
    public bool[] abilitiesUnlocked;
}

[Serializable]
public class Vector2Position
{
    public float x;
    public float y;

    public static implicit operator Vector2(Vector2Position v) => new Vector2(v.x, v.y);
    public static implicit operator Vector2Position(Vector2 v) => new Vector2Position { x = v.x, y = v.y };
}