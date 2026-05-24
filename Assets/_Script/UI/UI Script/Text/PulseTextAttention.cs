using UnityEngine;
using DG.Tweening; // Khai báo sử dụng DOTween

public class PulseTextAttention : MonoBehaviour
{
    [Header("Cài đặt Hiệu ứng (Tween Settings)")]
    [Tooltip("Hệ số phóng to (Ví dụ: 1.2 là to lên 20%)")]
    [SerializeField] private float scaleMultiplier = 1.2f;
    
    [Tooltip("Thời gian để hoàn thành 1 nhịp phóng to (giây)")]
    [SerializeField] private float pulseDuration = 0.5f;

    [Tooltip("Nếu true, chữ vẫn chớp nháy ngay cả khi Pause Game (Time.timeScale = 0)")]
    [SerializeField] private bool ignoreTimeScale = true;

    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;

        StartPulseEffect();
    }

    private void StartPulseEffect()
    {
        
        transform.DOScale(originalScale * scaleMultiplier, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(ignoreTimeScale);
    }

    private void OnDisable()
    {
        transform.DOPause();
    }

    private void OnEnable()
    {
        if (originalScale != Vector3.zero)
        {
            transform.DOPlay();
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}