// Assets/Scripts/Menu/PauseMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Paineis")]
    [SerializeField] private GameObject painelPause;
    [SerializeField] private GameObject painelOpcoes;

    [Header("Audio")]
    [SerializeField] private Slider sliderVolume;        // Música
    [SerializeField] private Slider sliderVolumeGeral;   // SFX

    private bool pausado;

    void Start()
    {
        painelPause.SetActive(false);
        painelOpcoes.SetActive(false);

        // carregar valores (aplica defaults)
        float volumeMusica = PlayerPrefs.GetFloat("volumeMusica", 1f);
        float volumeGeral = PlayerPrefs.GetFloat("volumeSFX", 1f);

        sliderVolume.value = volumeMusica;
        sliderVolumeGeral.value = volumeGeral;

        // listeners em tempo real
        sliderVolume.onValueChanged.AddListener(MudarVolumeMusica);
        sliderVolumeGeral.onValueChanged.AddListener(MudarVolumeGeral);
    }

    public void MudarVolumeMusica(float volume)
    {
        PlayerPrefs.SetFloat("volumeMusica", volume);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
            AudioManager.Instance.AtualizarVolumeMusica();
    }

    public void MudarVolumeGeral(float volume)
    {
        PlayerPrefs.SetFloat("volumeSFX", volume);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
            AudioManager.Instance.AtualizarVolumeSFX();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (painelOpcoes.activeSelf)
            {
                FecharOpcoes();
                return;
            }
            if (pausado) Continuar(); else Pausar();
        }
    }

    public void Pausar()
    {
        painelPause.SetActive(true);
        Time.timeScale = 0f;
        pausado = true;
    }

    public void Continuar()
    {
        painelPause.SetActive(false);
        painelOpcoes.SetActive(false);
        Time.timeScale = 1f;
        pausado = false;
    }

    public void AbrirOpcoes()
    {
        painelPause.SetActive(false);
        painelOpcoes.SetActive(true);
    }

    public void FecharOpcoes()
    {
        painelOpcoes.SetActive(false);
        painelPause.SetActive(true);
    }

    public void IrMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}