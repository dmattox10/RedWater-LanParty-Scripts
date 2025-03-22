using UnityEngine;
using UnityEngine.UI;

public class HealthController : MonoBehaviour
{
    [SerializeField] private HealthConfigurationSO healthConfig;
    private ComplexHealthBar healthBar;
    
    private float currentHealth;
    private float lastDamageTime;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => healthConfig.maxHealth;

    private void Awake()
    {
        currentHealth = healthConfig.maxHealth;
        CreateHealthBar();
    }

    private void CreateHealthBar()
    {
        var healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(transform);
        healthBarObj.transform.localPosition = Vector3.zero;
        healthBar = healthBarObj.AddComponent<ComplexHealthBar>();
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        lastDamageTime = Time.time;
        
        MessageBus.Instance.Publish(new GameEvent(
            GameEventType.HealthChanged,
            this,
            new { CurrentHealth = currentHealth, MaxHealth = healthConfig.maxHealth }
        ));

        healthBar.UpdateHealthBar(currentHealth, healthConfig.maxHealth);

        if (currentHealth <= 0)
        {
            MessageBus.Instance.Publish(new GameEvent(GameEventType.ShipDestroyed, this));
        }
    }

    private void Update()
    {
        if (healthConfig.canRegenerate && 
            currentHealth < healthConfig.maxHealth && 
            Time.time > lastDamageTime + healthConfig.regenerationDelay)
        {
            currentHealth = Mathf.Min(
                healthConfig.maxHealth,
                currentHealth + (healthConfig.regenerationRate * Time.deltaTime)
            );
            healthBar.UpdateHealthBar(currentHealth, healthConfig.maxHealth);
        }
    }
}