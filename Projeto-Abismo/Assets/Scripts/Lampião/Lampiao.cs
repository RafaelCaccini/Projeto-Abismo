using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

// =====================================
// Enum de modos do Lampião (Flags)
// Usado para configurar quais modos estão
// disponíveis em cada fase via Inspector
// =====================================

[System.Flags]
public enum LampiaoMode
{
    None = 0,
    Normal = 1 << 0,
    Afastar = 1 << 1,
    Atrair = 1 << 2,
    Paralisar = 1 << 3,
}

public class Lampiao : MonoBehaviour
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("Referências")]
    [SerializeField] private Transform player;

    [SerializeField] private PlayerController playerController;

    // =====================================
    // MODOS DISPONÍVEIS
    // =====================================

    [Header("Modos Disponíveis")]
    [Tooltip("Quais modos do Lampião estão habilitados nesta fase. Configure aqui no Inspector ou deixe o PlayerController aplicar via habilidades do jogador.")]
    [SerializeField] private LampiaoMode modesAvailable = LampiaoMode.Normal;

    // =====================================
    // LUZ
    // =====================================

    [Header("Luz")]
    [SerializeField] private GameObject lightVisual;

    [SerializeField] private GameObject lightArea;

    [SerializeField] private KeyCode toggleLightKey = KeyCode.L;

    // Expose keys so other scripts (ex: PlayerController) can respect
    // the Inspector-configured KeyCodes even when a centralized Input
    // handler (PlayerInputHandler) exists.
    public KeyCode ToggleLightKey => toggleLightKey;

    // =====================================
    // CORES POR MODO
    // =====================================

    [Header("Cores")]
    [Tooltip("Cor do Lampião no modo Normal.")]
    [SerializeField] private Color corNormal = new Color(1f, 0.9f, 0.5f);

    [Tooltip("Cor do Lampião no modo Afastar.")]
    [SerializeField] private Color corAfastar = new Color(1f, 0.3f, 0.3f);

    [Tooltip("Cor do Lampião no modo Atrair.")]
    [SerializeField] private Color corAtrair = new Color(0.3f, 0.6f, 1f);

    [Tooltip("Cor do Lampião no modo Paralisar.")]
    [SerializeField] private Color corParalisar = new Color(0.8f, 0.8f, 1f);

    // =====================================
    // MODOS: AFASTAMENTO / ATRAÇÃO
    // =====================================

    [Header("Modos: Afastar / Atrair")]
    [Tooltip("Força aplicada para afastar inimigos.")]
    [SerializeField] private float repelForce = 8f;

    [Tooltip("Força aplicada para atrair inimigos.")]
    [SerializeField] private float attractForce = 5f;

    [Tooltip("Raio de detecção de inimigos para os modos Afastar/Atrair/Paralisar. Se -1, usa o raio do CircleCollider2D da lightArea.")]
    [SerializeField] private float detectRadius = -1f;

    [Tooltip("LayerMask para filtrar inimigos na detecção por Overlap.")]
    [SerializeField] private LayerMask enemyLayer = 0;

    [Tooltip("Tag usada para identificar inimigos.")]
    [SerializeField] private string enemyTag = "Enemy";


    // =====================================
    // MODOS: PARALISAR
    // =====================================

    [Header("Modos: Paralisar")]
    [Tooltip("Tecla para ativar/desativar o modo Paralisar (configurado no inspector do PlayerInputHandler).")]
    [SerializeField] private KeyCode paralisarKey = KeyCode.Q;

    [Tooltip("Tecla para alternar entre os modos Normais (Normal → Afastar → Atrair).")]
    [SerializeField] private KeyCode alternarModoKey = KeyCode.Tab;

    public KeyCode ParalisarKey => paralisarKey;

    public KeyCode AlternarModoKey => alternarModoKey;

    [Tooltip("Duração da paralisação em segundos.")]
    [SerializeField] private float stunDuration = 3f;

    [Tooltip("Tempo de recarga (cooldown) da habilidade em segundos.")]
    [SerializeField] private float paralisarCooldown = 8f;


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

    // --- Estado do modo ---
    private LampiaoMode currentMode = LampiaoMode.Normal;

    private bool isParalisarActive = false;

    // --- Controle de Cooldown ---
    private float nextParalisarTime = 0f;


    // Salva as cores originais dos SpriteRenderers do lightVisual
    // para aplicar cor de modo sem perder o alpha original
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();


    // Dados para restauração de inimigos paralisados genericamente
    private Coroutine paralisarRoutine;

    // =====================================
    // START
    // =====================================

    void Start()
    {
        FindPlayer();

        // Snap imediato para a posição do player.
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

        // Salva cores originais dos SpriteRenderers
        SaveOriginalColors();
    }

    // =====================================
    // UPDATE
    // =====================================

    void Update()
    {
        UpdateBaseFollow();

        if (!isAdvancing)
            MoveToBase();

        HandleAdvance();

        ApplyFloat();

        ApplyFinalPosition();

        // Entrada local (teclado configurável no Inspector) —
        // Apenas quando não existe PlayerInputHandler global.
        if (PlayerInputHandler.Instance == null)
        {
            HandleLight();

            // Alternar modos
            if (isActive && Input.GetKeyDown(alternarModoKey))
            {
                AlternarModo();
            }

            // Paralisar
            if (isActive && Input.GetKeyDown(paralisarKey))
            {
                AtivarParalisar();
            }
        }
    }

    // =====================================
    // PLAYER
    // =====================================

    void FindPlayer()
    {
        if (player == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();

            if (pc != null)
            {
                player = pc.transform;
                playerController = pc;
            }
        }
    }

    void SnapToPlayer()
    {
        if (player == null || playerController == null)
            return;

        float dir = playerController.IsFacingRight() ? followOffsetX : -followOffsetX;

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
        bool controle = gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;

        if (teclado || controle)
        {
            ToggleLuzExterno();
        }
    }

    public void ToggleLuzExterno()
    {
        bool estavaAtivo = isActive;
        isActive = !isActive;

        if (lightVisual != null) lightVisual.SetActive(isActive);
        if (lightArea != null) lightArea.SetActive(isActive);
        if (playerController != null) playerController.SetLuz(isActive);

        if (isActive)
            ApplyModeColor();

        // Se a paralisar estiver em execução, para a rotina e restaura cor
        if (!isActive && paralisarRoutine != null)
        {
            StopCoroutine(paralisarRoutine);
            paralisarRoutine = null;

            isParalisarActive = false;
            ApplyModeColor();
        }

        if (!estavaAtivo && isActive)
            PlayLightSound();

        Debug.Log("[Lâmpiao] Ligado? " + isActive);
    }

    // =====================================
    // SOM
    // =====================================

    void PlayLightSound()
    {
        if (audioSource == null || ligarSom == null)
            return;

        audioSource.PlayOneShot(ligarSom);
    }

    // =====================================
    // VISUAL: FADE
    // =====================================

    public void AparecerComFade(float duration = 1f)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        FindPlayer();

        if (lightVisual == null)
            return;

        if (lightArea != null)
            lightArea.SetActive(false);

        StartCoroutine(FadeInVisualRoutine(duration));
    }

    private IEnumerator FadeInVisualRoutine(float duration)
    {
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

        for (int i = 0; i < rends.Length; i++)
        {
            rends[i].color = originals[i];
        }

        SaveOriginalColors();
        ApplyModeColor();
    }

    // =====================================
    // FOLLOW
    // =====================================

    void UpdateBaseFollow()
    {
        if (player == null || playerController == null)
            return;

        float dir = playerController.IsFacingRight() ? followOffsetX : -followOffsetX;

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

    // =====================================
    // ADVANCE
    // =====================================

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

        float dir = (playerController != null && playerController.IsFacingRight()) ? 1f : -1f;

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

    // =====================================
    // FLOAT
    // =====================================

    void ApplyFloat()
    {
        floatOffsetY = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
    }

    // =====================================
    // FINAL POSITION
    // =====================================

    void ApplyFinalPosition()
    {
        Vector3 p = transform.position;
        p.y = basePosition.y + floatOffsetY;
        transform.position = p;
    }

    // =====================================
    // FIXED UPDATE: AFASTAR / ATRAIR
    // =====================================

    void FixedUpdate()
    {
        if (!isActive)
            return;

        if (isParalisarActive || currentMode == LampiaoMode.Normal)
            return;

        if ((currentMode == LampiaoMode.Afastar && (modesAvailable & LampiaoMode.Afastar) != 0) ||
            (currentMode == LampiaoMode.Atrair && (modesAvailable & LampiaoMode.Atrair) != 0))
        {
            ApplyAfastarAtrair();
        }
    }

    void ApplyAfastarAtrair()
    {
        float radius = GetDetectRadius();
        Vector2 origin = new Vector2(transform.position.x, transform.position.y);

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            origin,
            radius,
            enemyLayer != 0 ? enemyLayer : Physics2D.DefaultRaycastLayers
        );

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!hit.CompareTag(enemyTag)) continue;

            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            Vector2 dir = ((Vector2)hit.transform.position - origin).normalized;
            float force = currentMode == LampiaoMode.Afastar ? repelForce : attractForce;
            float signedForce = currentMode == LampiaoMode.Afastar ? -force : force;

            rb.AddForce(dir * signedForce, ForceMode2D.Force);
        }
    }

    // =====================================
    // MODOS DO LAMPIÃO
    // =====================================

    public void AlternarModo()
    {
        if (!isActive)
            return;

        LampiaoMode cicloModes = modesAvailable & (LampiaoMode.Normal | LampiaoMode.Afastar | LampiaoMode.Atrair);

        if (cicloModes == LampiaoMode.None)
        {
            currentMode = LampiaoMode.Normal;
            return;
        }

        List<LampiaoMode> possible = new List<LampiaoMode>();
        if ((cicloModes & LampiaoMode.Normal) != 0) possible.Add(LampiaoMode.Normal);
        if ((cicloModes & LampiaoMode.Afastar) != 0) possible.Add(LampiaoMode.Afastar);
        if ((cicloModes & LampiaoMode.Atrair) != 0) possible.Add(LampiaoMode.Atrair);

        int currentIndex = possible.IndexOf(currentMode);
        int nextIndex = (currentIndex + 1) % possible.Count;

        if (currentIndex < 0)
            nextIndex = 0;

        currentMode = possible[nextIndex];
        ApplyModeColor();

        Debug.Log("[Lâmpiao] Modo atual: " + currentMode);
    }

    /// <summary>
    /// Ativa o modo Paralisar temporariamente com verificação de cooldown.
    /// </summary>
    public void AtivarParalisar()
    {
        if (!isActive)
        {
            Debug.Log("[Lampião] O Lampião precisa estar ligado para paralisar.");
            return;
        }

        // Verifica se Paralisar está liberado nesta fase
        if ((modesAvailable & LampiaoMode.Paralisar) == 0)
        {
            Debug.Log("[Lampião] Paralisar não está disponível nesta fase.");
            return;
        }

        // Verifica se está em cooldown
        if (Time.time < nextParalisarTime)
        {
            float tempoRestante = nextParalisarTime - Time.time;
            Debug.Log($"[Lampião] Paralisar em Cooldown! Faltam {tempoRestante:F1}s.");
            return;
        }

        // Não deixa iniciar duas vezes
        if (paralisarRoutine != null)
            return;

        paralisarRoutine = StartCoroutine(ParalisarRoutine());
    }

    public void ToggleParalisar()
    {
        AtivarParalisar();
    }

    private IEnumerator ParalisarRoutine()
    {
        isParalisarActive = true;

        // Inicia a contagem de cooldown
        nextParalisarTime = Time.time + paralisarCooldown;

        // Muda a cor imediatamente
        ApplyModeColor();

        // Paralisa os inimigos atuais dentro da área
        ApplyStunToEnemiesInLightArea();

        Debug.Log("[Lampião] PARALISAR ativado!");

        // Mantém a cor durante a duração do stun
        yield return new WaitForSeconds(stunDuration);

        isParalisarActive = false;

        // Volta para a cor do modo anterior
        ApplyModeColor();

        paralisarRoutine = null;

        Debug.Log("[Lampião] PARALISAR terminou.");
    }

    private void ApplyStunToEnemiesInLightArea()
    {
        if (lightArea == null)
        {
            Debug.LogWarning("[Lampião] LightArea não foi configurada.");
            return;
        }

        Collider2D areaCollider = lightArea.GetComponent<Collider2D>();

        if (areaCollider == null)
        {
            Debug.LogWarning("[Lampião] LightArea precisa possuir um Collider2D.");
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = false;
        filter.useDepth = false;

        List<Collider2D> resultados = new List<Collider2D>();
        areaCollider.Overlap(filter, resultados);

        HashSet<GameObject> inimigosProcessados = new HashSet<GameObject>();

        foreach (Collider2D hit in resultados)
        {
            if (hit == null)
                continue;

            GameObject enemyObject = hit.gameObject;

            if (!enemyObject.CompareTag(enemyTag))
            {
                Transform atual = hit.transform;

                while (atual != null)
                {
                    if (atual.CompareTag(enemyTag))
                    {
                        enemyObject = atual.gameObject;
                        break;
                    }

                    atual = atual.parent;
                }
            }

            if (!enemyObject.CompareTag(enemyTag))
                continue;

            if (!inimigosProcessados.Add(enemyObject))
                continue;

            StunEnemy(enemyObject);
        }
    }

    private void StunEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        IAtordoavel atordoavel = enemy.GetComponent<IAtordoavel>() ?? enemy.GetComponentInParent<IAtordoavel>();

        if (atordoavel != null)
        {
            atordoavel.Atordoar(stunDuration);
            Debug.Log($"[Lampião] Inimigo atordoado: {enemy.name} por {stunDuration}s.");
            return;
        }

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>() ?? enemy.GetComponentInParent<Rigidbody2D>();

        if (rb != null)
        {
            StartCoroutine(FreezeRigidbodyRoutine(rb, stunDuration));
            Debug.Log($"[Lampião] Rigidbody congelado: {enemy.name}");
        }
        else
        {
            Debug.LogWarning($"[Lampião] {enemy.name} não possui IAtordoavel nem Rigidbody2D.");
        }
    }

    private IEnumerator FreezeRigidbodyRoutine(Rigidbody2D rb, float duration)
    {
        if (rb == null)
            yield break;

        RigidbodyConstraints2D constraintsOriginais = rb.constraints;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        yield return new WaitForSeconds(duration);

        if (rb != null)
        {
            rb.constraints = constraintsOriginais;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void ConfigureModes(LampiaoMode modes)
    {
        modesAvailable = modes;

        if ((modesAvailable & currentMode) == 0 && (modesAvailable & LampiaoMode.Normal) != 0)
            currentMode = LampiaoMode.Normal;
        else if (modesAvailable == LampiaoMode.None)
            currentMode = LampiaoMode.Normal;
    }

    // =====================================
    // PROPRIEDADES / GETTERS
    // =====================================

    public LampiaoMode ModesAvailable => modesAvailable;
    public bool IsLightOn => isActive;
    public GameObject LightArea => lightArea;
    public bool IsParalisarActive => isParalisarActive;
    public LampiaoMode CurrentMode => currentMode;

    // --- Getters de Cooldown para UI ---
    public bool IsParalisarOnCooldown => Time.time < nextParalisarTime;
    public float ParalisarCooldownRemaining => Mathf.Max(0f, nextParalisarTime - Time.time);
    public float ParalisarCooldownProgress => paralisarCooldown > 0f ? Mathf.Clamp01(ParalisarCooldownRemaining / paralisarCooldown) : 0f;

    // =====================================
    // CORES
    // =====================================

    void SaveOriginalColors()
    {
        if (lightVisual == null) return;

        var rends = lightVisual.GetComponentsInChildren<SpriteRenderer>(true);
        originalColors.Clear();
        foreach (var rend in rends)
        {
            originalColors[rend] = rend.color;
        }
    }

    void ApplyModeColor()
    {
        if (lightVisual == null) return;

        Color targetColor = corNormal;

        if (isParalisarActive)
            targetColor = corParalisar;
        else if (currentMode == LampiaoMode.Afastar)
            targetColor = corAfastar;
        else if (currentMode == LampiaoMode.Atrair)
            targetColor = corAtrair;
        else
            targetColor = corNormal;

        var rends = lightVisual.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var rend in rends)
        {
            Color original = originalColors.ContainsKey(rend) ? originalColors[rend] : rend.color;
            Color newColor = new Color(targetColor.r, targetColor.g, targetColor.b, original.a);
            rend.color = newColor;
        }
    }

    // =====================================
    // DETECÇÃO
    // =====================================

    float GetDetectRadius()
    {
        if (lightArea != null)
        {
            CircleCollider2D circle = lightArea.GetComponent<CircleCollider2D>();
            if (circle != null)
                return circle.radius * Mathf.Max(lightArea.transform.lossyScale.x, lightArea.transform.lossyScale.y);
        }

        if (detectRadius > 0f)
            return detectRadius;

        return 3f;
    }
}