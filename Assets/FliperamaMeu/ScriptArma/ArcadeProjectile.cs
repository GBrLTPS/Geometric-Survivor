using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ArcadeProjectile : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    public float lifetime = 4f;
    public LayerMask hitLayers = ~0; // Camadas com que o projétil colide

    private float damage;
    private int pierceCount;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Tiro retilíneo
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true; // Trigger para atravessar ou dar dano limpo
    }

    public void Setup(Vector2 direction, float speed, float bulletDamage, int pierce)
    {
        damage = bulletDamage;
        pierceCount = pierce;

        rb.linearVelocity = direction.normalized * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignora colisão com o próprio jogador
        if (other.GetComponentInParent<ArcadeCharacter2D>() != null) return;

        // Tenta aplicar dano se o alvo tiver uma interface de vida (IDamageable)
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        pierceCount--;
        if (pierceCount < 0)
        {
            Destroy(gameObject);
        }
    }
}

// Interface para os inimigos/alvos
public interface IDamageable
{
    void TakeDamage(float amount);
}