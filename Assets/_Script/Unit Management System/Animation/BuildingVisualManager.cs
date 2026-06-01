using _Script.Enum;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BuildingVisualManager : MonoBehaviour
{
    [Header("Sprites Configuration")] 
    [SerializeField] private Sprite underConstructionSprite;
    [SerializeField] private Sprite completedSprite;
    [SerializeField] private Sprite destroyedSprite;

    private Building building;
    private SpriteRenderer spriteRenderer;
    private BuildingState lastState;

    private void Awake()
    {
        building = GetComponent<Building>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (building == null)
            Debug.LogError($"[BuildingVisualManager] Không tìm thấy script Building trên {gameObject.name}!");

        if (building != null) lastState = building.buildingState;
    }

    private void Start()
    {
        UpdateBuildingSprite();
    }

    private void Update()
    {
        if (building == null) return;

        if (building.buildingState != lastState)
        {
            lastState = building.buildingState;
            UpdateBuildingSprite();
        }
    }

    public void UpdateBuildingSprite()
    {
        Sprite targetSprite = null;

        switch (building.buildingState)
        {
            case BuildingState.UnderConstruction:
                targetSprite = underConstructionSprite;
                break;
            case BuildingState.Completed:
                targetSprite = completedSprite;
                break;
            case BuildingState.Destroyed:
                targetSprite = destroyedSprite;
                break;
        }

        if (targetSprite != null)
            spriteRenderer.sprite = targetSprite;
        else
            Debug.LogWarning(
                $"[BuildingVisualManager] Chưa gán Sprite cho trạng thái {building.buildingState} ở {gameObject.name}");
    }
}