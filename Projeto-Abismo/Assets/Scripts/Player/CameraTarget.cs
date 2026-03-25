using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 10f;

    [Header("Look Ahead")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSmooth = 5f;

    private PlayerController playerController;
    private float currentLookAhead;

    void Start()
    {
        if (player != null)
            playerController = player.GetComponent<PlayerController>();
    }

    void LateUpdate()
    {
        if (player == null || playerController == null)
            return;

        HandleLookAhead();
        FollowPlayer();
    }

    void HandleLookAhead()
    {
        float targetLookAhead = playerController.IsFacingRight() ? lookAheadDistance : -lookAheadDistance;

        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSmooth * Time.deltaTime);
    }

    void FollowPlayer()
    {
        Vector3 targetPosition = player.position + new Vector3(currentLookAhead, 0f, 0f);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }
}