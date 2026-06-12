using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")] [SerializeField]
    private AudioSource musicSourceA;

    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Mixer")] [SerializeField]
    private AudioMixer audioMixer;

    [Header("Crossfade Settings")] [SerializeField]
    private float fadeDuration = 2.0f;

    [Header("Audio Clips Data")] [SerializeField]
    private List<AudioData> audioDataList;

    private readonly Dictionary<string, AudioClip> audioClips = new();
    private AudioSource activeMusicSource;
    private Coroutine crossfadeCoroutine;

    [Serializable]
    public struct AudioData
    {
        public string key;
        public AudioClip clip;
    } 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioDictionary();

            activeMusicSource = musicSourceA;
        }
        else
        {
            Destroy(gameObject);
        } 
    }

    private void InitializeAudioDictionary()
    {
        foreach (var data in audioDataList)
            if (!audioClips.ContainsKey(data.key))
                audioClips.Add(data.key, data.clip);
    }

    public void PlayMusic(string key, bool loop = true)
    {
        if (!audioClips.TryGetValue(key, out var newClip)) return;

        if (activeMusicSource.clip == newClip && activeMusicSource.isPlaying) return;

        var bgmTargetSource = activeMusicSource == musicSourceA ? musicSourceB : musicSourceA;

        bgmTargetSource.clip = newClip;
        bgmTargetSource.loop = loop;
        bgmTargetSource.Play();

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossfadeMusicCoroutine(bgmTargetSource, fadeDuration));
    }

    private IEnumerator CrossfadeMusicCoroutine(AudioSource targetSource, float duration)
    {
        float time = 0;
        var startVolActive = activeMusicSource.volume;

        targetSource.volume = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            var progress = time / duration;

            activeMusicSource.volume = Mathf.Lerp(startVolActive, 0f, progress);
            targetSource.volume = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        activeMusicSource.volume = 0f;
        activeMusicSource.Stop();

        activeMusicSource = targetSource;
        activeMusicSource.volume = 1f;
    }

    public void PlaySFX(string key)
    {
        if (audioClips.TryGetValue(key, out var clip)) sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX3D(string key, AudioSource targetSource)
    {
        if (targetSource == null) return;

        if (audioClips.TryGetValue(key, out var clip))
            targetSource.PlayOneShot(clip);
        else
            Debug.LogWarning($"[AudioManager] Không tìm thấy SFX 3D có Key: {key}");
    }

    public void SetVolume(string exposedParamName, float value)
    {
        if (audioMixer == null) return;
        var dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        audioMixer.SetFloat(exposedParamName, dB);
    }

    public void PauseMusic()
    {
        if (activeMusicSource != null && activeMusicSource.isPlaying) activeMusicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (activeMusicSource != null && !activeMusicSource.isPlaying) activeMusicSource.UnPause();
    }

    public AudioClip GetAudioClip(string key)
    {
        if (audioClips.TryGetValue(key, out var clip)) return clip;
        return null;
    }
}

public static class SoundNames
{
    public const string DayTheme = "DayTheme";
    public const string NightTheme = "NightTheme";
    public const string BattleTheme = "BattleTheme";
    public const string VictoryTheme = "VictoryTheme";
    public const string GameOverTheme = "GameOverTheme";
    public const string MainMenuTheme = "MainMenuTheme";
    public const string SfxChangeScene = "SFX_ChangeScene";
    public const string SfxClick = "SFX_Click";
    public const string SfxNotification = "SFX_Notification";
    public const string SfxButtonTap = "SFX_ButtonTap";
    public const string SfxPaySuccess = "SFX_PaySuccess";
    public const string SfxCollect = "SFX_Collect";
    public const string SfxShoot = "SFX_Shoot";
    public const string SfxExplode = "SFX_Explode";
    public const string SfxHammerHit = "SFX_HammerHit";
    public const string SfxAxeHit = "SFX_AxeHit";
    public const string SfxSwordSlash = "SFX_SwordSlash";
    public const string SfxLancerHit = "SFX_LancerHit";
    public const string SfxSmallExplode = "SFX_SmallExplode";
    public const string SfxHeal = "SFX_Heal";
    public const string SfxNewDay = "SFX_NewDay";
    public const string SfxSuccess = "SFX_Success";
    public const string SfxBuildConfirm = "SFX_BuildConfirm";
    public const string SfxWarning = "SFX_Warning";
    public const string SfxTing = "SFX_Ting";
}