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

        player.transform.position =
            spawnPoint.position;

        Debug.Log(
            "Player movido pro spawn"
        );
    }
}