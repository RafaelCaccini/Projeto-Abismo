using UnityEngine;

public class PlayerSceneLoader : MonoBehaviour
{
    [Header("REFERÊNCIAS")]
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private GameObject backupPlayer;

    private void Start()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        // =========================
        // NÃO EXISTE PLAYER
        // =========================

        if (player == null)
        {
            Debug.Log(
                "Player não encontrado"
            );

            if (backupPlayer != null)
            {
                backupPlayer.SetActive(true);

                player = backupPlayer;

                Debug.Log(
                    "Backup ativado"
                );
            }
            else
            {
                Debug.LogError(
                    "BackupPlayer NULL"
                );

                return;
            }
        }

        // =========================
        // TELEPORTAR
        // =========================

        Vector3 posicaoSpawn =
            spawnPoint.position;

        // Usa checkpoint se disponível
        if (
            GameManager.Instance != null &&
            GameManager.Instance.HasCheckpoint
        )
        {
            posicaoSpawn =
                GameManager.Instance
                    .CheckpointPosition;
        }

        player.transform.position =
            posicaoSpawn;

        Debug.Log(
            "Player movido para: " +
            posicaoSpawn
        );
    }
}