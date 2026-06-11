using UnityEngine;
using System.Collections;

public class Lampiao : MonoBehaviour
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("Referências")]
    [SerializeField] private Transform player;

    [SerializeField] private PlayerController playerController;

    // =====================================
    // LUZ
    // =====================================

    [Header("Luz")]
    [SerializeField] private GameObject lightVisual;

    [SerializeField] private GameObject lightArea;

    [SerializeField] private KeyCode toggleLightKey = KeyCode.L;

    // =====================================
    // ÁUDIO
    // =====================================

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip ligarSom;

    // =====================================
    // MOVIMENTO
    // =====================================

    [Header("Movimento")]
    [SerializeField] private KeyCode moveKey = KeyCode.Space;

    [SerializeField] private float followOffsetX = 0.6f;

    [SerializeField] private float moveDistance = 1.5f;

    [SerializeField] private float followSpeed = 6f;

    // =====================================
    // FLUTUAÇÃO
    // =====================================

    [Header("Flutuação")]
    [SerializeField] private float floatAmplitude = 0.2f;

    [SerializeField] private float floatFrequency = 2f;

    // =====================================
    // AVANÇO
    // =====================================

    [Header("Avanço")]
    [SerializeField] private float advanceTime = 1f;

    // =====================================
    // CONTROLE
    // =====================================

    private bool isActive;

    private bool isAdvancing;

    private Vector3 basePosition;

    private Vector3 currentTarget;

    private float floatOffsetY;

    private Coroutine advanceRoutine;

    // =====================================
    // START
    // =====================================

    void Start()
    {
        FindPlayer();

        // Começa desligado
        if (lightVisual != null)
            lightVisual.SetActive(false);

        if (lightArea != null)
            lightArea.SetActive(false);

        // Segurança do áudio
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    // =====================================
    // UPDATE
    // =====================================

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

    // =====================================
    // PLAYER
    // =====================================

    void FindPlayer()
    {
        if (player == null)
        {
            PlayerController pc =
                FindFirstObjectByType<PlayerController>();

            if (pc != null)
            {
                player = pc.transform;
                playerController = pc;
            }
        }
    }

    // =====================================
    // LIGHT
    // =====================================

    void HandleLight()
    {
        if (Input.GetKeyDown(toggleLightKey))
        {
            bool estavaAtivo = isActive;

            isActive = !isActive;

            // Visual da luz
            if (lightVisual != null)
                lightVisual.SetActive(isActive);

            // Área da luz
            if (lightArea != null)
                lightArea.SetActive(isActive);

            // Atualiza player
            if (playerController != null)
                playerController.SetLuz(isActive);

            // SOMENTE AO LIGAR
            if (!estavaAtivo && isActive)
            {
                PlayLightSound();
            }

            Debug.Log(
                "[Lampião] Ligado? " + isActive
            );
        }
    }

    // =====================================
    // SOM
    // =====================================

    void PlayLightSound()
    {
        if (
            audioSource == null ||
            ligarSom == null
        )
            return;

        audioSource.PlayOneShot(ligarSom);
    }

    // =====================================
    // FOLLOW
    // =====================================

    void UpdateBaseFollow()
    {
        if (
            player == null ||
            playerController == null
        )
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

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                currentTarget,
                followSpeed * Time.deltaTime
            );
    }

    // =====================================
    // ADVANCE
    // =====================================

    void HandleAdvance()
    {
        if (
            Input.GetKeyDown(moveKey) &&
            advanceRoutine == null
        )
        {
            advanceRoutine =
                StartCoroutine(
                    AdvanceRoutine()
                );
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

        Vector3 advanceTarget =
            new Vector3(
                basePosition.x + dir * moveDistance,
                basePosition.y,
                transform.position.z
            );

        while (
            Vector3.Distance(
                transform.position,
                advanceTarget
            ) > 0.02f
        )
        {
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    advanceTarget,
                    followSpeed * Time.deltaTime
                );

            yield return null;
        }

        yield return new WaitForSeconds(
            advanceTime
        );

        isAdvancing = false;

        advanceRoutine = null;
    }

    // =====================================
    // FLOAT
    // =====================================

    void ApplyFloat()
    {
        floatOffsetY =
            Mathf.Sin(
                Time.time * floatFrequency
            ) * floatAmplitude;
    }

    // =====================================
    // FINAL POSITION
    // =====================================

    void ApplyFinalPosition()
    {
        Vector3 p = transform.position;

        p.y =
            basePosition.y + floatOffsetY;

        transform.position = p;
    }

    // =====================================
    // API
    // =====================================

    public bool IsLightOn => isActive;

    public GameObject LightArea => lightArea;
}

