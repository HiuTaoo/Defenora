using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SimpleSpriteAnimator : MonoBehaviour
{
    [Header("--- Animation Settings ---")]
    [SerializeField] private Sprite[] animationFrames; 
    [SerializeField] private float frameRate = 0.1f;   
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loop = true;

    private SpriteRenderer _spriteRenderer;
    private int _currentFrameIndex;
    private float _frameTimer;
    private bool _isPlaying;

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
        _frameTimer = 0f;
        _currentFrameIndex = Random.Range(0, animationFrames.Length); 
        _spriteRenderer.sprite = animationFrames[_currentFrameIndex];
    }

    public void Stop()
    {
        _isPlaying = false;
    }

    private void Update()
    {
        if (!_isPlaying || animationFrames == null || animationFrames.Length <= 1) return;

        _frameTimer += Time.deltaTime;

        if (_frameTimer >= frameRate)
        {
            _frameTimer -= frameRate; 
            
            _currentFrameIndex++;

            if (_currentFrameIndex >= animationFrames.Length)
            {
                if (loop)
                {
                    _currentFrameIndex = 0;
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
}