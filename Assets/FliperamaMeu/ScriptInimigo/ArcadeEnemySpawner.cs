using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public class EnemyConfig
{
    [HideInInspector] public string name; // Nome para identificação na lista
    
    [Header("Prefab e Liberação")]
    public GameObject prefab;
    [Tooltip("Nível mínimo do jogador para este inimigo começar a spawnar")]
    public int minLevelToSpawn = 1;

    [Tooltip("Peso/Proporção de spawn. Quanto maior o valor, mais comum ele é.")]
    [Range(0.1f, 100f)]
    public float spawnWeight = 10f;

    [Header("Atributos Base")]
    public float baseHealth = 20f;
    public float baseDamage = 10f;
    public float baseSpeed = 3f;
    public float baseXP = 5f;

    [Header("Escalonamento por Nível do Jogador")]
    public float healthPerLevel = 5f;
    public float damagePerLevel = 1.5f;
    public float speedPerLevel = 0.1f;
    public float xpPerLevel = 2f;
}

public class ArcadeEnemySpawner : MonoBehaviour
{
    [Header("Lista de Inimigos Configuráveis")]
    [Tooltip("Configure os prefabs, probabilidades e atributos individuais de cada inimigo.")]
    public List<EnemyConfig> enemyConfigs = new List<EnemyConfig>();

    [Header("Câmera Ortográfica do Fliperama")]
    [Tooltip("A câmera 2D ortográfica que filma a tela")]
    public Camera arcadeCamera;

    [Header("Posicionamento do Spawn (2D)")]
    [Tooltip("Distância horizontal mínima além do jogador no eixo X para spawnar")]
    public float minSpawnDistance = 14f;
    [Tooltip("Distância horizontal máxima além do jogador")]
    public float maxSpawnDistance = 22f;

    [Tooltip("Variação de altura no eixo Y em relação ao jogador")]
    public float minY = 0f;
    public float maxY = 6f;

    [Header("Controle de Ondas e Limites")]
    public float initialSpawnInterval = 2f;
    public float minimumSpawnInterval = 0.3f;
    public float difficultyIncreaseRate = 0.02f;

    [Header("Escalonamento de Quantidade de Inimigos")]
    public int baseMaxEnemies = 5;
    public int additionalEnemiesPerLevel = 2;
    public int absoluteMaxEnemies = 50;

    [Header("Despawn por Fora de Tela")]
    [Tooltip("Tempo em segundos fora da visão da câmera para o inimigo ser removido")]
    public float timeOutOfScreenToDespawn = 15f;

    private Transform playerTransform;
    private ArcadeCharacter2D character;
    private ArcadeLevelSystem levelSystem;
    private float currentSpawnInterval;
    private float nextSpawnTime;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<GameObject, float> outOfScreenTimers = new Dictionary<GameObject, float>();

    private void OnValidate()
    {
        // Atualiza os nomes no Inspector para facilitar a visualização da lista
        foreach (var config in enemyConfigs)
        {
            if (config.prefab != null)
            {
                config.name = $"{config.prefab.name} (Lvl Min: {config.minLevelToSpawn} | Peso: {config.spawnWeight})";
            }
            else
            {
                config.name = "[Prefab Não Atribuído]";
            }
        }
    }

    private void Start()
    {
        currentSpawnInterval = initialSpawnInterval;
        character = FindFirstObjectByType<ArcadeCharacter2D>();
        levelSystem = FindFirstObjectByType<ArcadeLevelSystem>();

        if (character != null)
        {
            playerTransform = character.transform;
        }

        if (arcadeCamera == null)
        {
            arcadeCamera = Camera.main;
        }
    }

    private void Update()
    {
        currentSpawnInterval = Mathf.Max(minimumSpawnInterval, currentSpawnInterval - (difficultyIncreaseRate * Time.deltaTime));

        if (Time.time >= nextSpawnTime)
        {
            TrySpawnEnemy();
            nextSpawnTime = Time.time + currentSpawnInterval;
        }

        CheckEnemiesOutOfScreen();
    }

    private int GetMaxEnemiesForCurrentLevel()
    {
        int currentLevel = (levelSystem != null) ? levelSystem.currentLevel : 1;
        int calculatedMax = baseMaxEnemies + ((currentLevel - 1) * additionalEnemiesPerLevel);
        return Mathf.Min(calculatedMax, absoluteMaxEnemies);
    }

    /// <summary>
    /// Seleciona um inimigo elegível com base no nível atual e no sistema de pesos (spawnWeight)
    /// </summary>
    private EnemyConfig SelectWeightedEnemyConfig(int currentLevel)
    {
        List<EnemyConfig> validConfigs = new List<EnemyConfig>();
        float totalWeight = 0f;

        // Filtra inimigos disponíveis para o nível atual
        foreach (var config in enemyConfigs)
        {
            if (config.prefab != null && currentLevel >= config.minLevelToSpawn)
            {
                validConfigs.Add(config);
                totalWeight += config.spawnWeight;
            }
        }

        if (validConfigs.Count == 0 || totalWeight <= 0f) return null;

        // Sorteio baseado no peso
        float randomValue = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var config in validConfigs)
        {
            currentSum += config.spawnWeight;
            if (randomValue <= currentSum)
            {
                return config;
            }
        }

        return validConfigs[0];
    }

    private void TrySpawnEnemy()
    {
        if (playerTransform == null) return;

        int currentLevel = (levelSystem != null) ? levelSystem.currentLevel : 1;

        EnemyConfig selectedConfig = SelectWeightedEnemyConfig(currentLevel);
        if (selectedConfig == null) return;

        activeEnemies.RemoveAll(enemy => enemy == null);

        int currentMaxEnemies = GetMaxEnemiesForCurrentLevel();
        if (activeEnemies.Count >= currentMaxEnemies) return;

        Vector3 spawnPos = CalculateOffscreenPosition();
        GameObject enemyObj = Instantiate(selectedConfig.prefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemyObj);

        int levelOffset = Mathf.Max(0, currentLevel - 1);

        // Calcula os atributos escalonados especificamente para este tipo de inimigo
        float finalHealth = selectedConfig.baseHealth + (levelOffset * selectedConfig.healthPerLevel);
        float finalDamage = selectedConfig.baseDamage + (levelOffset * selectedConfig.damagePerLevel);
        float finalSpeed = selectedConfig.baseSpeed + (levelOffset * selectedConfig.speedPerLevel);
        float finalXP = selectedConfig.baseXP + (levelOffset * selectedConfig.xpPerLevel);

        // 1. Aplicação via herança base ArcadeEnemy
        ArcadeEnemy baseEnemy = enemyObj.GetComponent<ArcadeEnemy>();
        if (baseEnemy != null)
        {
            baseEnemy.SetupScaledStats(finalHealth, finalSpeed, finalDamage, finalXP);
        }
        else
        {
            // 2. Fallback via Reflexão C# para scripts customizados (ex: ArcadeAgileEnemy)
            MonoBehaviour[] components = enemyObj.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour comp in components)
            {
                if (comp == null) continue;

                MethodInfo method = comp.GetType().GetMethod("SetupScaledStats");
                if (method != null)
                {
                    method.Invoke(comp, new object[] { finalHealth, finalSpeed, finalDamage, finalXP });
                    break;
                }
            }
        }
    }

    private void CheckEnemiesOutOfScreen()
    {
        if (arcadeCamera == null) return;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];

            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            Vector3 viewPos = arcadeCamera.WorldToViewportPoint(enemy.transform.position);

            bool isVisible = (viewPos.x >= 0f && viewPos.x <= 1f &&
                              viewPos.y >= 0f && viewPos.y <= 1f &&
                              viewPos.z > 0f);

            if (!isVisible)
            {
                if (!outOfScreenTimers.ContainsKey(enemy))
                {
                    outOfScreenTimers[enemy] = 0f;
                }

                outOfScreenTimers[enemy] += Time.deltaTime;

                if (outOfScreenTimers[enemy] >= timeOutOfScreenToDespawn)
                {
                    outOfScreenTimers.Remove(enemy);
                    activeEnemies.RemoveAt(i);
                    Destroy(enemy);
                }
            }
            else
            {
                if (outOfScreenTimers.ContainsKey(enemy))
                {
                    outOfScreenTimers.Remove(enemy);
                }
            }
        }
    }

    private Vector3 CalculateOffscreenPosition()
    {
        float side = Random.value > 0.5f ? 1f : -1f;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        float randomY = Random.Range(playerTransform.position.y + minY, playerTransform.position.y + maxY);

        float targetX = playerTransform.position.x + (side * distance);
        return new Vector3(targetX, randomY, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        Gizmos.color = Color.red;

        Vector3 centerRight = new Vector3(playerTransform.position.x + minSpawnDistance, playerTransform.position.y + (minY + maxY) * 0.5f, 0f);
        Vector3 centerLeft = new Vector3(playerTransform.position.x - minSpawnDistance, playerTransform.position.y + (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxSpawnDistance - minSpawnDistance, maxY - minY, 0f);

        Gizmos.DrawWireCube(centerRight, size);
        Gizmos.DrawWireCube(centerLeft, size);
    }
}