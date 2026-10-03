using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ArcadeAgileEnemy : MonoBehaviour, IDamageable
{
    [Header("Atributos do Inimigo Ágil")]
    public float maxHealth = 15f;
    public float speed = 5.5f;
    public float xpReward = 8f;

    [Header("Ataque ao Jogador")]
    public float attackDamage = 10f;
    public float damageCooldown = 0.4f;
    protected float nextDamageTime = 0f;

    [Header("Mecânica Única: Teleporte Zig-Zag")]
    [Tooltip("Distância horizontal para qual o inimigo se teleporta ao acertar o jogador")]
    public float teleportDistance = 4f;
    [Tooltip("Som opcional tocado no momento do teleporte")]
    public AudioClip teleportSound;

    [Header("Comportamento em Plataformas e Pulo")]
    public float jumpForce = 12f;
    [Tooltip("Distância vertical em Y para o inimigo considerar que o player está em um andar acima")]
    public float heightDifferenceToJump = 1.2f;
    [Tooltip("Tempo em segundos parado antes de tentar o pulo por travamento")]
    public float stuckThresholdTime = 0.25f;
    [Tooltip("Intervalo mínimo entre pulos")]
    public float jumpCooldown = 0.5f;
    [Tooltip("Velocidade mínima para considerar que está andando")]
    public float movementThreshold = 0.05f;

    [Header("Checagem de Chão e Plataformas (Raycasts 2D)")]
    public float groundCheckDistance = 0.6f;
    public float edgeCheckAheadDistance = 0.6f;
    public LayerMask groundLayer = ~0;

    [Header("Impacto / Feedback")]
    public float knockbackForce = 2f;
    public float flashDuration = 0.08f;
    public Color damageFlashColor = Color.white;

    [Header("Sons e Efeitos de Áudio")]
    public AudioClip hitSound;
    public AudioClip dieSound;
    public AudioSource audioSource;

    protected float currentHealth;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected ArcadeCharacterStats cachedPlayerStats;
    protected ArcadeLevelSystem cachedLevelSystem;

    private SpriteRenderer spriteRenderer;
    private Renderer genericRenderer;
    private Color originalColor = Color.white;
    private Coroutine flashRoutine;

    // Variáveis de controle
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private float nextJumpTime = 0f;
    private bool isGrounded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            genericRenderer = GetComponentInChildren<Renderer>();
            if (genericRenderer != null)
            {
                originalColor = genericRenderer.material.color;
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        currentHealth = maxHealth;
    }

    private void Start()
    {
        ArcadeCharacter2D player = FindFirstObjectByType<ArcadeCharacter2D>();
        if (player != null)
        {
            playerTransform = player.transform;
            cachedPlayerStats = player.GetComponent<ArcadeCharacterStats>();
        }

        cachedLevelSystem = FindFirstObjectByType<ArcadeLevelSystem>();
        lastPosition = transform.position;
    }

    public virtual void SetupScaledStats(float hp, float moveSpeed, float damage, float xp)
    {
        maxHealth = hp;
        currentHealth = hp;
        speed = moveSpeed;
        attackDamage = damage;
        xpReward = xp;
    }

    private void FixedUpdate()
    {
        // 1. Se o tempo do jogo estiver pausado (Menu de Power-Up / Pause)
        if (Time.timeScale == 0f)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // 2. Se o jogador estiver com isPlaying == false (Game Over / Início de Fase)
        if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.player != null)
        {
            if (!ArcadeGameManager.Instance.player.isPlaying)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        if (playerTransform == null) return;

        CheckGrounded();

        // Persegue o player na horizontal
        float dir = Mathf.Sign(playerTransform.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

        HandlePlatformNavigation(dir);
        CheckIfStuck();
    }

    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    private void HandlePlatformNavigation(float moveDir)
    {
        if (!isGrounded || Time.time < nextJumpTime) return;

        float deltaY = playerTransform.position.y - transform.position.y;

        if (deltaY > heightDifferenceToJump)
        {
            if (HasEdgeAhead(moveDir) || IsBlockedAhead(moveDir))
            {
                Jump();
                return;
            }

            if (Mathf.Abs(playerTransform.position.x - transform.position.x) < 2.5f)
            {
                Jump();
                return;
            }
        }
    }

    private bool HasEdgeAhead(float moveDir)
    {
        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(moveDir * edgeCheckAheadDistance, 0f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance + 0.5f, groundLayer);
        return hit.collider == null;
    }

    private bool IsBlockedAhead(float moveDir)
    {
        Vector2 forwardDir = new Vector2(moveDir, 0f);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, forwardDir, 0.7f, groundLayer);
        return hit.collider != null;
    }

    private void CheckIfStuck()
    {
        float distanceMoved = Mathf.Abs(transform.position.x - lastPosition.x);

        if (distanceMoved < movementThreshold * Time.fixedDeltaTime)
        {
            stuckTimer += Time.fixedDeltaTime;

            if (stuckTimer >= stuckThresholdTime && Time.time >= nextJumpTime && isGrounded)
            {
                Jump();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        nextJumpTime = Time.time + jumpCooldown;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        PlaySound(hitSound);

        if (gameObject.activeInHierarchy)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(DamageFlash());
        }

        if (playerTransform != null)
        {
            float pushDir = Mathf.Sign(transform.position.x - playerTransform.position.x);
            rb.linearVelocity = new Vector2(pushDir * knockbackForce, rb.linearVelocity.y + 1f);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        SetRendererColor(damageFlashColor);
        yield return new WaitForSeconds(flashDuration);
        SetRendererColor(originalColor);
        flashRoutine = null;
    }

    private void SetRendererColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        else if (genericRenderer != null)
        {
            genericRenderer.material.color = color;
        }
    }

    private void Die()
    {
        if (dieSound != null)
        {
            AudioSource.PlayClipAtPoint(dieSound, transform.position);
        }

        if (cachedPlayerStats != null)
        {
            cachedPlayerStats.OnEnemyKilled();
        }

        if (cachedLevelSystem != null)
        {
            cachedLevelSystem.AddXP(xpReward);
        }

        Destroy(gameObject);
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDamageAndTeleport(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealDamageAndTeleport(collision.gameObject);
    }

    private void TryDealDamageAndTeleport(GameObject target)
    {
        if (Time.time < nextDamageTime) return;

        ArcadeCharacterStats stats = target.GetComponentInParent<ArcadeCharacterStats>();
        if (stats != null)
        {
            // Causa dano no jogador
            stats.TakeDamage(attackDamage);
            nextDamageTime = Time.time + damageCooldown;

            // Teleporta o inimigo para o outro lado do jogador
            PerformSideTeleport(stats.transform);
        }
    }

    private void PerformSideTeleport(Transform pTransform)
    {
        if (pTransform == null) return;

        // Descobre de qual lado do jogador ele está (1 = direita, -1 = esquerda)
        float currentSide = Mathf.Sign(transform.position.x - pTransform.position.x);

        // Inverte o lado (-1 vira 1, e 1 vira -1)
        float targetSide = -currentSide;

        // Calcula a nova posição X mantendo a altura Y atual
        Vector3 newPosition = new Vector3(
            pTransform.position.x + (targetSide * teleportDistance),
            transform.position.y,
            transform.position.z
        );

        transform.position = newPosition;

        // Aplica impulso na nova direção para ele continuar correndo em direção ao player
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(-targetSide * speed, rb.linearVelocity.y);
        }

        PlaySound(teleportSound);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector2.down * groundCheckDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector2.right * 0.7f);
    }
}