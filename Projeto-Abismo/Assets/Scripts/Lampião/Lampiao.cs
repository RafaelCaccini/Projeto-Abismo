using UnityEngine;
using System.Collections;

public class Lampiao : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;

    [Header("Luz")]
    [SerializeField] private GameObject lightArea;
    [SerializeField] private KeyCode toggleLightKey = KeyCode.L;

    [Header("Movimento")]
    [SerializeField] private KeyCode moveKey = KeyCode.Space;
    [SerializeField] private float moveDistance = 1.5f;     // avanço máximo
    [SerializeField] private float followSpeed = 5f;        // velocidade horizontal
    [SerializeField] private float followOffsetX = 0.5f;    // distância mínima atrás do player
    [SerializeField] private float floatAmplitude = 0.2f;   // altura da flutuação
    [SerializeField] private float floatFrequency = 2f;     // velocidade da flutuação
    [SerializeField] private float advanceTime = 1f;        // tempo de avanço

    private bool isActive = false;
    private float baseY;
    private float floatOffset;
    private Vector3 targetPosition;
    private Coroutine advanceRoutine;

    private PlayerController playerController;

    void Start()
    {
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        baseY = transform.position.y;

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (lightArea != null)
            lightArea.SetActive(isActive);

        UpdateTargetPosition();
    }

    void Update()
    {
        ToggleLight();
        ApplyFloating();

        if (advanceRoutine == null)
        {
            UpdateTargetPosition();
            FollowPlayer();
        }

        if (Input.GetKeyDown(moveKey) && advanceRoutine == null)
        {
            advanceRoutine = StartCoroutine(AdvanceThenReturn());
        }
    }

    void ToggleLight()
    {
        if (Input.GetKeyDown(toggleLightKey))
        {
            isActive = !isActive;
            if (lightArea != null) lightArea.SetActive(isActive);
            if (playerController != null) playerController.SetLuz(isActive);
        }
    }

    void UpdateTargetPosition()
    {
        if (player == null || playerController == null) return;

        // sempre atrás do player, limitado por followOffsetX
        float offsetX = playerController.IsFacingRight() ? -followOffsetX : followOffsetX;
        targetPosition = new Vector3(player.position.x + offsetX, baseY, transform.position.z);
    }

    void FollowPlayer()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    IEnumerator AdvanceThenReturn()
    {
        if (player == null || playerController == null) yield break;

        // posição avançada com clamp para não ultrapassar player + limite
        float advanceX = player.position.x + Mathf.Clamp(playerController.IsFacingRight() ? moveDistance : -moveDistance, -moveDistance, moveDistance);
        Vector3 advancePosition = new Vector3(advanceX, transform.position.y, transform.position.z);

        while (Vector3.Distance(transform.position, advancePosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, advancePosition, followSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(advanceTime);

        UpdateTargetPosition();
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, followSpeed * Time.deltaTime);
            yield return null;
        }

        advanceRoutine = null;
    }

    void ApplyFloating()
    {
        floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, baseY + floatOffset, transform.position.z);
    }

    public bool IsLightOn => isActive;
    public GameObject LightArea => lightArea;
}