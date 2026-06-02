using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SimpleSpriteAnimator : MonoBehaviour
{
    [Header("--- Animation Settings ---")]
    [SerializeField] private Sprite[] animationFrames;

    [SerializeField] private float frameRate = 0.12f;   
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loop = true;

    [Header("--- Wind Gust / Desynchronization ---")]
    [Tooltip("Tỷ lệ ngẫu nhiên thay đổi tốc độ chạy anim giữa các cây để tăng độ lệch (ví dụ lệch +- 15%)")]
    [Range(0f, 0.3f)]
    [SerializeField]
    private float speedRandomness = 0.15f;

    [Tooltip("Bật tính năng nghỉ ngắt quãng giữa các cơn gió")] [SerializeField]
    private bool useRandomPause = true;

    [SerializeField] private float minPauseDuration = 0.5f;
    [SerializeField] private float maxPauseDuration = 2f;

    [Header("--- Shake Cycle Settings ---")]
    [Tooltip("Số vòng rung liên tục tối thiểu trước khi dừng nghỉ")]
    [SerializeField]
    private int minShakeCycles = 3;

    [Tooltip("Số vòng rung liên tục tối đa trước khi dừng nghỉ")] [SerializeField]
    private int maxShakeCycles = 7;

    private SpriteRenderer _spriteRenderer;
    private int _currentFrameIndex;
    private float _frameTimer;
    private bool _isPlaying;

    private float _actualFrameRate;
    private bool _isPausing;
    private float _pauseTimer;
    private float _currentPauseDuration;

    private int _targetShakeCycles;
    private int _currentShakeCycleCount; 

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (playOnAwake)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (animationFrames == null || animationFrames.Length == 0) return;
        
        _isPlaying = true;
        _isPausing = false;
        _frameTimer = 0f;

        var randomOffset = Random.Range(-speedRandomness, speedRandomness);
        _actualFrameRate = frameRate * (1f + randomOffset);

        ResetTargetShakeCycles();

        _currentFrameIndex = Random.Range(0, animationFrames.Length); 
        _spriteRenderer.sprite = animationFrames[_currentFrameIndex];
    }

    public void Stop()
    {
        _isPlaying = false;
        _isPausing = false;
    }

    /// <summary>
    ///     Ép hiển thị một ảnh tĩnh cố định (Dùng khi cây bị chặt đổ)
    /// </summary>
    public void SetStaticSprite(Sprite staticSprite)
    {
        Stop();
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (staticSprite != null) _spriteRenderer.sprite = staticSprite;
    }

    private void Update()
    {
        if (!_isPlaying || animationFrames == null || animationFrames.Length <= 1) return;

        if (_isPausing)
        {
            _pauseTimer += Time.deltaTime;
            if (_pauseTimer >= _currentPauseDuration)
            {
                _isPausing = false;
                _frameTimer = 0f;
                ResetTargetShakeCycles();
            }

            return;
        }

        _frameTimer += Time.deltaTime;

        if (_frameTimer >= _actualFrameRate)
        {
            _frameTimer -= _actualFrameRate; 
            _currentFrameIndex++;

            if (_currentFrameIndex >= animationFrames.Length)
            {
                if (loop)
                {
                    _currentFrameIndex = 0;

                    _currentShakeCycleCount++;

                    if (useRandomPause && _currentShakeCycleCount >= _targetShakeCycles)
                    {
                        if (Random.value > 0.4f)
                        {
                            TriggerRandomPause();
                            return;
                        }
                    }
                }
                else
                {
                    _currentFrameIndex = animationFrames.Length - 1;
                    _isPlaying = false;
                    return;
                }
            }

            _spriteRenderer.sprite = animationFrames[_currentFrameIndex];
        }
    }

    /// <summary>
    ///     Kích hoạt trạng thái đứng yên tạm thời của cây
    /// </summary>
    private void TriggerRandomPause()
    {
        _isPausing = true;
        _pauseTimer = 0f;
        _currentPauseDuration = Random.Range(minPauseDuration, maxPauseDuration);

        _currentFrameIndex = 0;
        _spriteRenderer.sprite = animationFrames[_currentFrameIndex];
    }

    /// <summary>
    ///     🌟 Tính toán lại số vòng cần rung liên tục cho chu kỳ kế tiếp
    /// </summary>
    private void ResetTargetShakeCycles()
    {
        _currentShakeCycleCount = 0;
        _targetShakeCycles = Random.Range(minShakeCycles, maxShakeCycles + 1);
    }
}