using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;

public class RegionStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI activeRegionsText;
    [SerializeField] private TextMeshProUGUI totalObjectsText;
    [SerializeField] private TextMeshProUGUI activeObjectsText;
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private TextMeshProUGUI batchesText;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool showStats = true;

    private RegionManager regionManager;
    private float timer;
    private int frameCount;
    private float timeSum;

    void Start()
    {
        regionManager = FindObjectOfType<RegionManager>();
        if (regionManager == null)
        {
            Debug.LogWarning("RegionManager not found!");
            enabled = false;
        }
    }

    void Update()
    {
        if (!showStats) return;

        frameCount++;
        timeSum += Time.unscaledDeltaTime;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            UpdateStats();
            timer = 0f;
            frameCount = 0;
            timeSum = 0f;
        }
    }

    void UpdateStats()
    {
        // Region stats
        if (regionManager != null)
        {
            activeRegionsText?.SetText($"Active Regions: {regionManager.GetActiveRegionCount()}");
            totalObjectsText?.SetText($"Total Objects: {regionManager.GetTotalObjectCount()}");
            activeObjectsText?.SetText($"Active Objects: {regionManager.GetActiveObjectCount()}");
        }

        // FPS
        if (fpsText != null && timeSum > 0f)
        {
            float fps = frameCount / timeSum;
            fpsText.text = $"FPS: {fps:F1}";
            fpsText.color = fps >= 50 ? Color.green : fps >= 30 ? Color.yellow : Color.red;
        }

        // Batches (Chỉ hiển thị gợi ý, không lấy được số thực trong runtime build)
        if (batchesText != null)
        {
#if UNITY_EDITOR
            // Khi chạy trong Editor, nhắc bật Stats
            batchesText.text = "Batches: (Xem Stats Panel)";
#else
            // Khi build, không có API nào lấy được batches trực tiếp
            batchesText.text = "Batches: N/A in build";
#endif
        }
    }

    public void ToggleStats()
    {
        showStats = !showStats;
        gameObject.SetActive(showStats);
    }
}
