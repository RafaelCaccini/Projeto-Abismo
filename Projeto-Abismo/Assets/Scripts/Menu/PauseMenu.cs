using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Paineis")]
    [SerializeField] private GameObject painelPause;
    [SerializeField] private GameObject painelOpcoes;

    [Header("Audio")]
    [SerializeField] private Slider sliderVolume;

    private bool pausado;

    void Start()
    {
        painelPause.SetActive(false);
        painelOpcoes.SetActive(false);

        // volume salvo
        float volume =
            PlayerPrefs.GetFloat("volume", 1f);

        AudioListener.volume = volume;

        sliderVolume.value = volume;

        sliderVolume.onValueChanged
            .AddListener(MudarVolume);
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

            if (pausado)
                Continuar();
            else
                Pausar();
        }
    }

    // =====================================
    // PAUSE
    // =====================================

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

    // =====================================
    // OPÇÕES
    // =====================================

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

    // =====================================
    // VOLUME
    // =====================================

    public void MudarVolume(float volume)
    {
        AudioListener.volume = volume;

        PlayerPrefs.SetFloat(
            "volume",
            volume
        );

        PlayerPrefs.Save();
    }

    // =====================================
    // MENU
    // =====================================

    public void IrMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("Menu");
    }
}