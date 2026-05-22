using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Musica")]
    [SerializeField] private AudioSource musicSource;

    [SerializeField] private AudioClip musicaFases;

    [Header("Cenas")]
    [SerializeField] private string[] cenasComMusica;

    private void Awake()
    {
        // impede duplicar
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // nao destruir entre cenas
        DontDestroyOnLoad(gameObject);
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
        VerificarMusica();
    }

    void CenaCarregada(Scene scene, LoadSceneMode mode)
    {
        VerificarMusica();
    }

    void VerificarMusica()
    {
        string cenaAtual =
            SceneManager.GetActiveScene().name;

        bool tocar = false;

        foreach (string cena in cenasComMusica)
        {
            if (cenaAtual == cena)
            {
                tocar = true;
                break;
            }
        }

        if (tocar)
        {
            if (!musicSource.isPlaying)
            {
                musicSource.clip = musicaFases;

                musicSource.loop = true;

                musicSource.Play();

                Debug.Log("🎵 musica tocando");
            }
        }
        else
        {
            musicSource.Stop();
        }
    }
}