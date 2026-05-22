using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private float startPos3;
    public GameObject cameraParallax3;
    public float parallaxEffect3;

    void Start()
    {
        startPos3 = transform.position.x;
    }


    void Update()
    {
        float distance = cameraParallax3.transform.position.x * parallaxEffect3;

        transform.position = new Vector3(startPos3 + distance, transform.position.y, transform.position.z);
    }
}