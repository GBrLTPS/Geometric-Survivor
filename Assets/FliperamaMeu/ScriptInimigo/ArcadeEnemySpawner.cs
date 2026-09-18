using System.Collections.Generic;
using UnityEngine;

public class ArcadeEnemySpawner : MonoBehaviour
{
    [Header("Prefab do Inimigo")]
    [Tooltip("Arraste o prefab do quadrado vermelho (deve ter Rigidbody2D)")]
    public GameObject enemyPrefab;

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
    
    [Header("Escalonamento de Quantidade por Nível")]
    public int baseMaxEnemies = 5;
    public int additionalEnemiesPerLevel = 2;
    public int absoluteMaxEnemies = 50;

    [Header("Despawn por Fora de Tela")]
    [Tooltip("Tempo em segundos fora da visão da câmera para o inimigo ser removido")]
    public float timeOutOfScreenToDespawn = 15f;

    [Header("Escalonamento por Nível do Jogador (Dificuldade)")]
    public float healthIncreasePerLevel = 6f;
    public float damageIncreasePerLevel = 1.5f;
    public float speedIncreasePerLevel = 0.15f;
    public float xpRewardIncreasePerLevel = 2f;

    private Transform playerTransform;
    private ArcadeCharacter2D character;
    private ArcadeLevelSystem levelSystem;
    private float currentSpawnInterval;
    private float nextSpawnTime;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<GameObject, float> outOfScreenTimers = new Dictionary<GameObject, float>();

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

    private void TrySpawnEnemy()
    {
        if (enemyPrefab == null || playerTransform == null) return;

        activeEnemies.RemoveAll(enemy => enemy == null);

        int currentMaxEnemies = GetMaxEnemiesForCurrentLevel();
        if (activeEnemies.Count >= currentMaxEnemies) return;

        Vector3 spawnPos = CalculateOffscreenPosition();
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemyObj);

        ArcadeEnemy enemy = enemyObj.GetComponent<ArcadeEnemy>();
        if (enemy != null)
        {
            int currentLevel = (levelSystem != null) ? levelSystem.currentLevel : 1;
            int levelOffset = Mathf.Max(0, currentLevel - 1);
            float scaledHp = enemy.maxHealth + (levelOffset * healthIncreasePerLevel);
            float scaledDamage = enemy.attackDamage + (levelOffset * damageIncreasePerLevel);
            float scaledSpeed = enemy.speed + (levelOffset * speedIncreasePerLevel);
            float scaledXP = enemy.xpReward + (levelOffset * xpRewardIncreasePerLevel);

            enemy.SetupScaledStats(scaledHp, scaledSpeed, scaledDamage, scaledXP);
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