using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class Trail : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] int trailLength = 20;
    [SerializeField] float forwardSpeed = 5f;
    [SerializeField] float positionUpdateRate = 0.05f;
    [SerializeField] float curveSmoothness = 2f;

    LineRenderer lineRenderer;
    Queue<Vector3> trailPositions;
    Vector3 lastPosition;
    float updateTimer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        trailPositions = new Queue<Vector3>();
        lastPosition = transform.position;
        
        // Initialize trail with current position
        for (int i = 0; i < trailLength; i++)
        {
            Vector3 initialPos = transform.position - transform.forward * (i * 0.1f);
            trailPositions.Enqueue(initialPos);
        }
        
        UpdateLineRenderer();
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= positionUpdateRate)
        {
            UpdateTrail();
            updateTimer = 0f;
        }
    }

    void UpdateTrail()
    {
        Vector3 currentPosition = transform.position;
        Vector3 movementDelta = currentPosition - lastPosition;
        
        Vector3 forwardMovement = -transform.forward * forwardSpeed * positionUpdateRate;
        Vector3 newTrailPosition = currentPosition;
        
        trailPositions.Enqueue(newTrailPosition);
        
        if (trailPositions.Count > trailLength)
        {
            trailPositions.Dequeue();
        }
        
        Vector3[] positions = trailPositions.ToArray();
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] += forwardMovement;
            
            float curveInfluence = 1f - (float)i / positions.Length;
            positions[i] += movementDelta * curveInfluence * curveSmoothness;
        }
        
        trailPositions.Clear();
        foreach (Vector3 pos in positions)
        {
            trailPositions.Enqueue(pos);
        }
        
        lastPosition = currentPosition;
        UpdateLineRenderer();
    }

    void UpdateLineRenderer()
    {
        if (lineRenderer == null) return;
        
        Vector3[] positions = trailPositions.ToArray();
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
    }
}
