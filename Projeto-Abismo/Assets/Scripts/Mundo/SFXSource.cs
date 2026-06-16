using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXSource : MonoBehaviour
{
    private AudioSource src;
    private float baseVolume = 1f;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        if (src != null)
            baseVolume = src.volume;
    }

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSFXVolumeChanged += HandleSFXVolumeChanged;
            // aplica imediatamente o volume atual
            HandleSFXVolumeChanged(AudioManager.Instance.SFXVolume);
        }
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnSFXVolumeChanged -= HandleSFXVolumeChanged;
    }

    private void HandleSFXVolumeChanged(float sfxVolume)
    {
        if (src != null)
            src.volume = baseVolume * sfxVolume;
    }
}