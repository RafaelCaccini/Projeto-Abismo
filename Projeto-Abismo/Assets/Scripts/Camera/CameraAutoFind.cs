using UnityEngine;
using Unity.Cinemachine;

public class CameraAutoFind : MonoBehaviour
{
    private CinemachineCamera cam;

    void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
    }

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();

        if (player == null)
        {
            Debug.LogError("PLAYER NÃO ENCONTRADO");
            return;
        }

        cam.Follow = player.transform;
        cam.LookAt = player.transform;

        Debug.Log("Camera conectada no player: " + player.GetInstanceID());
    }
}