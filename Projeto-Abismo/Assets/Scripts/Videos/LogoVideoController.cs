using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class LogoVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string menuSceneName = "Menu";

    private void Start()
    {
        videoPlayer.loopPointReached += VideoFinished;
        videoPlayer.Play();
    }

    private void VideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(menuSceneName);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= VideoFinished;
        }
    }
}