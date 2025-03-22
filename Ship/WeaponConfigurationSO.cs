using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Configuration", menuName = "Ships/Weapon Configuration")]
public class WeaponConfigurationSO : ScriptableObject
{
    [Header("Weapon Properties")]
    public string weaponName;
    public HardpointSize requiredHardpointSize;
    public float fireRate = 1f;
    public float damage = 10f;
    public float range = 10f;
    public float rotationSpeed = 90f; // degrees per second

    [Header("Visuals")]
    public Sprite turretSprite;
    public Sprite projectileSprite;
    public RuntimeAnimatorController muzzleFlashController;
}