using System.Collections; // Cần thiết để dùng Coroutine
using UnityEngine;
using UnityEngine.UI; // Cần thiết để dùng UI Slider
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Cài đặt Transition Trắng")]
    [Tooltip("Canvas dùng riêng cho hiệu ứng che màn hình")]
    [SerializeField] private RectTransform transitionCanvas;
    
    [Tooltip("Panel Trắng bên Trái")]
    [SerializeField] private RectTransform leftWhitePanel;
    
    [Tooltip("Panel Trắng bên Phải")]
    [SerializeField] private RectTransform rightWhitePanel;
    
    [Tooltip("Thời gian trượt vào (giây)")]
    [SerializeField] private float transitionDuration = 0.5f;
    
    [Tooltip("Tên Scene tiếp theo muốn load")]
    [SerializeField] private int nextSceneIndex = 1;

    [Header("Cài đặt Thanh Tiến Độ (Loading)")]
    [Tooltip("Object cha chứa toàn bộ UI Loading (Thanh Slider + Chữ Loading)")]
    [SerializeField] private GameObject loadingGroup;
    
    [Tooltip("Thanh trượt Loading")]
    [SerializeField] private Slider progressBar;
    
    [Tooltip("Chữ hiển thị phần trăm (100%)")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("UI Khác")]
    [Tooltip("Chữ 'Click to Start' (sẽ bị tắt khi bấm)")]
    [SerializeField] private TextMeshProUGUI text;

    private bool isTransitioning = false;

    private void Start()
    {
        // Ẩn thanh loading khi vừa vào Main Menu
        if (loadingGroup != null)
        {
            loadingGroup.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI() && !isTransitioning)
            {
                if (text != null) text.gameObject.SetActive(false);
                StartWhiteWipeTransition();
            }
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void StartWhiteWipeTransition()
    {
        isTransitioning = true;

        float halfScreenWidth = transitionCanvas.rect.width / 2f;
        float overlapOffset = 20f; 
        
        float targetLeft = halfScreenWidth - overlapOffset;
        float targetRight = -halfScreenWidth + overlapOffset;

        leftWhitePanel.DOAnchorPosX(targetLeft, transitionDuration).SetEase(Ease.InOutSine);

        rightWhitePanel.DOAnchorPosX(targetRight, transitionDuration).SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                // Khi 2 tấm panel trắng đã đóng sầm lại xong -> Bắt đầu load ngầm Scene mới
                StartCoroutine(LoadSceneAsync());
            });
    }

    /// <summary>
    /// Coroutine xử lý tải Scene ngầm và cập nhật thanh tiến độ
    /// </summary>
    private IEnumerator LoadSceneAsync()
    {
        // 1. Bật giao diện Loading lên (Nằm đè lên lớp nền trắng)
        if (loadingGroup != null) loadingGroup.SetActive(true);

        // 2. Bắt đầu tải Scene trong nền
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(nextSceneIndex);

        // Chặn Unity không cho phép tự động chuyển Scene ngay cả khi đã tải xong ngầm 
        // (Để ta có thể giữ thanh progress ở 100% một lúc cho đẹp, thay vì giật chớp nhoáng)
        asyncOperation.allowSceneActivation = false;

        // Vòng lặp chạy liên tục mỗi khung hình cho đến khi load xong
        while (!asyncOperation.isDone)
        {
            // Đoạn này khắc phục "cú lừa 0.9" của Unity. Ép tiến độ về khoảng 0 -> 1
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);

            // Cập nhật Slider UI
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Cập nhật Text %
            if (progressText != null)
            {
                // Chuyển sang dạng % nguyên (ví dụ: 85%)
                progressText.text = "Loading... " + (progress * 100f).ToString("F0") + "%";
            }

            // Khi Unity đã tải ngầm xong (progress thực tế đạt 0.9)
            if (asyncOperation.progress >= 0.9f)
            {
                // Ở đây bạn có thể cho đợi thêm 0.5 giây để người chơi nhìn thấy số 100%
                // Tuy nhiên hiện tại mình sẽ cho nó mở Scene luôn.
                asyncOperation.allowSceneActivation = true;
            }

            yield return null; // Chờ đến khung hình tiếp theo
        }
    }
}