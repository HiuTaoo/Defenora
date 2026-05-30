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

    public float dayLengthInMinutes = 5f;

    [Header("Timer Text")] public TextMeshProUGUI timeDisplayText;

    private float timeMultiplier;

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
    }


    private void Update()
    {
        currentTime += Time.deltaTime * timeMultiplier;
        if (currentTime >= 24f)
            currentTime -= 24f;

        UpdateLighting();
        UpdateTimeDisplay();
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

    public bool IsNightTime()
    {
        return currentTime is >= 18f or <= 6f;
    }
}