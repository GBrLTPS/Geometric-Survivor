using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeWeaponShooter : MonoBehaviour
{
    [Header("Prefab e Ponto de Saída")]
    [Tooltip("Prefab da bala (Sprite/Objeto 2D com ArcadeProjectile)")]
    public GameObject projectilePrefab;
    
    [Tooltip("Ponto vazio na ponta do cano da arma de onde a bala surge")]
    public Transform firePoint;

    [Header("Atributos Base de Tiro (Fallback sem Stats)")]
    public float baseBulletSpeed = 16f;
    public float baseDamage = 10f;
    public float baseFireRate = 2f; // Tiros por segundo
    public int baseProjectilesPerShot = 1; // Quantidade de balas por tiro
    public float baseSpreadAngle = 12f; // Abertura em graus para múltiplos tiros
    public int basePierce = 0; // Quantos inimigos perfura antes de sumir

    [Header("Inputs VR (Gatilho da mão para atirar)")]
    [Tooltip("Botão para atirar contínuo ou manual")]
    public InputAction shootAction = new InputAction("Shoot", InputActionType.Button);

    [Header("Modo de Disparo")]
    [Tooltip("Se marcado, atira sozinho no fireRate (estilo clássico Survivor-like)")]
    public bool autoFire = false;

    private ArcadeCharacter2D character;
    private ArcadeCharacterStats stats;
    private float nextFireTime = 0f;

    // Propriedades dinâmicas vinculadas aos Stats (com fallback para as variáveis base)
    private float Damage => stats != null ? stats.attackDamage : baseDamage;
    private float FireRate => stats != null ? stats.attackRate : baseFireRate;
    private int ProjectilesPerShot => stats != null ? stats.projectilesPerShot : baseProjectilesPerShot;
    private int PierceCount => stats != null ? stats.pierceCount : basePierce;
    private float BulletSpeed => stats != null ? stats.bulletSpeed : baseBulletSpeed;

    private void Awake()
    {
        character = GetComponentInParent<ArcadeCharacter2D>();
        stats = GetComponentInParent<ArcadeCharacterStats>();

        if (firePoint == null) firePoint = transform;
    }

    private void OnEnable()
    {
        shootAction.Enable();
    }

    private void OnDisable()
    {
        shootAction.Disable();
    }

    private void Update()
    {
        // Se o fliperama não estiver ativo, bloqueia o disparo
        if (character != null && !character.isPlaying) return;

        bool isTryingToShoot = autoFire || 
                               shootAction.IsPressed() || 
                               (Mouse.current != null && Mouse.current.leftButton.isPressed);

        if (isTryingToShoot && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + (1f / Mathf.Max(0.05f, FireRate));
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        int count = Mathf.Max(1, ProjectilesPerShot);
        float startAngle = -(count - 1) * (baseSpreadAngle * 0.5f);

        // Direção base no plano 2D (Eixo XY do firePoint)
        Vector2 baseDirection = firePoint.right;

        for (int i = 0; i < count; i++)
        {
            float currentSpread = (count == 1) ? 0f : (startAngle + (i * baseSpreadAngle));
            Vector2 shootDir = Quaternion.Euler(0f, 0f, currentSpread) * baseDirection;

            // Instancia o projétil rotacionado na direção 2D
            float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
            GameObject bulletObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0f, 0f, angle));
            
            ArcadeProjectile projectile = bulletObj.GetComponent<ArcadeProjectile>();

            if (projectile != null)
            {
                projectile.Setup(shootDir, BulletSpeed, Damage, PierceCount);
            }
        }
    }
}