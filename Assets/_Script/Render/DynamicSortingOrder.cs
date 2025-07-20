using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicDepthByZ : MonoBehaviour
{
    private float zStepPerY = 0.1f;
    private float zStepPerX = 0.001f;
    private float zStepPerLayer = 0.05f; 
    private float baseZ = 0.01f;
    private int layerIndex;
    public bool runOnce = false;
    

    private Bounds worldBounds;
    private bool boundsInitialized = false;


    void Start()
    {
        InitializeWorldBounds();
        UpdateLayerIndex();

    }

    void LateUpdate()
    {
        if (!boundsInitialized)
        {
            InitializeWorldBounds();
            boundsInitialized = true;
        }

        if (runOnce && Application.isPlaying)
        {
            UpdateDepth();
            enabled = false; 
        }
        else
        {
            UpdateDepth();
        }
    }

    void InitializeWorldBounds()
    {
        var tilemap = GameObject.FindWithTag("Ground")?.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        if (tilemap != null)
        {
            worldBounds = tilemap.localBounds;
            boundsInitialized = true;
        }
        else
        {
            worldBounds = new Bounds(Vector3.zero, new Vector3(100, 100, 0));
            boundsInitialized = true;
        }
    }

    void UpdateDepth()
    {
        Vector3 pos = transform.position;
        float minY = worldBounds.min.y; 
        float minX = worldBounds.min.x; 

        float yDistance = pos.y - minY; 
        float xDistance = pos.x - minX; 

        float newZ = baseZ + (yDistance * zStepPerY) + (xDistance * zStepPerX) - (layerIndex * zStepPerLayer);

        transform.position = new Vector3(pos.x, pos.y, newZ);
    }

    [ContextMenu("Update Depth Now")]
    public void UpdateDepthManually()
    {
        if (!boundsInitialized)
        {
            InitializeWorldBounds();
        }
        UpdateDepth();
    }

    private void UpdateLayerIndex()
    {
        if (CompareTag("Tree"))
        {
            layerIndex = GetComponent<Tree>()?.layerIndex ?? 0;
        }
        else if (CompareTag("Bush"))
        {
            layerIndex = GetComponent<Bush>()?.layerIndex ?? 0;
        }
        else if (CompareTag("Rock"))
        {
            layerIndex = GetComponent<Rock>()?.layerIndex ?? 0;
        }
        else if (CompareTag("Building"))
        {
            layerIndex = GetComponent<Building>()?.LayerIndex ?? 0;
        }
        else
        {
            layerIndex = 0;
        }
    }
}