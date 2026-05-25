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
        }
    }

    void Update()
    {
        if (Input.anyKeyDown)
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