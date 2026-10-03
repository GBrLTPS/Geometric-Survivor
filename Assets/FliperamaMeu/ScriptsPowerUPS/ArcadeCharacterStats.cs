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

    [Header("Sons e Efeitos de Áudio")]
    public AudioSource audioSource;
    public AudioClip takeDamageSound;
    public AudioClip healSound;
    public AudioClip lifeStealSound;
    public AudioClip upgradeSound;

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
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        ResetToDefaults();
    }

    private void Update()
    {
        // Regeneração de vida passiva por tempo (se ativa e não estiver com vida cheia)
        if (hpRegenPerSecond > 0f && currentHealth > 0 && currentHealth < maxHealth)
        {
            // Cura sem disparar o som a todo frame para não poluir o áudio
            SilentHeal(hpRegenPerSecond * Time.deltaTime);
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
    public void FullHeal() 
    { 
        Heal(maxHealth); 
    }

    public void Heal(float amount)
    {
        if (amount <= 0 || currentHealth >= maxHealth) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        PlaySound(healSound);
        OnHealthChanged?.Invoke();
    }

    // Cura silenciosa interna (para a regeneração passiva por segundo)
    private void SilentHeal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke();
    }

    public void OnEnemyKilled()
    {
        if (lifeStealOnKill > 0f)
        {
            SilentHeal(lifeStealOnKill);
            PlaySound(lifeStealSound);
        }
    }

    public void AddMaxHealth(float amount, bool healAmount = true)
    {
        maxHealth += amount;
        if (healAmount) currentHealth += amount;
        PlaySound(upgradeSound);
        OnHealthChanged?.Invoke();
    }

    public void AddHpRegen(float amount) { hpRegenPerSecond += amount; NotifyUpgrade(); }
    public void AddLifeStealOnKill(float amount) { lifeStealOnKill += amount; NotifyUpgrade(); }
    public void AddMoveSpeed(float amount) { moveSpeed += amount; NotifyUpgrade(); }
    public void AddJumpForce(float amount) { jumpForce += amount; NotifyUpgrade(); }
    public void AddExtraJumps(int count) { extraJumps += count; NotifyUpgrade(); }
    public void AddDashForce(float amount) { dashForce += amount; NotifyUpgrade(); }
    public void AddAttackDamage(float amount) { attackDamage += amount; NotifyUpgrade(); }
    public void AddAttackRate(float multiplier) { attackRate += multiplier; NotifyUpgrade(); }
    public void AddProjectilesPerShot(int count) { projectilesPerShot += count; NotifyUpgrade(); }
    public void AddPierceCount(int count) { pierceCount += count; NotifyUpgrade(); }
    public void AddBulletSpeed(float amount) { bulletSpeed += amount; NotifyUpgrade(); }
    public void AddPickupRange(float amount) { pickupRange += amount; NotifyUpgrade(); }

    private void NotifyUpgrade()
    {
        PlaySound(upgradeSound);
        OnStatsUpdated?.Invoke();
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        PlaySound(takeDamageSound);
        OnHealthChanged?.Invoke();

        if (currentHealth <= 0 && ArcadeGameManager.Instance != null)
        {
            ArcadeGameManager.Instance.ResetGame();
        }
    }

    private void PlaySound(AudioClip clip)
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
}