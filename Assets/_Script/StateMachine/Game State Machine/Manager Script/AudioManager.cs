using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager: MonoBehaviour
{
    public static AudioManager Instance;
    
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private Dictionary<string, AudioClip> audioClips;

    public AudioManager(AudioSource music, AudioSource sfx)
    {
        musicSource = music;
        sfxSource = sfx;
        audioClips = new Dictionary<string, AudioClip>();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void RegisterAudio(string key, AudioClip clip)
    {
        audioClips[key] = clip;
    }

    public void PlayMusic(string key, bool loop = true)
    {
        if (audioClips.ContainsKey(key))
        {
            musicSource.clip = audioClips[key];
            musicSource.loop = loop;
            musicSource.Play();
        }
    }

    public void PlaySFX(string key)
    {
        if (audioClips.ContainsKey(key))
        {
            sfxSource.PlayOneShot(audioClips[key]);
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}
