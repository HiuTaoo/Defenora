using System;
using DG.Tweening;
using UnityEngine;

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;

    [Header("List ribbon")]
    [SerializeField] private GameObject[] ribbons;
    
    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.15f;

    public int layerIndex = 0;
    private Vector3[] originalPositions;

    public Action<int> OnLayerIndexChange;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        GetOriginalPosition();
    }

    private void Update()
    {
        Show(); 
    }

    public void GetOriginalPosition()
    {
        originalPositions = new Vector3[ribbons.Length];

        for (int i = 0; i < ribbons.Length; i++)
        {
            RectTransform rect = ribbons[i].GetComponent<RectTransform>();
            if (rect != null)
            {
                originalPositions[i] = rect.anchoredPosition3D;
            }
            else
            {
                originalPositions[i] = ribbons[i].transform.localPosition;
            }
        }

        MoveRibbonToLeft(0);
    }

    public void ChangeLayer()
    {
        OnLayerIndexChange?.Invoke(layerIndex);
    }

    public void Show()
    {
        // Thay vì kiểm tra anyKeyDown mơ hồ, chúng ta lọc chính xác ký tự đầu vào
        if (Input.anyKeyDown)
        {
            var hasSwitched = false; // Cờ hiệu đánh dấu xem có thực sự đổi layer không

            switch (Input.inputString)
            {
                case "1":
                    if (layerIndex != 0)
                    {
                        layerIndex = 0;
                        hasSwitched = true;
                    }
                    MoveRibbonToLeft(0);
                    break;
                case "2":
                    if (layerIndex != 1)
                    {
                        layerIndex = 1;
                        hasSwitched = true;
                    }
                    MoveRibbonToLeft(1);
                    break;
                case "3":
                    if (layerIndex != 2)
                    {
                        layerIndex = 2;
                        hasSwitched = true;
                    }
                    MoveRibbonToLeft(2);
                    break;
            }

            // VỊ TRÍ 1: Chỉ phát SFX khi người chơi bấm đúng phím 1, 2, 3 và Layer thực sự thay đổi
            if (hasSwitched)
            {
                PlayLayerSwitchSFX();
                ChangeLayer();
            }
        }
    }

    public void SwitchToNextLayer()
    {
        if (ribbons == null || ribbons.Length == 0) return;

        layerIndex = (layerIndex + 1) % ribbons.Length;

        // VỊ TRÍ 2: Phát SFX khi người chơi click chuột vào nút UI gọi hàm chuyển tiếp này
        PlayLayerSwitchSFX();

        MoveRibbonToLeft(layerIndex);
        ChangeLayer();
    }

    /// <summary>
    ///     Hàm phụ trợ gọi AudioManager phát SFX an toàn qua Singleton toàn cục
    /// </summary>
    private void PlayLayerSwitchSFX()
    {
        if (AudioManager.Instance != null)
            // Sử dụng tiếng SFX_Click của bạn, hoặc bạn có thể tạo hằng số mới như SFX_LayerChange tùy ý[cite: 8]
            AudioManager.Instance.PlaySFX(SoundNames.SfxButtonTap);
    }

    public void MoveRibbonToLeft(int activeIndex)
    {
        for (int i = 0; i < ribbons.Length; i++)
        {
            ribbons[i].transform.SetSiblingIndex(ribbons.Length - i);
        }
        
        ribbons[activeIndex].transform.SetSiblingIndex(ribbons.Length);

        for (int i = 0; i < ribbons.Length; i++)
        {
            Vector3 original = originalPositions[i];
            
            ribbons[i].transform.DOKill();

            RectTransform rect = ribbons[i].GetComponent<RectTransform>();
            if (rect != null)
            {
                if (i == activeIndex)
                {
                    Vector2 targetPos = new Vector2(original.x - 50f, original.y);
                    rect.DOAnchorPos(targetPos, moveDuration).SetEase(Ease.OutCubic);
                }
                else
                {
                    rect.DOAnchorPos(original, moveDuration).SetEase(Ease.OutCubic);
                }
            }
            else 
            {
                if (i == activeIndex)
                {
                    Vector3 targetPos = new Vector3(original.x - 50f, original.y, original.z);
                    ribbons[i].transform.DOLocalMove(targetPos, moveDuration).SetEase(Ease.OutCubic);
                }
                else
                {
                    ribbons[i].transform.DOLocalMove(original, moveDuration).SetEase(Ease.OutCubic);
                }
            }
        }
    }
}