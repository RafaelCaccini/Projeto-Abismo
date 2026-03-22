using UnityEngine;

public class Lampiao : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 5f;         // Velocidade de seguir o player
    [SerializeField] private float distanceBack = 2f;        // Distância atrás do player enquanto anda
    [SerializeField] private float distanceFront = 5f;       // Distância à frente quando o player para
    [SerializeField] private float followOffsetY = 1f;       // Altura vertical acima do player

    [Header("Movimento Flutuante")]
    [SerializeField] private float floatSpeed = 2f;          // Velocidade da flutuação
    [SerializeField] private float floatHeight = 0.3f;       // Amplitude da flutuação

    [Header("Luz")]
    [SerializeField] public GameObject lightArea;
    [SerializeField] private KeyCode toggleKey = KeyCode.L;

    private bool isActive = false;
    private float floatTimer;
    private Vector3 velocity = Vector3.zero;
    private Vector3 lastPlayerPos;

    void Start()
    {
        if (lightArea != null)
            lightArea.SetActive(isActive);

        if (player != null)
            lastPlayerPos = player.position;
    }

    void Update()
    {
        if (player != null)
            FollowPlayer();

        ToggleLight();
    }

    void FollowPlayer()
    {
        // Direção do player: 1 = direita, -1 = esquerda
        float direction = Mathf.Sign(player.localScale.x);
        if (direction == 0) direction = 1f;

        // Detecta se o player está se movendo
        bool playerMoving = Mathf.Abs(player.position.x - lastPlayerPos.x) > 0.01f;

        // Escolhe distância: atrás se andando, à frente se parado
        float distanceX = playerMoving ? -distanceBack : distanceFront;

        // Calcula posição alvo com flutuação vertical
        Vector3 targetPos = new Vector3(
            player.position.x + direction * distanceX,
            player.position.y + followOffsetY + Mathf.Sin(floatTimer * floatSpeed) * floatHeight,
            transform.position.z
        );

        // SmoothDamp para movimento natural
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.15f);

        // Atualiza lastPlayerPos e timer
        lastPlayerPos = player.position;
        floatTimer += Time.deltaTime;
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
}