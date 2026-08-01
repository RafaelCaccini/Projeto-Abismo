using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : MonoBehaviour
{
    [SerializeField] private string sceneName;

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (other.CompareTag("Player"))
        {
            // Limpa checkpoint ao trocar de cena
            // (nova fase = novo começo)
            if (GameManager.Instance != null)
            {
                GameManager.Instance
                    .ClearCheckpoint();
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}