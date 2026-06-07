using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    public Transform cameraTransform;
    public float parallaxEffect = 0.5f;

    private float lastCameraX;

    private void Awake()
    {
        ResolveCamera();
    }

    void Start()
    {
        if (ResolveCamera())
            lastCameraX = cameraTransform.position.x;
    }

    void LateUpdate()
    {
        if (!ResolveCamera())
            return;

        float deltaX = cameraTransform.position.x - lastCameraX;

        transform.position += new Vector3(deltaX * parallaxEffect, 0f, 0f);

        lastCameraX = cameraTransform.position.x;
    }

    private bool ResolveCamera()
    {
        if (cameraTransform != null)
            return true;

        var mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
            return true;
        }

        var anyCamera = FindFirstObjectByType<Camera>();

        if (anyCamera != null)
        {
            cameraTransform = anyCamera.transform;
            return true;
        }

        return false;
    }
}
