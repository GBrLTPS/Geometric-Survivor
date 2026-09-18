using UnityEngine;

public enum UpgradeType
{
    // Cura e Sobrevivência
    InstantFullHeal,       // Enche a vida na hora 100%
    InstantHealAmount,     // Cura um valor fixo imediato (ex: 50 HP)
    HpRegenPerSecond,      // Regeneração de HP com o tempo
    LifeStealOnKill,       // Cura ao matar inimigo
    MaxHealth,             // Aumenta a vida máxima

    // Movimentação
    MoveSpeed,             // Velocidade de corrida
    ExtraJump,             // +1 ou +2 Pulos no ar
    JumpForce,             // Pulo mais alto
    DashForce,             // Dash mais forte

    // Ataque e Projéteis
    AttackDamage,          // Dano da bala
    AttackRate,            // Cadência (tiros por segundo)
    ProjectilesPerShot,    // Múltiplos tiros ao mesmo tempo (Spread/Leque)
    PierceCount,           // Quantos inimigos o tiro atravessa
    BulletSpeed,           // Velocidade da bala

    // Utilitário
    PickupRange,           // Ímã de XP
    Custom
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Survivor/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Identificação do Power-up")]
    public string upgradeName = "Novo Power-up";
    [TextArea(2, 4)] public string description = "Descrição do efeito.";
    public Sprite icon;
    public int maxLevel = 5;

    [Header("Tipo de Melhoria")]
    public UpgradeType type;
    [Tooltip("Valor aplicado por cada nível da melhoria")]
    public float valuePerLevel = 1f;

    public virtual void ApplyUpgrade(ArcadeCharacterStats stats, int currentLevel)
    {
        switch (type)
        {
            case UpgradeType.InstantFullHeal:
                stats.FullHeal();
                break;
            case UpgradeType.InstantHealAmount:
                stats.Heal(valuePerLevel);
                break;
            case UpgradeType.HpRegenPerSecond:
                stats.AddHpRegen(valuePerLevel);
                break;
            case UpgradeType.LifeStealOnKill:
                stats.AddLifeStealOnKill(valuePerLevel);
                break;
            case UpgradeType.MaxHealth:
                stats.AddMaxHealth(valuePerLevel);
                break;
            case UpgradeType.MoveSpeed:
                stats.AddMoveSpeed(valuePerLevel);
                break;
            case UpgradeType.ExtraJump:
                stats.AddExtraJumps((int)valuePerLevel);
                break;
            case UpgradeType.JumpForce:
                stats.AddJumpForce(valuePerLevel);
                break;
            case UpgradeType.DashForce:
                stats.AddDashForce(valuePerLevel);
                break;
            case UpgradeType.AttackDamage:
                stats.AddAttackDamage(valuePerLevel);
                break;
            case UpgradeType.AttackRate:
                stats.AddAttackRate(valuePerLevel);
                break;
            case UpgradeType.ProjectilesPerShot:
                stats.AddProjectilesPerShot((int)valuePerLevel);
                stats.AddPierceCount((int)valuePerLevel);
                break;
            case UpgradeType.PierceCount:
                stats.AddPierceCount((int)valuePerLevel);
                break;
            case UpgradeType.BulletSpeed:
                stats.AddBulletSpeed(valuePerLevel);
                break;
            case UpgradeType.PickupRange:
                stats.AddPickupRange(valuePerLevel);
                break;
            case UpgradeType.Custom:
                break;
        }

        Debug.Log($"<color=green>Upgrade Aplicado:</color> {upgradeName} (Nível {currentLevel})");
    }
}