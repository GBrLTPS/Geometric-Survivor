using UnityEngine;

public class ArcadeGameManager : MonoBehaviour
{
    public static ArcadeGameManager Instance { get; private set; }

    [Header("Referências")]
    public ArcadeCharacter2D player;
    public ArcadeCharacterStats playerStats;
    public ArcadeLevelSystem levelSystem;
    public ArcadeEnemySpawner enemySpawner;

    [Header("Interface de Usuário (UI)")]
    public GameObject powerUpMenuUI;

    [Header("Configuração de Respawn")]
    [Tooltip("Se vazio, usará a posição inicial do jogador ao dar Play")]
    public Transform spawnPoint;
    private Vector3 initialSpawnPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (player == null) player = FindFirstObjectByType<ArcadeCharacter2D>();
        if (playerStats == null) playerStats = FindFirstObjectByType<ArcadeCharacterStats>();
        if (levelSystem == null) levelSystem = FindFirstObjectByType<ArcadeLevelSystem>();
        if (enemySpawner == null) enemySpawner = FindFirstObjectByType<ArcadeEnemySpawner>();
    }

    private void Start()
    {
        if (spawnPoint != null)
        {
            initialSpawnPosition = spawnPoint.position;
        }
        else if (player != null)
        {
            initialSpawnPosition = player.transform.position;
        }

        // Garanta que o menu comece fechado e o tempo ativo
        if (powerUpMenuUI != null) powerUpMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    // --- CONTROLE DE PAUSA E POWER-UPS ---

    public void OpenPowerUpMenu()
    {
        if (powerUpMenuUI != null) powerUpMenuUI.SetActive(true);
        
        // Congela o tempo global da física, inimigos e spawner
        Time.timeScale = 0f;

        // Desativa a movimentação do jogador
        if (player != null) player.SetControlState(false);
    }

    public void ClosePowerUpMenu()
    {
        if (powerUpMenuUI != null) powerUpMenuUI.SetActive(false);
        
        // Retorna o tempo normal da engine
        Time.timeScale = 1f;

        // Devolve o controle ao jogador
        if (player != null) player.SetControlState(true);
    }

    public void SetGameActiveState(bool active)
    {
        if (player != null)
        {
            player.SetControlState(active);
        }

        if (enemySpawner != null)
        {
            enemySpawner.enabled = active;
        }
    }

    // --- RESET DO JOGO ---

    public void ResetGame()
    {
        Debug.Log("<color=yellow><b>RESETANDO JOGO DO ZERO...</b></color>");

        // Descongela o tempo caso estivesse pausado no menu
        Time.timeScale = 1f;

        #if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
        #endif

        // 1. Destrói todos os inimigos vivos na cena
        ArcadeEnemy[] enemies = FindObjectsByType<ArcadeEnemy>(FindObjectsSortMode.None);
        foreach (ArcadeEnemy enemy in enemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        // 2. Destrói todos os projéteis restantes
        ArcadeProjectile[] bullets = FindObjectsByType<ArcadeProjectile>(FindObjectsSortMode.None);
        foreach (ArcadeProjectile bullet in bullets)
        {
            if (bullet != null && bullet.gameObject != null)
            {
                Destroy(bullet.gameObject);
            }
        }

        // 3. Reseta o jogador: reposiciona e zera a inércia da física 2D
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            player.transform.position = initialSpawnPosition;
            player.SetControlState(true);
        }

        // 4. Reseta a vida e atributos base do jogador
        if (playerStats != null)
        {
            playerStats.ResetToDefaults();
        }

        // 5. Reseta o sistema de níveis/XP
        if (levelSystem != null)
        {
            levelSystem.currentLevel = 1;
            levelSystem.currentXP = 0f;
            levelSystem.xpToNextLevel = 10f;
        }

        // 6. Reseta a dificuldade do Spawner
        if (enemySpawner != null)
        {
            enemySpawner.StopAllCoroutines();
            enemySpawner.CancelInvoke();
            enemySpawner.enabled = true;
        }
    }
}