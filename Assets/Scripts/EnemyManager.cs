using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyBehaviorConfig
{
    [Header("Movement Behavior")]
    public EnemyAI.MovementType movementType = EnemyAI.MovementType.Random;
    public float moveDuration = 2f;
    public Vector3[] waypoints;
    public bool loopWaypoints = true;
    
    [Header("Attack Behavior")]
    public EnemyAI.AttackType attackType = EnemyAI.AttackType.SingleShot;
    public int burstCount = 3;
    public float burstDelay = 0.1f;
    
    [Header("General Settings")]
    public float waitTime = 1f;
    public int health = 15;
    public Vector2 movementBounds = new Vector2(5.5f, 3f);
}

[System.Serializable]
public class EnemySpawn
{
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] Vector2 spawnPosition;
    [SerializeField] EnemyBehaviorConfig behaviorConfig = new EnemyBehaviorConfig();
    
    public GameObject EnemyPrefab => enemyPrefab;
    public Vector2 SpawnPosition => spawnPosition;
    public EnemyBehaviorConfig BehaviorConfig => behaviorConfig;
}

[System.Serializable]
public class Wave
{
    [SerializeField] string waveName;
    [SerializeField] EnemySpawn[] enemies;
    [SerializeField] float delayBeforeWave = 2f;
    
    public string WaveName => waveName;
    public EnemySpawn[] Enemies => enemies;
    public float DelayBeforeWave => delayBeforeWave;
}

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    
    [Header("Wave Configuration")]
    [SerializeField] Wave[] waves;
    [SerializeField] float delayBetweenSpawns = 0.5f;
    
    [Header("Default Enemy Behavior (fallback)")]
    [SerializeField] EnemyBehaviorConfig defaultBehaviorConfig = new EnemyBehaviorConfig();
    
    [Header("Spawn Animation")]
    [SerializeField] float spawnZ = 50f;      // Z position to spawn enemies at
    [SerializeField] float targetZ = 15f;     // Z position to lerp enemies to
    [SerializeField] float lerpDuration = 1f; // Time to lerp from spawn to target Z
    
    [Header("Debug")]
    [SerializeField] bool autoStartWaves = true;
    
    private int currentWaveIndex = 0;
    private List<GameObject> currentEnemies = new List<GameObject>();
    private bool isSpawningWave = false;
    private float waveStartTimer = 0f;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        if (autoStartWaves && waves.Length > 0)
        {
            StartNextWave();
        }
    }
    
    void Update()
    {
        // Check if current wave is complete
        if (!isSpawningWave && currentEnemies.Count == 0 && currentWaveIndex < waves.Length)
        {
            waveStartTimer -= Time.deltaTime;
            if (waveStartTimer <= 0f)
            {
                StartNextWave();
            }
        }
        
        // Clean up destroyed enemies from list
        currentEnemies.RemoveAll(enemy => enemy == null);
    }
    
    void StartNextWave()
    {
        if (currentWaveIndex >= waves.Length)
        {
            Debug.Log("All waves completed!");
            return;
        }
        
        Wave currentWave = waves[currentWaveIndex];
        Debug.Log($"Starting {currentWave.WaveName}");
        
        isSpawningWave = true;
        StartCoroutine(SpawnWaveCoroutine(currentWave));
    }
    
    System.Collections.IEnumerator SpawnWaveCoroutine(Wave wave)
    {
        foreach (EnemySpawn enemySpawn in wave.Enemies)
        {
            if (enemySpawn.EnemyPrefab != null)
            {
                Vector3 spawnPos = new Vector3(enemySpawn.SpawnPosition.x, enemySpawn.SpawnPosition.y, spawnZ);
                GameObject enemy = Instantiate(enemySpawn.EnemyPrefab, spawnPos, Quaternion.identity);
                
                // Configure enemy behavior from EnemyManager settings
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.enabled = false; // Disable initially
                    ApplyBehaviorConfig(enemyAI, enemySpawn.BehaviorConfig);
                }
                
                // Start lerp coroutine for this enemy
                StartCoroutine(LerpEnemyToPosition(enemy, spawnPos, new Vector3(spawnPos.x, spawnPos.y, targetZ)));
                
                currentEnemies.Add(enemy);
                
                yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }
        
        isSpawningWave = false;
        currentWaveIndex++;
        
        // Set timer for next wave
        if (currentWaveIndex < waves.Length)
        {
            waveStartTimer = waves[currentWaveIndex].DelayBeforeWave;
        }
    }
    
    void ApplyBehaviorConfig(EnemyAI enemyAI, EnemyBehaviorConfig config)
    {
        // Use default config as fallback if config is null
        if (config == null) config = defaultBehaviorConfig;
        
        // Apply configuration to the enemy AI
        enemyAI.SetBehaviorConfig(config);
    }
    
    System.Collections.IEnumerator LerpEnemyToPosition(GameObject enemy, Vector3 startPos, Vector3 endPos)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < lerpDuration && enemy != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / lerpDuration;
            
            // Use smooth step for more natural movement
            t = t * t * (3f - 2f * t);
            
            enemy.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            yield return null;
        }
        
        // Ensure final position is set
        if (enemy != null)
        {
            enemy.transform.position = endPos;
            
            // Enable EnemyAI after lerp is complete
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.enabled = true;
            }
        }
    }
    
    public void RegisterEnemyDestroyed(GameObject enemy)
    {
        currentEnemies.Remove(enemy);
        AudioManager.Instance.PlayExplosion();
    }
    
    public int GetCurrentWaveIndex() => currentWaveIndex;
    public int GetTotalWaves() => waves.Length;
    public int GetRemainingEnemies() => currentEnemies.Count;
}
