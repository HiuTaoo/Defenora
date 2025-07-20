using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RegionObject : MonoBehaviour
{
    [Header("Region Settings")]
    [SerializeField] private bool autoRegister = true;
    [SerializeField] private bool keepAnimatorEnabled = false; 

    private RegionManager regionManager;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isRegistered = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (autoRegister)
        {
            RegisterToRegion();
        }
    }

    public void RegisterToRegion()
    {
        if (isRegistered) return;

        regionManager = FindObjectOfType<RegionManager>();
        if (regionManager != null)
        {
            regionManager.RegisterObject(gameObject);
            isRegistered = true;
        }
        else
        {
            Debug.LogWarning($"RegionManager not found for {gameObject.name}");
        }
    }

    public void UnregisterFromRegion()
    {
        if (!isRegistered) return;

        if (regionManager != null)
        {
            regionManager.UnregisterObject(gameObject);
            isRegistered = false;
        }
    }

    void OnDestroy()
    {
        UnregisterFromRegion();
    }

    public void OnRegionActivated()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        if (animator != null && !keepAnimatorEnabled)
            animator.enabled = true;
    }

    public void OnRegionDeactivated()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (animator != null && !keepAnimatorEnabled)
            animator.enabled = false;
    }

    public void UpdateRegion()
    {
        if (isRegistered && regionManager != null)
        {
            UnregisterFromRegion();
            RegisterToRegion();
        }
    }
}