
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Setup")]
    public GameObject EnemyPrefab;
    public int StartingEnemyCount = 6;

    [Header("Player References")]
    public Transform Player;
    public PlayerHealth PlayerHealth;

    [Header("Spawn Points")]
    public Transform[] SpawnPoints;

    [Header("Raktabeej Mode")]
    public bool RaktabeejMode = true;
    public int EnemiesSpawnedPerKill = 2;
    public int MaximumActiveEnemies = 200;

    [Header("Enemy Count")]
    [SerializeField] private int activeEnemies = 0;

    public int ActiveEnemies => activeEnemies;

    private void Start()
    {
        if (EnemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy Prefab is not assigned.", this);
            return;
        }

        if (Player == null || PlayerHealth == null)
        {
            Debug.LogError(
                "EnemySpawner: Player or PlayerHealth is not assigned.", this);
            return;
        }

        if (SpawnPoints == null || SpawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner: No Spawn Points assigned.", this);
            return;
        }

        int enemiesToSpawn = Mathf.Min(
            StartingEnemyCount,
            MaximumActiveEnemies);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = SpawnPoints[i % SpawnPoints.Length];
            SpawnEnemy(spawnPoint);
        }
    }

    private bool SpawnEnemy(Transform spawnPoint)
    {
        if (spawnPoint == null || EnemyPrefab == null)
            return false;

        if (activeEnemies >= MaximumActiveEnemies)
            return false;

        GameObject newEnemy = Instantiate(
            EnemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation);

        EnemyHealth enemyHealth = newEnemy.GetComponent<EnemyHealth>();
        EnemyShooter enemyShooter = newEnemy.GetComponent<EnemyShooter>();

        AlienProximityVoice proximityVoice =
            newEnemy.GetComponentInChildren<AlienProximityVoice>();

        if (proximityVoice != null)
        {
            proximityVoice.Player = Player;
        }

        if (enemyHealth != null)
        {
            enemyHealth.Spawner = this;
        }
        else
        {
            Debug.LogError(
                "EnemySpawner: Enemy prefab has no EnemyHealth component.",
                newEnemy);
        }

        if (enemyShooter != null)
        {
            enemyShooter.player = Player;
            enemyShooter.playerHealth = PlayerHealth;
        }
        else
        {
            Debug.LogError(
                "EnemySpawner: Enemy prefab has no EnemyShooter component.",
                newEnemy);
        }

        activeEnemies++;

        Debug.Log($"ALIENS IN ARENA: {activeEnemies}");

        return true;
    }

    public void EnemyDied()
    {
        activeEnemies = Mathf.Max(0, activeEnemies - 1);

        Debug.Log($"ALIEN KILLED | ALIENS REMAINING: {activeEnemies}");

        if (!RaktabeejMode)
            return;

        for (int i = 0; i < EnemiesSpawnedPerKill; i++)
        {
            if (activeEnemies >= MaximumActiveEnemies)
                break;

            SpawnRandomEnemy();
        }
    }

    private void SpawnRandomEnemy()
    {
        if (SpawnPoints == null || SpawnPoints.Length == 0)
            return;

        Transform spawnPoint =
            SpawnPoints[Random.Range(0, SpawnPoints.Length)];

        SpawnEnemy(spawnPoint);
    }
}
