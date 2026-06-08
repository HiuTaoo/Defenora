using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class TimeOfDaySystem : MonoBehaviour
{
    public static TimeOfDaySystem Instance;

    [Header("Lighting Settings")] public Light2D globalLight;
    public Gradient dayColorGradient;
    public AnimationCurve intensityCurve;

    [Header("Time Settings")] [Range(0f, 24f)] [SerializeField]
    private float currentTime = 6f;

    public int CurrentDay { get; set; } = 1; 
    public float dayLengthInMinutes = 5f;

    [Header("Timer Text")] public TextMeshProUGUI timeDisplayText;
    private float timeMultiplier;

    public Action<int> OnHourChanged;

    public Action<int> OnDayChanged;
    
    private int _lastHourValue = -1;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        globalLight = transform.Find("Global Light")?.GetComponent<Light2D>();
    }

    private void Start()
    {
        timeMultiplier = 24f / (dayLengthInMinutes * 60f);

        _lastHourValue = Mathf.FloorToInt(currentTime);
    }

    private void Update()
    {
        currentTime += Time.deltaTime * timeMultiplier;
        
        if (currentTime >= 24f)
        {
            currentTime -= 24f;

            CurrentDay++;
            OnDayChanged?.Invoke(CurrentDay);
        }

        UpdateLighting();
        UpdateTimeDisplay();

        var currentHourInt = Mathf.FloorToInt(currentTime);
        if (currentHourInt != _lastHourValue)
        {
            _lastHourValue = currentHourInt;

            if (currentHourInt == 6)
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(SoundNames.SfxNewDay);

            OnHourChanged?.Invoke(currentHourInt);
        }
    }

    private void UpdateLighting()
    {
        var timePercent = currentTime / 24f;
        if (globalLight != null)
        {
            globalLight.color = dayColorGradient.Evaluate(timePercent);
            globalLight.intensity = intensityCurve.Evaluate(timePercent);
        }
    }

    private void UpdateTimeDisplay()
    {
        var hours = Mathf.FloorToInt(currentTime);
        var minutes = Mathf.FloorToInt((currentTime - hours) * 60f);

        var formattedTime = $"{hours:00}:{minutes:00}";
        if (timeDisplayText != null) timeDisplayText.text = formattedTime;
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }

    public int GetCurrentHourInt()
    {
        return Mathf.FloorToInt(currentTime);
    }

    public bool IsNightTime()
    {
        return currentTime is >= 18f or <= 6f;
    }

    public void SetCurrentDay(int day)
    {
        CurrentDay = day;
    }

    public void SetCurrentTime(float time)
    {
        currentTime = time;

        var currentHourInt = Mathf.FloorToInt(currentTime);
        _lastHourValue = currentHourInt;

        OnHourChanged?.Invoke(currentHourInt);

        Debug.Log(
            $"[TimeSystem] Đã nạp thời gian nhảy cóc thành công: {currentHourInt}h. Đã phát tín hiệu đồng bộ toàn Scene!");
    }
}