using UnityEngine;

public class Background : MonoBehaviour
{
    [SerializeField] float scrollSpeed = 0.1f;
    Material mat;
    float offsetY = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Use .material to get an instance for this object
            mat = renderer.material;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (mat != null)
        {
            offsetY += scrollSpeed * Time.deltaTime;
            Vector2 offset = mat.mainTextureOffset;
            offset.y = offsetY;
            mat.mainTextureOffset = offset;
        }
    }
}
