
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AutoLightToggle : MonoBehaviour
{
    private Light2D pointLight;
    private TimeOfDaySystem timeSystem;

    private void Awake()
    {
        pointLight = GetComponent<Light2D>();
        timeSystem = GetTimeOfDaySystem();
    }

    void Update()
    {
        GetTimeOfDaySystem();

        pointLight.enabled = timeSystem.currentTime >= 18 || timeSystem.currentTime < 6;
    }

    private TimeOfDaySystem GetTimeOfDaySystem()
    {
        if (timeSystem == null)
        {
            timeSystem = TimeOfDaySystem.Instance;
        }
        return timeSystem;
    }
}
