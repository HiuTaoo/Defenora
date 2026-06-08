using UnityEngine;
using UnityEngine.UI;

public class AudioSettingUI : MonoBehaviour
{
    [Header("UI Components")] [SerializeField]
    private Slider masterSlider;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MasterPrefKey = "Setting_MasterVolume";
    private const string MusicPrefKey = "Setting_MusicVolume";
    private const string SfxPrefKey = "Setting_SFXVolume";

    private void Start()
    {
        var savedMaster = PlayerPrefs.GetFloat(MasterPrefKey, 1f);
        var savedMusic = PlayerPrefs.GetFloat(MusicPrefKey, 1f);
        var savedSfx = PlayerPrefs.GetFloat(SfxPrefKey, 1f);

        if (masterSlider != null) masterSlider.value = savedMaster;
        if (musicSlider != null) musicSlider.value = savedMusic;
        if (sfxSlider != null) sfxSlider.value = savedSfx;

        ApplyAllVolumes();

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
    }

    private void ApplyAllVolumes()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.SetVolume("MasterVolume", masterSlider.value);
        AudioManager.Instance.SetVolume("MusicVolume", musicSlider.value);
        AudioManager.Instance.SetVolume("SFXVolume", sfxSlider.value);
    }

    #region Sự kiện lắng nghe khi kéo Slider

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume("MasterVolume", value);

        PlayerPrefs.SetFloat(MasterPrefKey, value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume("MusicVolume", value);

        PlayerPrefs.SetFloat(MusicPrefKey, value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume("SFXVolume", value);

        PlayerPrefs.SetFloat(SfxPrefKey, value);
    }

    #endregion

    private void OnDestroy()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveAllListeners();
        if (musicSlider != null) musicSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveAllListeners();

        PlayerPrefs.Save();
        Debug.Log("[AudioSetting] 💾 Đã lưu cấu hình âm thanh thành công!");
    }
}