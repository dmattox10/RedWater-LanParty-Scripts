public enum GameEventType
{
    WeaponFired,
    DamageTaken,
    ShipDestroyed,
    HealthChanged
}

public class GameEvent
{
    public GameEventType Type { get; private set; }
    public object Sender { get; private set; }
    public object Data { get; private set; }

    public GameEvent(GameEventType type, object sender, object data = null)
    {
        Type = type;
        Sender = sender;
        Data = data;
    }
}