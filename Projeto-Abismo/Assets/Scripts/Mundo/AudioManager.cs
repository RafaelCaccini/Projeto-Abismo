
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class MusicaPorCena
{
    [Tooltip("Nome exato da cena, como aparece no Unity.")]
    public string nomeCena;

    [Tooltip("Música que será reproduzida nessa cena.")]
    public AudioClip musica;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Fonte de áudio")]
    [SerializeField] private AudioSource musicSource;

    [Header("Músicas por cena")]
    [SerializeField] private MusicaPorCena[] musicasPorCena;

    [Header("Configuração")]
    [SerializeField] private bool pararMusicaSeNaoEncontrarCena = true;

    public Action<float> OnSFXVolumeChanged;

    public float MusicVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    private void Awake()
    {
        // Impede que existam dois AudioManagers.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Mantém o gerenciador ao trocar de cena.
        DontDestroyOnLoad(gameObject);

        // Procura um AudioSource caso não tenha sido atribuído.
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        // Carrega os volumes salvos.
        MusicVolume = PlayerPrefs.GetFloat("volumeMusica", 1f);
        SFXVolume = PlayerPrefs.GetFloat("volumeSFX", 1f);

        // Configura a fonte de música.
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = MusicVolume;
        }
        else
        {
            Debug.LogError(
                "[AudioManager] Nenhum AudioSource foi atribuído!"
            );
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += CenaCarregada;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= CenaCarregada;
    }

    private void Start()
    {
        AtualizarVolumeMusica();
        AtualizarVolumeSFX();

        // Seleciona a música da cena inicial.
        VerificarMusica();
    }

    private void CenaCarregada(Scene scene, LoadSceneMode mode)
    {
        // Executado sempre que uma nova cena é carregada.
        VerificarMusica();
    }

    private void VerificarMusica()
    {
        if (musicSource == null)
        {
            Debug.LogError(
                "[AudioManager] O AudioSource está vazio."
            );
            return;
        }

        string cenaAtual = SceneManager.GetActiveScene().name;
        AudioClip musicaEncontrada = null;

        // Procura a música correspondente ao nome da cena.
        if (musicasPorCena != null)
        {
            foreach (MusicaPorCena item in musicasPorCena)
            {
                if (item == null)
                    continue;

                if (item.nomeCena == cenaAtual)
                {
                    musicaEncontrada = item.musica;
                    break;
                }
            }
        }

        // Não encontrou música para essa cena.
        if (musicaEncontrada == null)
        {
            if (pararMusicaSeNaoEncontrarCena)
            {
                musicSource.Stop();
                musicSource.clip = null;
            }

            Debug.Log(
                $"[AudioManager] Nenhuma música configurada para: {cenaAtual}"
            );

            return;
        }

        // Se a música já está tocando, não reinicia.
        if (musicSource.clip == musicaEncontrada &&
            musicSource.isPlaying)
        {
            return;
        }

        // Troca para a música da nova cena.
        musicSource.Stop();
        musicSource.clip = musicaEncontrada;
        musicSource.loop = true;
        AtualizarVolumeMusica();
        musicSource.Play();

        Debug.Log(
            $"[AudioManager] Tocando '{musicaEncontrada.name}' na cena '{cenaAtual}'."
        );
    }

    public void AtualizarVolumeMusica()
    {
        MusicVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat("volumeMusica", 1f)
        );

        if (musicSource != null)
        {
            musicSource.volume = MusicVolume;
        }
    }

    public void AtualizarVolumeSFX()
    {
        SFXVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat("volumeSFX", 1f)
        );

        OnSFXVolumeChanged?.Invoke(SFXVolume);
    }

    public void PlaySFXFromSource(
        AudioSource source,
        AudioClip clip,
        float volumeScale = 1f)
    {
        if (source == null || clip == null)
            return;

        source.PlayOneShot(
            clip,
            SFXVolume * Mathf.Clamp01(volumeScale)
        );
    }

    public void PlaySFXAtPoint(
        AudioClip clip,
        Vector3 position,
        float volumeScale = 1f)
    {
        if (clip == null)
            return;

        GameObject go = new GameObject("SFX_" + clip.name);
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();

        source.spatialBlend = 0f;
        source.clip = clip;
        source.volume = SFXVolume * Mathf.Clamp01(volumeScale);
        source.Play();

        Destroy(go, clip.length + 0.1f);
    }
}
