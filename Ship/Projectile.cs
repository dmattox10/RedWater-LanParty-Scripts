using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    private float damage;
    private float speed = 10f;
    private float lifetime = 3f;
    private Vector2 direction;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        var collider = GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        Destroy(gameObject, lifetime);
    }

    public void Initialize(float projectileDamage, Sprite projectileSprite)
    {
        damage = projectileDamage;
        spriteRenderer.sprite = projectileSprite;
        direction = transform.right; // Use the object's right vector as forward direction
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var healthController = other.GetComponent<HealthController>();
        if (healthController != null)
        {
            healthController.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}