using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArcadeHUDDisplay : MonoBehaviour
{
    [Header("Barra de Vida (HP)")]
    [Tooltip("Slider da Barra de Vida")]
    public Slider healthSlider;
    [Tooltip("Texto opcional da vida (ex: 100 / 100)")]
    public TextMeshProUGUI healthText;

    [Header("Barra de Experiência (XP)")]
    [Tooltip("Slider da Barra de XP")]
    public Slider xpSlider;
    [Tooltip("Texto do XP (ex: 1 / 10 XP)")]
    public TextMeshProUGUI xpText;

    [Header("Nível")]
    [Tooltip("Texto do Nível (ex: LVL 1)")]
    public TextMeshProUGUI levelText;

    [Header("Seguir Câmera")]
    public Camera arcadeCamera;
    public float distanceFromCamera = 5f;

    private ArcadeCharacterStats playerStats;
    private ArcadeLevelSystem levelSystem;

    private void Start()
    {
        playerStats = FindFirstObjectByType<ArcadeCharacterStats>();
        levelSystem = FindFirstObjectByType<ArcadeLevelSystem>();

        if (arcadeCamera == null)
        {
            arcadeCamera = Camera.main;
        }

        // Eventos de Vida
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthDisplay;
            UpdateHealthDisplay();
        }

        // Eventos de XP e Nível
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp += UpdateLevelDisplay;
            levelSystem.OnXPChanged += UpdateXPDisplay;
            UpdateLevelDisplay(levelSystem.currentLevel);
            UpdateXPDisplay(levelSystem.currentXP, levelSystem.xpToNextLevel);
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthDisplay;
        }

        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= UpdateLevelDisplay;
            levelSystem.OnXPChanged -= UpdateXPDisplay;
        }
    }

    private void LateUpdate()
    {
        if (transform.parent == null && arcadeCamera != null)
        {
            transform.position = arcadeCamera.transform.position + (arcadeCamera.transform.forward * distanceFromCamera);
            transform.rotation = arcadeCamera.transform.rotation;
        }
    }

    private void UpdateHealthDisplay()
    {
        if (playerStats == null) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = playerStats.maxHealth;
            healthSlider.value = playerStats.currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(playerStats.currentHealth)} / {Mathf.CeilToInt(playerStats.maxHealth)}";
        }
    }

    private void UpdateLevelDisplay(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"LVL {newLevel}";
        }
    }

    private void UpdateXPDisplay(float currentXP, float maxXP)
    {
        if (xpSlider != null)
        {
            xpSlider.maxValue = maxXP;
            xpSlider.value = currentXP;
        }

        if (xpText != null)
        {
            // Formato pedido: 1 / 10 XP
            xpText.text = $"{Mathf.FloorToInt(currentXP)} / {Mathf.CeilToInt(maxXP)} XP";
        }
    }
}