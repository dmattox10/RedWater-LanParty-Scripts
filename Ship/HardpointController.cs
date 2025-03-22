using UnityEngine;

public class HardpointController : MonoBehaviour
{
    private Hardpoint hardpointData;
    private TurretController mountedTurret;
    private SpriteRenderer baseRenderer;

    public Hardpoint Data => hardpointData;

    public void Initialize(Hardpoint data, Sprite baseSprite)
    {
        hardpointData = data;
        baseRenderer = gameObject.AddComponent<SpriteRenderer>();
        baseRenderer.sprite = baseSprite;
        baseRenderer.sortingLayerName = "Turrets";
        baseRenderer.sortingOrder = 0;
    }

    public bool MountWeapon(WeaponConfigurationSO weaponConfig)
    {
        if (mountedTurret != null || weaponConfig.requiredHardpointSize != hardpointData.size)
            return false;

        GameObject turretObj = new GameObject($"Turret_{weaponConfig.weaponName}");
        turretObj.transform.SetParent(transform, false);
        turretObj.transform.localPosition = Vector3.zero;

        mountedTurret = turretObj.AddComponent<TurretController>();
        mountedTurret.WeaponConfig = weaponConfig;
        mountedTurret.InitializeTurret();

        hardpointData.mountedWeaponId = weaponConfig.weaponName;
        return true;
    }

    public void UnmountWeapon()
    {
        if (mountedTurret != null)
        {
            Destroy(mountedTurret.gameObject);
            mountedTurret = null;
            hardpointData.mountedWeaponId = null;
        }
    }
}