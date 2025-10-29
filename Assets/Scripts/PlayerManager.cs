using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Mouse Follow Settings")]
    [SerializeField] Camera mainCamera;
    [SerializeField] float followSpeed = 10f; // How fast the player follows the cursor (higher = more responsive)
    [SerializeField] [Range(0f, 1f)] float smoothing = 0.15f; // Smoothing factor (0 = instant, 1 = very smooth)
    [SerializeField] bool useAbsolutePosition = false; // If true, player moves directly to cursor; if false, uses smooth interpolation
    
    [Header("Movement Bounds")]
    [SerializeField] float xBounds = 5.5f;
    [SerializeField] float yBounds = 3f;
    
    [Header("Visual Feedback")]
    [SerializeField] float maxSwayAngle = 20f;
    [SerializeField] float swaySpeed = 8f;
    
    [Header("Health System")]
    [SerializeField] int maxHealth = 100;
    [SerializeField] int currentHealth;

    [Header("Shooting")]
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] float shootingInterval = 0.2f;

    // Private variables
    private Vector3 currentVelocity; // For SmoothDamp
    private Vector3 mouseWorldPosition;
    private float currentZRotation;
    private float spawnTimer;
    private int spawnIndex;
    private ParticleSystem[] cachedSpawnPointParticles; // Cached particle systems for spawn points

    void Start()
    {
        currentHealth = maxHealth;
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        mouseWorldPosition = transform.position;
        spawnTimer = 0f;
        
        // Cache particle systems from spawn points
        CacheSpawnPointParticles();
    }

    void CacheSpawnPointParticles()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            cachedSpawnPointParticles = new ParticleSystem[0];
            return;
        }

        cachedSpawnPointParticles = new ParticleSystem[spawnPoints.Length];

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                cachedSpawnPointParticles[i] = spawnPoints[i].GetComponent<ParticleSystem>();
            }
        }
    }

    void Update()
    {
        UpdateMousePosition();
        UpdatePlayerMovement();
        UpdateRotationSway();
        HandleShooting();
    }

    void UpdateMousePosition()
    {
        if (mainCamera == null || Mouse.current == null) return;

        // Get mouse screen position
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        
        // Convert to world position on the player's Z plane
        Vector3 mouseWorldPos3D = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Mathf.Abs(mainCamera.transform.position.z - transform.position.z)
        ));
        
        // Store as target position with clamped bounds
        mouseWorldPosition = new Vector3(
            Mathf.Clamp(mouseWorldPos3D.x, -xBounds, xBounds),
            Mathf.Clamp(mouseWorldPos3D.y, -yBounds, yBounds),
            transform.position.z
        );
    }

    void UpdatePlayerMovement()
    {
        Vector3 targetPosition = mouseWorldPosition;
        Vector3 newPosition;

        if (useAbsolutePosition)
        {
            // Direct movement with lerp
            newPosition = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            // Smooth damped movement (more natural feel)
            newPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                smoothing,
                followSpeed
            );
        }

        // Apply final clamped position
        transform.position = new Vector3(
            Mathf.Clamp(newPosition.x, -xBounds, xBounds),
            Mathf.Clamp(newPosition.y, -yBounds, yBounds),
            transform.position.z
        );
    }

    void UpdateRotationSway()
    {
        // Calculate sway based on horizontal velocity
        float horizontalVelocity = currentVelocity.x;
        float targetRotation = -horizontalVelocity * maxSwayAngle;
        
        currentZRotation = Mathf.LerpAngle(currentZRotation, targetRotation, swaySpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, currentZRotation);
    }

    void HandleShooting()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed || projectilePrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnTimer = 0f;
            return;
        }

        spawnTimer -= Time.deltaTime;
        
        if (spawnTimer <= 0f)
        {
            int currentSpawnIndex = spawnIndex % spawnPoints.Length;
            Transform spawnPoint = spawnPoints[currentSpawnIndex];
            
            if (spawnPoint != null)
            {
                // Instantiate the projectile
                Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
                
                // Play particle effect at the spawn point using cached particle system
                PlaySpawnPointEffect(currentSpawnIndex);
                
                // Play shoot sound effect
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPlayerShoot();
                }
            }

            spawnIndex = (spawnIndex + 1) % spawnPoints.Length;
            spawnTimer = shootingInterval;
        }
    }

    void PlaySpawnPointEffect(int spawnPointIndex)
    {
        // Use cached particle system instead of doing GetComponent every time
        if (spawnPointIndex >= 0 && spawnPointIndex < cachedSpawnPointParticles.Length)
        {
            ParticleSystem particleSystem = cachedSpawnPointParticles[spawnPointIndex];
            
            if (particleSystem != null)
            {
                // Play the particle effect
                particleSystem.Play();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemyProjectile"))
        {
            int damage = 1;

            EnemyProjectile enemyProj = other.GetComponent<EnemyProjectile>();
            if (enemyProj != null)
            {
                damage = enemyProj.damage;
            }

            TakeDamage(damage);
            Destroy(other.gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");

        AudioManager.Instance.PlayPlayerHit();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        currentHealth = maxHealth;
    }

    // Public API
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
}

[System.Serializable]
public class EnemyProjectile : MonoBehaviour
{
    public int damage = 1;
}
