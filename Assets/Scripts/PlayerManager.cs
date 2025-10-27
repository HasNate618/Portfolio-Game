using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    float moveSpeed = 5f;
    [SerializeField]
    float acceleration = 20f;
    [SerializeField]
    float deceleration = 25f;
    [SerializeField]
    float maxSwayAngle = 20f; // Maximum rotation angle for sway
    [SerializeField]
    float swaySmooth = 8f;    // How quickly the rotation interpolates

    [Header("Movement Bounds")]
    [SerializeField]
    float xBounds = 5.5f;  // X movement bounds from center
    [SerializeField]
    float yBounds = 3f;    // Y movement bounds from center

    [Header("Mouse Follow Settings")]
    [SerializeField]
    Camera mainCamera;
    [SerializeField]
    float mouseSmoothness = 0.1f; // How smoothly to follow the cursor (lower = smoother)

    [Header("Health System")]
    [SerializeField]
    int maxHealth = 100;
    [SerializeField]
    int currentHealth;

    [Header("Shooting")]
    [SerializeField]
    Transform[] spawnPoints; // Assign 4 transforms in the inspector
    [SerializeField]
    GameObject projectilePrefab;
    [SerializeField]
    float shootingInterval = 0.2f;

    Vector2 velocity;
    float currentZRotation = 0f;

    float spawnTimer = 0f;
    int spawnIndex = 0;
    Vector3 targetPosition;

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Get main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        targetPosition = transform.position;
    }

    void Update()
    {
        // Get mouse position in world space
        if (mainCamera != null && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.transform.position.z - transform.position.z));
            
            // Keep the same Z position
            targetPosition = new Vector3(worldMousePos.x, worldMousePos.y, transform.position.z);
            
            // Clamp target position within bounds
            targetPosition.x = Mathf.Clamp(targetPosition.x, -xBounds, xBounds);
            targetPosition.y = Mathf.Clamp(targetPosition.y, -yBounds, yBounds);
        }

        // Store old position for actual movement calculation
        Vector3 oldPosition = transform.position;

        // Smoothly move towards mouse position
        Vector3 direction = targetPosition - transform.position;
        Vector2 targetVelocity = new Vector2(direction.x, direction.y) / mouseSmoothness;
        
        // Apply acceleration/deceleration
        if (direction.sqrMagnitude > 0.01f)
        {
            velocity = Vector2.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration * Time.deltaTime);
        }

        // Limit velocity to max speed
        velocity = Vector2.ClampMagnitude(velocity, moveSpeed);

        Vector3 move = new Vector3(velocity.x, velocity.y, 0f) * Time.deltaTime;
        Vector3 newPosition = transform.position + move;
        
        // Check bounds and clamp velocity if hitting bounds
        if (newPosition.x < -xBounds || newPosition.x > xBounds)
        {
            velocity.x = 0f; // Stop X velocity when hitting X bounds
            newPosition.x = Mathf.Clamp(newPosition.x, -xBounds, xBounds);
        }
        
        if (newPosition.y < -yBounds || newPosition.y > yBounds)
        {
            velocity.y = 0f; // Stop Y velocity when hitting Y bounds
            newPosition.y = Mathf.Clamp(newPosition.y, -yBounds, yBounds);
        }
        
        transform.position = newPosition;

        // Calculate actual movement delta for sway (not internal velocity)
        Vector3 actualMovement = transform.position - oldPosition;
        float actualXVelocity = actualMovement.x / Time.deltaTime;

        // Rotational sway based on actual horizontal movement
        float targetZ = -actualXVelocity / moveSpeed * maxSwayAngle;
        currentZRotation = Mathf.LerpAngle(currentZRotation, targetZ, Time.deltaTime * swaySmooth);
        transform.rotation = Quaternion.Euler(0f, 0f, currentZRotation);

        // Shoot on left mouse button hold
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && projectilePrefab != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                // Wrap index to available spawn points
                Transform spawnPoint = spawnPoints[spawnIndex % spawnPoints.Length];
                if (spawnPoint != null)
                {
                    Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
                }

                spawnIndex = (spawnIndex + 1) % spawnPoints.Length;
                spawnTimer = shootingInterval;
            }
        }
        else
        {
            spawnTimer = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check for enemy projectile collision
        if (other.CompareTag("EnemyProjectile"))
        {
            // Try to get damage from the projectile
            int damage = 1; // Default damage

            // Check if projectile has a damage component or field
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
        currentHealth = Mathf.Max(0, currentHealth); // Ensure health doesn't go negative

        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        // Add death logic here (restart level, show game over screen, etc.)
        
        // For now, just reset health (you can modify this behavior)
        currentHealth = maxHealth;
    }

    // Public methods for external access
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
}

// Simple component for enemy projectiles to specify damage
[System.Serializable]
public class EnemyProjectile : MonoBehaviour
{
    public int damage = 1;
}
