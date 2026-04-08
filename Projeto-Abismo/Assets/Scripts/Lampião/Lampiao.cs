using UnityEngine;

public class Lampiao : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;

    [Header("Luz")]
    [SerializeField] private GameObject lightArea;
    [SerializeField] private KeyCode toggleKey = KeyCode.L;

    private bool isActive = false;
    [Header("Follow")]
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private float followSpeed = 10f;

    private Vector3 followOffset;
    private float followOffsetAbsX = 0f;
    private PlayerController playerController;

    void Start()
    {
        if (lightArea != null)
            lightArea.SetActive(isActive);

        // tenta auto-atribuir o player se não definido
        if (player == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null)
                player = pc.transform;
        }
        if (player != null)
        {
            followOffset = transform.position - player.position;
            followOffsetAbsX = Mathf.Abs(followOffset.x);
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = FindObjectOfType<PlayerController>();
        }
    }

    void Update()
    {
        ToggleLight();
        HandleFollow();
    }

    void HandleFollow()
    {
        if (!followPlayer || player == null) return;
        bool facingRight = true;
        if (playerController != null)
            facingRight = playerController.IsFacingRight();

        // Mantém o lampião sempre na frente do jogador conforme a direção
        float offsetX = followOffsetAbsX * (facingRight ? 1f : -1f);
        Vector3 targetPos = player.position + new Vector3(offsetX, followOffset.y, followOffset.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        // Espelha a escala do lampião para acompanhar a direção do jogador
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    void ToggleLight()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isActive = !isActive;

            if (lightArea != null)
                lightArea.SetActive(isActive);
        }
    }

    public bool IsLightOn => lightArea != null && lightArea.activeSelf;

    // Exponha a referência à área de luz para outros scripts (somente leitura)
    public GameObject LightArea => lightArea;
}