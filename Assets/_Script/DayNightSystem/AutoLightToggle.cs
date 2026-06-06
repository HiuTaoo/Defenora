using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AutoLightToggle : MonoBehaviour
{
    private Light2D pointLight;
    private TimeOfDaySystem timeSystem;

    private void Awake()
    {
        pointLight = GetComponent<Light2D>();
    }

    private void Start()
    {
        timeSystem = TimeOfDaySystem.Instance;

        if (timeSystem != null)
            timeSystem.OnHourChanged += HandleHourChanged;
        else
            Debug.LogError($"[{gameObject.name}] Không tìm thấy TimeOfDaySystem.Instance ở hàm Start!");

        EvaluateAndToggleLight();
    }

    private void OnDestroy()
    {
        if (timeSystem != null) timeSystem.OnHourChanged -= HandleHourChanged;
    }

    private void HandleHourChanged(int currentHour)
    {
        EvaluateAndToggleLight();
    }

    public void EvaluateAndToggleLight()
    {
        if (timeSystem == null) timeSystem = TimeOfDaySystem.Instance;
        if (timeSystem == null || pointLight == null) return;

        var shouldBeOn = timeSystem.IsNightTime();

        if (pointLight.enabled != shouldBeOn)
        {
            pointLight.enabled = shouldBeOn;
        }
    }
}