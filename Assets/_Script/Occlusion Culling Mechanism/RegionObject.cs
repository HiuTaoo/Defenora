using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RegionObject : MonoBehaviour
{
    [Header("Region Settings")]
    [SerializeField] private bool autoRegister = true;
    [SerializeField] private bool keepAnimatorEnabled = false; 

    private RegionManager regionManager;
    private SpriteRenderer spriteRenderer;

    private Animator standardAnimator;
    private SimpleSpriteAnimator customAnimator;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        standardAnimator = GetComponent<Animator>();
        customAnimator = GetComponent<SimpleSpriteAnimator>(); 
    }

    void Start()
    {
        if (autoRegister) RegisterToRegion();
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
    }

    private bool isRegistered;

    public void UnregisterFromRegion()
    {
        if (!isRegistered) return;
        if (regionManager != null)
        {
            regionManager.UnregisterObject(gameObject);
            isRegistered = false;
        }
    }

    private void OnDestroy()
    {
        UnregisterFromRegion();
    }

    public void OnRegionActivated()
    {
        if (spriteRenderer != null) 
            spriteRenderer.enabled = true;

        if (standardAnimator != null && !keepAnimatorEnabled)
            standardAnimator.enabled = true;

        if (customAnimator != null)
        {
            customAnimator.enabled = true;
            customAnimator.Play();
        }
    }

    public void OnRegionDeactivated()
    {
        if (spriteRenderer != null) 
            spriteRenderer.enabled = false;

        if (standardAnimator != null && !keepAnimatorEnabled)
            standardAnimator.enabled = false;

        if (customAnimator != null)
        {
            customAnimator.Stop();
            customAnimator.enabled = false;
        }
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