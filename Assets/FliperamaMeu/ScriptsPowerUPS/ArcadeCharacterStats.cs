using System;
using UnityEngine;

public class ArcadeCharacterStats : MonoBehaviour
{
    [Header("Vida e Sobrevivência")]
    public float baseMaxHealth = 100f;
    public float baseHpRegenPerSecond = 0f; // Regeneração por tempo
    public float baseLifeStealOnKill = 0f;  // Regeneração ao matar

    [Header("Mobilidade")]
    public float baseMoveSpeed = 5f;
    public float baseJumpForce = 8f;
    public int baseExtraJumps = 1;
    public float baseDashForce = 14f;

    [Header("Ataque e Disparos")]
    public float baseAttackDamage = 10f;
    public float baseAttackRate = 1f; // Tiros por segundo
    public int baseProjectilesPerShot = 1; // Balas por disparo (Spread)
    public int basePierceCount = 0; // Quantidade de inimigos perfurados
    public float baseBulletSpeed = 16f;

    [Header("Utilitários")]
    public float basePickupRange = 2.5f;

    // Atributos Atuais Dinâmicos
    public float currentHealth { get; private set; }
    public float maxHealth { get; private set; }
    public float hpRegenPerSecond { get; private set; }
    public float lifeStealOnKill { get; private set; }
    public float moveSpeed { get; private set; }
    public float jumpForce { get; private set; }
    public int extraJumps { get; private set; }
    public float dashForce { get; private set; }
    public float attackDamage { get; private set; }
    public float attackRate { get; private set; }
    public int projectilesPerShot { get; private set; }
    public int pierceCount { get; private set; }
    public float bulletSpeed { get; private set; }
    public float pickupRange { get; private set; }

    public event Action OnHealthChanged;
    public event Action OnStatsUpdated;

    private void Awake()
    {
        ResetToDefaults();
    }

    private void Update()
    {
        // Regeneração de vida passiva por tempo (se ativa e não estiver com vida cheia)
        if (hpRegenPerSecond > 0f && currentHealth > 0 && currentHealth < maxHealth)
        {
            Heal(hpRegenPerSecond * Time.deltaTime);
        }
    }

    public void ResetToDefaults()
    {
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
        hpRegenPerSecond = baseHpRegenPerSecond;
        lifeStealOnKill = baseLifeStealOnKill;

        moveSpeed = baseMoveSpeed;
        jumpForce = baseJumpForce;
        extraJumps = baseExtraJumps;
        dashForce = baseDashForce;

        attackDamage = baseAttackDamage;
        attackRate = baseAttackRate;
        projectilesPerShot = baseProjectilesPerShot;
        pierceCount = basePierceCount;
        bulletSpeed = baseBulletSpeed;

        pickupRange = basePickupRange;

        OnStatsUpdated?.Invoke();
        OnHealthChanged?.Invoke();
    }

    // Métodos de Cura e Modificação de Atributos
    public void FullHeal() { Heal(maxHealth); }
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke();
    }

    public void OnEnemyKilled()
    {
        if (lifeStealOnKill > 0f)
        {
            Heal(lifeStealOnKill);
        }
    }

    public void AddMaxHealth(float amount, bool healAmount = true)
    {
        maxHealth += amount;
        if (healAmount) currentHealth += amount;
        OnHealthChanged?.Invoke();
    }

    public void AddHpRegen(float amount) { hpRegenPerSecond += amount; OnStatsUpdated?.Invoke(); }
    public void AddLifeStealOnKill(float amount) { lifeStealOnKill += amount; OnStatsUpdated?.Invoke(); }
    public void AddMoveSpeed(float amount) { moveSpeed += amount; OnStatsUpdated?.Invoke(); }
    public void AddJumpForce(float amount) { jumpForce += amount; OnStatsUpdated?.Invoke(); }
    public void AddExtraJumps(int count) { extraJumps += count; OnStatsUpdated?.Invoke(); }
    public void AddDashForce(float amount) { dashForce += amount; OnStatsUpdated?.Invoke(); }
    public void AddAttackDamage(float amount) { attackDamage += amount; OnStatsUpdated?.Invoke(); }
    public void AddAttackRate(float multiplier) { attackRate += multiplier; OnStatsUpdated?.Invoke(); }
    public void AddProjectilesPerShot(int count) { projectilesPerShot += count; OnStatsUpdated?.Invoke(); }
    public void AddPierceCount(int count) { pierceCount += count; OnStatsUpdated?.Invoke(); }
    public void AddBulletSpeed(float amount) { bulletSpeed += amount; OnStatsUpdated?.Invoke(); }
    public void AddPickupRange(float amount) { pickupRange += amount; OnStatsUpdated?.Invoke(); }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke();

        if (currentHealth <= 0 && ArcadeGameManager.Instance != null)
        {
            ArcadeGameManager.Instance.ResetGame();
        }
    }
}