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

    private void LateUpdate()
    {
        // Only update if we have no target (scene transition case)
        if (currentTarget == null && cam != null)
        {
            TryBindCamera();
        }
    }

    public void TryBindCamera()
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

        // Check if player or currentTarget was destroyed (GameObject.Destroy marks it for destruction)
        // If either the stored target or the current player is null/destroyed, force re-binding
        bool targetDestroyed = currentTarget == null || currentTarget.gameObject == null || currentTarget.gameObject.scene.name == null;
        bool playerChanged = player.transform != currentTarget;

        if (!targetDestroyed && player.transform == currentTarget && cam.Follow == currentTarget)
            return;

        currentTarget = player.transform;
        cam.Follow = currentTarget;
        cam.Target.CustomLookAtTarget = false;
        cam.Target.LookAtTarget = null;
        cam.CancelDamping(true);

        Debug.Log("Camera conectada no player: " + player.GetInstanceID());
    }

    /// <summary>
    /// Reset camera target - call when player is destroyed/reset between scenes
    /// </summary>
    public void ResetTarget()
    {
        currentTarget = null;
    }

    /// <summary>
    /// Called when a player is exiting to a new scene
    /// </summary>
    public void SetExitingPlayer(GameObject exitingPlayerObject)
    {
        // If the exiting player was our current target, clear it
        if (currentTarget != null && currentTarget.gameObject == exitingPlayerObject)
        {
            currentTarget = null;
        }
    }

    private PlayerController FindPlayer()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null && playerObject.TryGetComponent(out PlayerController player))
            return player;

        return FindFirstObjectByType<PlayerController>();
    }
}
