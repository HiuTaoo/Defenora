using _Script.Object_Pooling;
using UnityEngine;
using UnityEngine.UI;

public class UINotificationManager : MonoBehaviour
{
    public static UINotificationManager Instance { get; private set; }

    [Header("--- Layout Settings ---")]
    [Tooltip("Thanh chứa các thông báo (Nên gắn Vertical Layout Group)")]
    [SerializeField]
    private Transform notificationContainer;

    [Header("--- Visibility Settings ---")]
    [Tooltip("Kéo chính khung Scroll View hoặc Panel bọc ngoài hệ thống thông báo vào đây để tự động ẩn/hiện")]
    [SerializeField]
    private GameObject mainNotificationPanel;

    private int _activeToastCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdatePanelVisibility();
    }

    /// <summary>
    ///     Hàm vạn năng: Gọi từ bất kỳ đâu trong game để bắn thông báo lên màn hình
    /// </summary>
    public void ShowNotification(string message, NotificationColorType colorType = NotificationColorType.Info)
    {
        if (notificationContainer == null) return;

        _activeToastCount++;
        UpdatePanelVisibility();

        var toastObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.toastItemPrefab, notificationContainer.position,
            Quaternion.identity);

        if (toastObj != null)
        {
            toastObj.transform.SetParent(notificationContainer, false);
            toastObj.transform.localScale = Vector3.one;
            toastObj.transform.SetAsLastSibling();

            var toastScript = toastObj.GetComponent<ToastItem>();
            if (toastScript != null)
            {
                var targetColor = GetColorFromType(colorType);

                toastScript.SetupToast(message, targetColor);
            }
            else
            {
                _activeToastCount--;
                UpdatePanelVisibility();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(notificationContainer.GetComponent<RectTransform>());

            var scrollRect = notificationContainer.GetComponentInParent<ScrollRect>();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            _activeToastCount--;
            UpdatePanelVisibility();
        }
    }

    public void OnToastExpired()
    {
        _activeToastCount--;

        if (_activeToastCount < 0) _activeToastCount = 0;

        UpdatePanelVisibility();
    }

    /// <summary>
    ///     Kiểm tra số lượng và thực hiện ẩn/hiện Panel tập trung
    /// </summary>
    private void UpdatePanelVisibility()
    {
        if (mainNotificationPanel == null) return;

        var shouldShow = _activeToastCount > 0;

        if (mainNotificationPanel.activeSelf != shouldShow) mainNotificationPanel.SetActive(shouldShow);
    }

    private Color GetColorFromType(NotificationColorType type)
    {
        return type switch
        {
            NotificationColorType.Error => Color.red,
            NotificationColorType.Success => new Color(0f, 0.8f, 0f),
            NotificationColorType.Warning => Color.yellow,
            NotificationColorType.Info => Color.white,
            _ => Color.white
        };
    }
}