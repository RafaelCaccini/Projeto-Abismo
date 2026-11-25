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
            // Salva referência ao GameObject player que está saindo
            GameObject exitingPlayerObject = other.gameObject;

            // Limpa checkpoint ao trocar de cena
            // (nova fase = novo começo)
            if (GameManager.Instance != null)
            {
                GameManager.Instance
                    .ClearCheckpoint();
            }

            // Notifica CameraAutoFind qual player está saindo
            // para que ele saiba qual player está sendo substituído
            var cameraAutoFinds = FindObjectsOfType<CameraAutoFind>();
            foreach (var caf in cameraAutoFinds)
            {
                caf.SetExitingPlayer(exitingPlayerObject);
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}