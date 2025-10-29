using UnityEngine;
using System.Collections;

// Interface for movement behaviors
public interface IMovementBehavior
{
    IEnumerator ExecuteMovement(Transform enemyTransform, Vector2 bounds);
}

// Interface for attack behaviors
public interface IAttackBehavior
{
    void ExecuteAttack(Transform firePoint, GameObject projectilePrefab);
}

// Random movement behavior
[System.Serializable]
public class RandomMovementBehavior : IMovementBehavior
{
    [SerializeField] private float moveDuration = 2f;
    
    public RandomMovementBehavior(float duration = 2f)
    {
        moveDuration = duration;
    }
    
    public IEnumerator ExecuteMovement(Transform enemyTransform, Vector2 bounds)
    {
        float randomX = Random.Range(-bounds.x, bounds.x);
        float randomY = Random.Range(-bounds.y, bounds.y);
        Vector3 targetPosition = new Vector3(randomX, randomY, enemyTransform.position.z);
        
        Vector3 startPosition = enemyTransform.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < moveDuration && enemyTransform != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            t = t * t * (3f - 2f * t); // Smooth curve
            
            enemyTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        if (enemyTransform != null)
            enemyTransform.position = targetPosition;
    }
}

// Sequential waypoint movement behavior
[System.Serializable]
public class WaypointMovementBehavior : IMovementBehavior
{
    [SerializeField] private Vector3[] waypoints;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private bool loop = true;
    private int currentWaypointIndex = 0;
    
    public WaypointMovementBehavior(Vector3[] points, float duration = 2f, bool shouldLoop = true)
    {
        waypoints = points;
        moveDuration = duration;
        loop = shouldLoop;
    }
    
    public IEnumerator ExecuteMovement(Transform enemyTransform, Vector2 bounds)
    {
        if (waypoints == null || waypoints.Length == 0) yield break;
        
        Vector3 targetPosition = waypoints[currentWaypointIndex];
        targetPosition.z = enemyTransform.position.z; // Keep same Z
        
        Vector3 startPosition = enemyTransform.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < moveDuration && enemyTransform != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            t = t * t * (3f - 2f * t);
            
            enemyTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        if (enemyTransform != null)
            enemyTransform.position = targetPosition;
        
        // Move to next waypoint
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
        {
            if (loop)
                currentWaypointIndex = 0;
            else
                currentWaypointIndex = waypoints.Length - 1;
        }
    }
}

// Basic single shot attack
[System.Serializable]
public class SingleShotAttack : IAttackBehavior
{
    public void ExecuteAttack(Transform firePoint, GameObject projectilePrefab)
    {
        if (projectilePrefab == null) return;
        
        Vector3 shootDirection = Vector3.back;
        GameObject projectile = Object.Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));
        
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript == null)
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = shootDirection * 10f;
        }

        AudioManager.Instance.PlayEnemyShoot();
    }
}

// Burst fire attack
[System.Serializable]
public class BurstFireAttack : IAttackBehavior
{
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.1f;
    
    public BurstFireAttack(int count = 3, float delay = 0.1f)
    {
        burstCount = count;
        burstDelay = delay;
    }
    
    public void ExecuteAttack(Transform firePoint, GameObject projectilePrefab)
    {
        if (projectilePrefab == null) return;
        
        MonoBehaviour mb = firePoint.GetComponent<MonoBehaviour>();
        if (mb != null)
            mb.StartCoroutine(BurstCoroutine(firePoint, projectilePrefab));
    }

    private IEnumerator BurstCoroutine(Transform firePoint, GameObject projectilePrefab)
    {
        for (int i = 0; i < burstCount; i++)
        {
            Vector3 shootDirection = Vector3.back;
            GameObject projectile = Object.Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));
            
            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript == null)
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.linearVelocity = shootDirection * 10f;
            }

            AudioManager.Instance.PlayEnemyShoot();

            if (i < burstCount - 1)
                yield return new WaitForSeconds(burstDelay);
        }
    }
}

public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] int health = 15;
    
    [Header("Combat")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float waitTime = 1f;
    
    [Header("Movement")]
    [SerializeField] Vector2 movementBounds = new Vector2(4.9f, 2.5f);
    
    [Header("Behavior Selection")]
    [SerializeField] MovementType movementType = MovementType.Random;
    [SerializeField] AttackType attackType = AttackType.SingleShot;
    
    [Header("Waypoint Movement (if using waypoints)")]
    [SerializeField] Vector3[] waypoints;
    [SerializeField] float moveDuration = 2f;
    [SerializeField] bool loopWaypoints = true;
    
    [Header("Burst Attack Settings (if using burst)")]
    [SerializeField] int burstCount = 3;
    [SerializeField] float burstDelay = 0.1f;
    
    [Header("Destruction")]
    [SerializeField] GameObject destructionEffect;
    
    private bool isActive = false;
    private bool isCycleRunning = false;
    private EnemyState currentState = EnemyState.Waiting;
    
    private IMovementBehavior movementBehavior;
    private IAttackBehavior attackBehavior;
    
    public enum MovementType { Random, Waypoints }
    public enum AttackType { SingleShot, BurstFire }
    
    enum EnemyState
    {
        Waiting,
        Shooting,
        Moving
    }
    
    void Start()
    {
        isActive = enabled;
        InitializeBehaviors();
        
        if (isActive)
        {
            StartBehaviorCycle();
        }
    }

    // Method to configure behavior from EnemyManager
    public void SetBehaviorConfig(EnemyBehaviorConfig config)
    {
        if (config == null) return;

        // Apply configuration values
        movementType = config.movementType;
        attackType = config.attackType;
        moveDuration = config.moveDuration;
        waypoints = config.waypoints;
        loopWaypoints = config.loopWaypoints;
        burstCount = config.burstCount;
        burstDelay = config.burstDelay;
        waitTime = config.waitTime;
        health = config.health;
        movementBounds = config.movementBounds;

        // Reinitialize behaviors with new config
        InitializeBehaviors();
    }

    void InitializeBehaviors()
    {
        // Initialize movement behavior
        switch (movementType)
        {
            case MovementType.Random:
                movementBehavior = new RandomMovementBehavior(moveDuration);
                break;
            case MovementType.Waypoints:
                movementBehavior = new WaypointMovementBehavior(waypoints, moveDuration, loopWaypoints);
                break;
        }
        
        // Initialize attack behavior
        switch (attackType)
        {
            case AttackType.SingleShot:
                attackBehavior = new SingleShotAttack();
                break;
            case AttackType.BurstFire:
                attackBehavior = new BurstFireAttack(burstCount, burstDelay);
                break;
        }
    }
    
    void OnEnable()
    {
        isActive = true;
        StartBehaviorCycle();
    }
    
    void OnDisable()
    {
        isActive = false;
        isCycleRunning = false;
        StopAllCoroutines();
    }
    
    void StartBehaviorCycle()
    {
        if (!isActive || isCycleRunning) return;
        
        currentState = EnemyState.Waiting;
        isCycleRunning = true;
        StartCoroutine(BehaviorCycle());
    }
    
    System.Collections.IEnumerator BehaviorCycle()
    {
        while (isActive && gameObject != null)
        {
            switch (currentState)
            {
                case EnemyState.Waiting:
                    yield return new WaitForSeconds(waitTime);
                    currentState = EnemyState.Shooting;
                    break;
                    
                case EnemyState.Shooting:
                    ExecuteAttack();
                    yield return new WaitForSeconds(0.5f);
                    currentState = EnemyState.Moving;
                    break;
                    
                case EnemyState.Moving:
                    yield return StartCoroutine(ExecuteMovement());
                    currentState = EnemyState.Waiting;
                    break;
            }
        }
        
        isCycleRunning = false;
    }
    
    void ExecuteAttack()
    {
        Transform shootFrom = firePoint != null ? firePoint : transform;
        attackBehavior?.ExecuteAttack(shootFrom, projectilePrefab);
    }
    
    IEnumerator ExecuteMovement()
    {
        if (movementBehavior != null)
            yield return StartCoroutine(movementBehavior.ExecuteMovement(transform, movementBounds));
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (!isActive) return;
        
        health -= damage;
        if (health <= 0)
        {
            DestroyEnemy();
        }
    }
    
    void DestroyEnemy()
    {
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, transform.rotation);
        }
        
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemyDestroyed(gameObject);
        }
        
        Destroy(gameObject);
    }
}
