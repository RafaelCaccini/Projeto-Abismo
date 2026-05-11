using UnityEngine;
using System.Collections;

public class Lampiao : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;

    [Header("Luz")]
    [SerializeField] private GameObject lightVisual;
    [SerializeField] private GameObject lightArea;
    [SerializeField] private KeyCode toggleLightKey = KeyCode.L;

    [Header("Movimento")]
    [SerializeField] private KeyCode moveKey = KeyCode.Space;
    [SerializeField] private float followOffsetX = 0.6f;
    [SerializeField] private float moveDistance = 1.5f;
    [SerializeField] private float followSpeed = 6f;

    [Header("Flutuação")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatFrequency = 2f;

    [Header("Avanço")]
    [SerializeField] private float advanceTime = 1f;

    private bool isActive;
    private bool isAdvancing;

    private Vector3 basePosition;
    private Vector3 currentTarget;

    private float floatOffsetY;
    private Coroutine advanceRoutine;

    private PlayerController playerController;

    void Start()
    {
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();

            if (pc != null)
                player = pc.transform;
        }

        if (player != null)
            playerController = player.GetComponent<PlayerController>();

        // Começa desligado
        if (lightVisual != null)
            lightVisual.SetActive(false);

        if (lightArea != null)
            lightArea.SetActive(false);
    }

    void Update()
    {
        HandleLight();

        UpdateBaseFollow();

        if (!isAdvancing)
            MoveToBase();

        HandleAdvance();

        ApplyFloat();

        ApplyFinalPosition();
    }

    // ---------------- LIGHT ----------------
    void HandleLight()
    {
        if (Input.GetKeyDown(toggleLightKey))
        {
            isActive = !isActive;

            // Visual da luz
            if (lightVisual != null)
                lightVisual.SetActive(isActive);

            // Área que detecta espinhos
            if (lightArea != null)
                lightArea.SetActive(isActive);

            if (playerController != null)
                playerController.SetLuz(isActive);

            Debug.Log("Lampião ligado? " + isActive);
        }
    }

    // ---------------- FOLLOW ----------------
    void UpdateBaseFollow()
    {
        if (player == null || playerController == null)
            return;

        float dir =
            playerController.IsFacingRight()
            ? followOffsetX
            : -followOffsetX;

        basePosition = new Vector3(
            player.position.x + dir,
            player.position.y,
            transform.position.z
        );
    }

    void MoveToBase()
    {
        currentTarget = basePosition;

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget,
            followSpeed * Time.deltaTime
        );
    }

    // ---------------- ADVANCE ----------------
    void HandleAdvance()
    {
        if (Input.GetKeyDown(moveKey) && advanceRoutine == null)
        {
            advanceRoutine = StartCoroutine(AdvanceRoutine());
        }
    }

    IEnumerator AdvanceRoutine()
    {
        isAdvancing = true;

        float dir =
            playerController != null &&
            playerController.IsFacingRight()
            ? 1f
            : -1f;

        Vector3 advanceTarget = new Vector3(
            basePosition.x + dir * moveDistance,
            basePosition.y,
            transform.position.z
        );

        while (Vector3.Distance(transform.position, advanceTarget) > 0.02f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                advanceTarget,
                followSpeed * Time.deltaTime
            );

            yield return null;
        }

        yield return new WaitForSeconds(advanceTime);

        isAdvancing = false;
        advanceRoutine = null;
    }

    // ---------------- FLOAT ----------------
    void ApplyFloat()
    {
        floatOffsetY =
            Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
    }

    // ---------------- FINAL POSITION ----------------
    void ApplyFinalPosition()
    {
        Vector3 p = transform.position;

        p.y = basePosition.y + floatOffsetY;

        transform.position = p;
    }

    // ---------------- API ----------------
    public bool IsLightOn => isActive;

    public GameObject LightArea => lightArea;
}