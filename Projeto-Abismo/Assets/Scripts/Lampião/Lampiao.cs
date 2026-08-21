using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

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

        // Snap imediato para a posição do player.
        // IMPORTANTE: quando a cena recarrega (respawn), o GameManager
        // já reposicionou o player no checkpoint ANTES de Start() ser chamado,
        // então este snap garante que o lampião nasça junto ao player no checkpoint
        // em vez de voltar para a posição original da cena.
        SnapToPlayer();

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
        // NOTE: entrada do lampião agora é gerenciada pelo PlayerController
        // via PlayerInputHandler (PlayerController.HandleLampiao -> ToggleLuzExterno).
        // Isso permite remapeamento de tecla pelo inspector do PlayerInputHandler.

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

    void SnapToPlayer()
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

        Vector3 snapPos = new Vector3(
            player.position.x + dir,
            player.position.y,
            transform.position.z
        );

        transform.position = snapPos;
        basePosition = snapPos;
    }

    // =====================================
    // LIGHT
    // =====================================

    void HandleLight()
    {
        Gamepad gamepad = Gamepad.current;

        bool teclado = Input.GetKeyDown(toggleLightKey);
        bool controle = gamepad != null && gamepad.buttonNorth.wasPressedThisFrame; // Triângulo PS / Y Xbox

        if (teclado || controle)
        {
            bool estavaAtivo = isActive;
            isActive = !isActive;

            if (lightVisual != null) lightVisual.SetActive(isActive);
            if (lightArea != null) lightArea.SetActive(isActive);
            if (playerController != null) playerController.SetLuz(isActive);

            if (!estavaAtivo && isActive)
                PlayLightSound();

            Debug.Log("[Lampião] Ligado? " + isActive);
        }
    }

    // Adiciona esse método público no Lampiao.cs
    public void ToggleLuzExterno()
    {
        bool estavaAtivo = isActive;
        isActive = !isActive;

        if (lightVisual != null) lightVisual.SetActive(isActive);
        if (lightArea != null) lightArea.SetActive(isActive);
        if (playerController != null) playerController.SetLuz(isActive);

        if (!estavaAtivo && isActive)
            PlayLightSound();

        Debug.Log("[Lampião] Ligado? " + isActive);
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
    // VISUAL: aparecer com fade (usado ao spawnar na cena)
    // =====================================
    // duration em segundos
    public void AparecerComFade(float duration = 1f)
    {
        // Caso o GameObject esteja inativo (foi spawnado/desativado), ativar primeiro
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // garantir referências
        FindPlayer();

        if (lightVisual == null)
            return;

        // garante que area da luz esteja desligada por enquanto
        if (lightArea != null)
            lightArea.SetActive(false);

        // inicia fade (ativa o objeto visual se estiver desativado)
        StartCoroutine(FadeInVisualRoutine(duration));
    }

    private IEnumerator FadeInVisualRoutine(float duration)
    {
        // pega todos os SpriteRenderers do visual (inclui filhos)
        var rends = lightVisual.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originals = new Color[rends.Length];

        for (int i = 0; i < rends.Length; i++)
        {
            originals[i] = rends[i].color;
            Color c = originals[i];
            c.a = 0f;
            rends[i].color = c;
            rends[i].gameObject.SetActive(true);
        }

        lightVisual.SetActive(true);

        float t = 0f;
        while (t < duration)
        {
            float alpha = Mathf.Clamp01(t / duration);
            for (int i = 0; i < rends.Length; i++)
            {
                Color c = originals[i];
                c.a = alpha * originals[i].a;
                rends[i].color = c;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // garante valores finais
        for (int i = 0; i < rends.Length; i++)
        {
            rends[i].color = originals[i];
        }

        // manter visual ativo; não ativa área de luz automaticamente (o jogador deve ligar)
        // se desejar ativar a lightArea junto com o visual, descomente abaixo:
        // if (lightArea != null) lightArea.SetActive(true);
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

