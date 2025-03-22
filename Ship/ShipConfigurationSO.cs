using UnityEngine;

[CreateAssetMenu(fileName = "New Ship Configuration", menuName = "Ships/Ship Configuration")]
public class ShipConfigurationSO : ScriptableObject
{
    [Header("Ship Properties")]
    public ShipClass shipClass;
    public ShipStats stats;

    [Header("Sprites")]
    public Sprite friendlySprite;
    public Sprite enemySprite;
    public Sprite friendlyDestroyedSprite;  // Add these if needed
    public Sprite enemyDestroyedSprite;     // Add these if needed

    [Header("Wake")]
    public RuntimeAnimatorController wakeController;
    public Vector2 wakeForwardOffset = new Vector2(0, -0.14f);
    public Vector2 wakeReverseOffset = new Vector2(0, 0.25f);

    [Header("Hardpoints")]
    public Hardpoint[] hardpoints;
}