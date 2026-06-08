using UnityEngine;

public class AudioBridge : MonoBehaviour
{
    public void PlaySFX(string key)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(key);
        else
            Debug.LogWarning("[AudioBridge] Không tìm thấy AudioManager.Instance toàn cục!");
    }
}