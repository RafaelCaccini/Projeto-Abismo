using UnityEngine;
using Unity.Cinemachine;

public class CameraAutoFind : MonoBehaviour
{
    private CinemachineCamera cam;
    private CinemachineBrain brain;
    private Transform currentTarget;

    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
    }

    private void OnEnable()
    {
        TryBindCamera();
    }

    private void Start()
    {
        TryBindCamera();
    }

    private void Update()
    {
        TryBindCamera();
    }

    private void TryBindCamera()
    {
        if (cam == null)
            cam = GetComponent<CinemachineCamera>();

        if (cam == null)
            return;

        if (brain == null)
        {
            var mainCamera = Camera.main;

            if (mainCamera != null)
                brain = mainCamera.GetComponent<CinemachineBrain>();

            if (brain == null)
                brain = FindFirstObjectByType<CinemachineBrain>();
        }

        if (brain != null)
        {
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }

        var player = FindPlayer();

        if (player == null)
            return;

        if (player.transform == currentTarget && cam.Follow == currentTarget)
            return;

        currentTarget = player.transform;
        cam.Follow = currentTarget;
        cam.Target.CustomLookAtTarget = false;
        cam.Target.LookAtTarget = null;
        cam.CancelDamping(true);

        Debug.Log("Camera conectada no player: " + player.GetInstanceID());
    }

    private PlayerController FindPlayer()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null && playerObject.TryGetComponent(out PlayerController player))
            return player;

        return FindFirstObjectByType<PlayerController>();
    }
}
