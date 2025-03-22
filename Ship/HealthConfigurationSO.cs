using UnityEngine;

[CreateAssetMenu(fileName = "NewHealthConfiguration", menuName = "Ship/Health Configuration")]
public class HealthConfigurationSO : ScriptableObject
{
    [Header("Health Settings")]
    public float maxHealth = 150f;
    public float regenerationRate = 0f;
    public float regenerationDelay = 5f;
    public bool canRegenerate = false;
    
    [Header("Health Bar Sprites")]
    public Sprite greenSprite;
    public Sprite yellowSprite;
    public Sprite redSprite;
    public Sprite darkSprite;
    public Sprite letterD;
    public Sprite letterA;
    public Sprite letterN;
    public Sprite letterG;
    public Sprite letterE;
    public Sprite letterR;

    [Header("Visual Settings")]
    public float flashSpeed = 1f;
    public float criticalFlashSpeedMultiplier = 2f;
    public Vector2 healthBarOffset = new Vector2(0, 1.2f);
    public Vector2 segmentSize = new Vector2(0.32f, 0.1f);
}