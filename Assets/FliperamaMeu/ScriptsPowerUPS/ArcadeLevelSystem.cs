using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcadeLevelSystem : MonoBehaviour
{
    [Header("Configuração de Níveis")]
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 10f;
    public float xpGrowthFactor = 1.35f;

    [Header("Lista Geral de Upgrades Disponíveis")]
    [Tooltip("Arraste aqui os arquivos de Upgrade criados na pasta Project")]
    public List<UpgradeData> availableUpgrades = new List<UpgradeData>();

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnXPChanged;

    private ArcadeCharacterStats stats;

    private void Awake()
    {
        stats = GetComponent<ArcadeCharacterStats>();
    }

    public void AddXP(float amount)
    {
        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel = Mathf.Round(xpToNextLevel * xpGrowthFactor);
            LevelUp();
        }

        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    private void LevelUp()
    {
        Debug.Log($"<color=cyan><b>LEVEL UP! Nível Atual: {currentLevel}</b></color>");
        
        OnLevelUp?.Invoke(currentLevel);

        // Abre o menu de Power-Up e pausa a física/jogo
        if (ArcadeGameManager.Instance != null)
        {
            ArcadeGameManager.Instance.OpenPowerUpMenu();
        }
    }

    public void ApplyUpgradeToPlayer(UpgradeData upgrade)
    {
        if (upgrade != null && stats != null)
        {
            upgrade.ApplyUpgrade(stats, currentLevel);
        }

        // Fecha o menu de Power-Up e despausa o jogo
        if (ArcadeGameManager.Instance != null)
        {
            ArcadeGameManager.Instance.ClosePowerUpMenu();
        }
    }
}