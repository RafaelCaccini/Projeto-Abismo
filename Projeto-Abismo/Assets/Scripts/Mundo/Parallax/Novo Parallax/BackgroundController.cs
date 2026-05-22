using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    public Transform cameraTransform;
    public float parallaxEffect = 0.5f;

    private float lastCameraX;

    void Start()
    {
        lastCameraX = cameraTransform.position.x;
    }

    void LateUpdate()
    {
        float deltaX = cameraTransform.position.x - lastCameraX;

        transform.position += new Vector3(deltaX * parallaxEffect, 0f, 0f);

        lastCameraX = cameraTransform.position.x;
    }
}