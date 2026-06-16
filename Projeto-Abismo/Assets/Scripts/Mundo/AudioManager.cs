// Assets/Scripts/Mundo/AudioManager.cs
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Música")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicaFases;
    [SerializeField] private string[] cenasComMusica;

    // eventos e volumes
    public Action<float> OnSFXVolumeChanged;
    public float MusicVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    private void Awake()
    {
        // singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // carregar preferências
        MusicVolume = PlayerPrefs.GetFloat("volumeMusica", 1f);
        SFXVolume = PlayerPrefs.GetFloat("volumeSFX", 1f);
    }

    private void OnEnable() => SceneManager.sceneLoaded += CenaCarregada;
    private void OnDisable() => SceneManager.sceneLoaded -= CenaCarregada;

    private void Start()
    {
        AtualizarVolumeMusica();
        AtualizarVolumeSFX();
        VerificarMusica();
    }

    void CenaCarregada(Scene scene, LoadSceneMode mode) => VerificarMusica();

    void VerificarMusica()
    {
        string cenaAtual = SceneManager.GetActiveScene().name;
        bool tocar = false;
        foreach (string cena in cenasComMusica)
        {
            if (cenaAtual == cena) { tocar = true; break; }
        }

        if (tocar)
        {
            if (musicSource != null && !musicSource.isPlaying)
            {
                musicSource.clip = musicaFases;
                musicSource.loop = true;
                AtualizarVolumeMusica(); // garante volume correto antes de tocar
                musicSource.Play();
            }
        }
        else
        {
            musicSource?.Stop();
        }
    }

    // chama quando slider de música muda
    public void AtualizarVolumeMusica()
    {
        MusicVolume = PlayerPrefs.GetFloat("volumeMusica", 1f);
        if (musicSource != null)
            musicSource.volume = MusicVolume;
    }

    // chama quando slider de SFX muda
    public void AtualizarVolumeSFX()
    {
        SFXVolume = PlayerPrefs.GetFloat("volumeSFX", 1f);
        OnSFXVolumeChanged?.Invoke(SFXVolume);
    }

    // Play via AudioSource existente (recomendado para efeitos locais)
    public void PlaySFXFromSource(AudioSource source, AudioClip clip, float volumeScale = 1f)
    {
        if (source == null || clip == null) return;
        source.PlayOneShot(clip, SFXVolume * volumeScale);
    }

    // Play em posição (cria temporariamente um AudioSource)
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;
        GameObject go = new GameObject("SFX_" + clip.name);
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f; // 2D by default; ajuste se quiser 3D
        src.clip = clip;
        src.volume = SFXVolume * volumeScale;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }
}