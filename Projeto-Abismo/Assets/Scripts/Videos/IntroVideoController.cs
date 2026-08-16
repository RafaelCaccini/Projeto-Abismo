using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideoManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Cena")]
    [SerializeField] private string nomeCena = "Parte1";

    private bool carregando;

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += FinalizarVideo;
            videoPlayer.Play();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            PularVideo();
        }
    }

    void FinalizarVideo(VideoPlayer vp)
    {
        CarregarCena();
    }

    public void PularVideo()
    {
        CarregarCena();
    }

    void CarregarCena()
    {
        if (carregando)
            return;

        carregando = true;

        SceneManager.LoadScene(nomeCena);
    }
}