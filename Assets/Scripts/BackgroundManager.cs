using UnityEngine;
using System.Collections.Generic;

public class BackgroundManager : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] GameObject backgroundPrefab;
    [SerializeField] int backgroundCount = 10;
    [SerializeField] float spacing = 10f;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float resetZPosition = 50f;
    [SerializeField] float frontZPosition = 0f;
    
    [Header("Optional Settings")]
    [SerializeField] bool autoStart = true;
    [SerializeField] Vector3 spawnOffset = Vector3.zero;
    
    private List<GameObject> backgroundObjects = new List<GameObject>();
    private bool isMoving = false;
    
    void Start()
    {
        if (autoStart)
        {
            InitializeBackground();
        }
    }
    
    void Update()
    {
        if (isMoving)
        {
            MoveBackgroundObjects();
        }
    }
    
    public void InitializeBackground()
    {
        if (backgroundPrefab == null)
        {
            Debug.LogWarning("Background prefab is not assigned!");
            return;
        }
        
        // Clear existing background objects
        ClearBackground();
        
        // Spawn background objects evenly spaced
        for (int i = 0; i < backgroundCount; i++)
        {
            float zPos = resetZPosition - (i * spacing / GameConfig.playerSpeedMultiplier);
            Vector3 spawnPos = new Vector3(spawnOffset.x, spawnOffset.y, zPos);
            
            GameObject bgObj = Instantiate(backgroundPrefab, spawnPos, Quaternion.identity, transform);
            backgroundObjects.Add(bgObj);
        }
        
        isMoving = true;
        //Debug.Log($"Initialized {backgroundCount} background objects");
    }
    
    void MoveBackgroundObjects()
    {
        for (int i = backgroundObjects.Count - 1; i >= 0; i--)
        {
            if (backgroundObjects[i] == null)
            {
                backgroundObjects.RemoveAt(i);
                continue;
            }
            
            GameObject bgObj = backgroundObjects[i];
            
            // Move towards the front (use Vector3.back for negative Z movement)
            Vector3 moveDirection = moveSpeed > 0 ? Vector3.back : Vector3.forward;
            bgObj.transform.position += moveDirection * Mathf.Abs(moveSpeed) * Time.deltaTime * GameConfig.playerSpeedMultiplier;
            
            // Check if object has reached or passed the front position
            bool shouldReset = moveSpeed > 0 ? 
                bgObj.transform.position.z <= frontZPosition : 
                bgObj.transform.position.z >= frontZPosition;
                
            if (shouldReset)
            {
                // Find the furthest back object to determine new position
                float furthestZ = GetFurthestBackZPosition();
                
                // Reset position to the back
                Vector3 newPos = bgObj.transform.position;
                if (moveSpeed > 0)
                {
                    newPos.z = furthestZ + spacing; // For positive speed, add spacing
                }
                else
                {
                    newPos.z = furthestZ - spacing; // For negative speed, subtract spacing
                }
                bgObj.transform.position = newPos;
            }
        }
    }
    
    float GetFurthestBackZPosition()
    {
        if (backgroundObjects.Count == 0) return resetZPosition;
        
        float furthestZ = moveSpeed > 0 ? float.MinValue : float.MaxValue;
        
        foreach (GameObject bgObj in backgroundObjects)
        {
            if (bgObj != null)
            {
                if (moveSpeed > 0)
                {
                    if (bgObj.transform.position.z > furthestZ)
                        furthestZ = bgObj.transform.position.z;
                }
                else
                {
                    if (bgObj.transform.position.z < furthestZ)
                        furthestZ = bgObj.transform.position.z;
                }
            }
        }
        
        return furthestZ;
    }
    
    public void StartMovement()
    {
        isMoving = true;
    }
    
    public void StopMovement()
    {
        isMoving = false;
    }
    
    public void ClearBackground()
    {
        foreach (GameObject bgObj in backgroundObjects)
        {
            if (bgObj != null)
            {
                if (Application.isPlaying)
                    Destroy(bgObj);
                else
                    DestroyImmediate(bgObj);
            }
        }
        backgroundObjects.Clear();
    }
    
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
}
