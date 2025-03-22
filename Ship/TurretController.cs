using UnityEngine;

public class TurretController : MonoBehaviour
{
    [SerializeField] private WeaponConfigurationSO weaponConfig;
    
    private float nextFireTime;
    private Transform target;
    private SpriteRenderer spriteRenderer;
    private Animator muzzleFlashAnimator;

    public WeaponConfigurationSO WeaponConfig
    {
        get => weaponConfig;
        set => weaponConfig = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (weaponConfig != null)
        {
            InitializeTurret();
        }
    }

    public void InitializeTurret()
    {
        if (weaponConfig == null) return;

        // Setup sprite
        spriteRenderer.sprite = weaponConfig.turretSprite;
        spriteRenderer.sortingLayerName = "Turrets";
        spriteRenderer.sortingOrder = 2;

        // Setup muzzle flash if needed
        if (weaponConfig.muzzleFlashController != null)
        {
            GameObject muzzleFlash = new GameObject("MuzzleFlash");
            muzzleFlash.transform.SetParent(transform);
            muzzleFlash.transform.localPosition = Vector2.right * 0.5f;
            
            var muzzleRenderer = muzzleFlash.AddComponent<SpriteRenderer>();
            muzzleRenderer.sortingOrder = 3;
            
            muzzleFlashAnimator = muzzleFlash.AddComponent<Animator>();
            muzzleFlashAnimator.runtimeAnimatorController = weaponConfig.muzzleFlashController;
        }
    }

    private void Update()
    {
        if (target != null)
        {
            // Rotate towards target
            Vector2 direction = target.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                weaponConfig.rotationSpeed * Time.deltaTime
            );

            // Check if can fire
            if (Time.time >= nextFireTime && IsTargetInRange())
            {
                Fire();
            }
        }
    }

    private bool IsTargetInRange()
    {
        if (target == null) return false;
        return Vector2.Distance(transform.position, target.position) <= weaponConfig.range;
    }

    private void Fire()
    {
        nextFireTime = Time.time + 1f / weaponConfig.fireRate;
        
        // Spawn projectile
        // TODO: Use object pooling for better performance
        GameObject projectile = new GameObject("Projectile");
        projectile.transform.position = transform.position;
        projectile.transform.rotation = transform.rotation;
        
        var projectileScript = projectile.AddComponent<Projectile>();
        projectileScript.Initialize(weaponConfig.damage, weaponConfig.projectileSprite);
        
        // Publish firing event
        MessageBus.Instance.Publish(new GameEvent(
            GameEventType.WeaponFired,
            this,
            new { Damage = weaponConfig.damage, Position = transform.position }
        ));
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}