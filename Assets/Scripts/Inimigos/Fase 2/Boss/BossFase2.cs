using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss da Fase 2 - Implementa três comportamentos principais:
/// 1. Voar - Movimento aéreo com animações de voo
/// 2. Fogo no Chão - Fogo surge do chão, jogador pula para desviar
/// 3. Projétil - Tiro disparado de canto da arena, trajetória A→B
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossFase2 : MonoBehaviour, IDamageable
{
    // =====================================
    // ENUMS E ESTADOS
    // =====================================

    public enum BossState
    {
        Intro,
        Idle,
        Flying,
        GroundFireAttack,
        ProjectileAttack,
        Transition,
        Death
    }

    public enum AttackPattern
    {
        None,
        GroundFire,
        Projectile,
        Combo
    }

    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("REFERÊNCIAS")]
    [Tooltip("Animator do boss (para animações de voo, ataques, morte)")]
    [SerializeField] private Animator animator;

    [Tooltip("SpriteRenderer principal (para flip e efeitos visuais)")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Transform do jogador (se nulo, busca por Tag 'Player')")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("AudioSource para efeitos sonoros")]
    [SerializeField] private AudioSource audioSource;

    [Header("PONTOS DE SPAWN DE ATAQUES")]
    [Tooltip("Pontos dos cantos da arena de onde saem os projéteis")]
    [SerializeField] private Transform[] arenaCorners;

    [Tooltip("Pontos de spawn do fogo no chão (distribuídos pelo chão da arena)")]
    [SerializeField] private Transform[] groundFireSpawnPoints;

    [Tooltip("Ponto central onde o boss fica durante o idle/voo")]
    [SerializeField] private Transform centerPoint;

    [Tooltip("Pontos de patrulha aérea (para movimento de voo)")]
    [SerializeField] private Transform[] flightPatrolPoints;

    // =====================================
    // VOO (MOVIMENTO AÉREO)
    // =====================================

    [Header("VOO - MOVIMENTO AÉREO")]
    [SerializeField] private float flySpeed = 4f;
    [SerializeField] private float flyAcceleration = 2f;
    [SerializeField] private float hoverAmplitude = 0.5f;      // Amplitude da oscilação vertical (sensação de voo)
    [SerializeField] private float hoverFrequency = 1f;        // Frequência da oscilação
    [SerializeField] private float minFlyHeight = 2f;          // Altura mínima do voo
    [SerializeField] private float maxFlyHeight = 5f;          // Altura máxima do voo
    [SerializeField] private float turnSmoothTime = 0.3f;      // Suavização ao virar
    [SerializeField] private bool enableWingFlapAnimation = true; // Ativar animação de bater asas

    // =====================================
    // ATAQUE: FOGO NO CHÃO
    // =====================================

    [Header("ATAQUE: FOGO NO CHÃO")]
    [SerializeField] private GameObject groundFirePrefab;      // Prefab do efeito de fogo
    [SerializeField] private int fireSpawnCount = 3;           // Quantos pontos de fogo spawnam por ataque
    [SerializeField] private float fireWarningTime = 1.5f;     // Tempo de aviso antes do fogo aparecer
    [SerializeField] private float fireDuration = 2f;          // Tempo que o fogo fica ativo
    [SerializeField] private float fireFadeTime = 0.5f;        // Tempo para o fogo descer/desaparecer
    [SerializeField] private float fireDamage = 2;             // Dano do fogo
    [SerializeField] private LayerMask playerLayer;            // Layer do jogador
    [SerializeField] private Color fireWarningColor = new Color(1f, 0.5f, 0f, 0.5f); // Cor do aviso
    [SerializeField] private GameObject fireWarningPrefab;     // Prefab opcional de aviso visual
    [SerializeField] private float groundFireCooldown = 8f;    // Cooldown entre ataques de fogo

    // =====================================
    // ATAQUE: PROJÉTIL
    // =====================================

    [Header("ATAQUE: PROJÉTIL")]
    [SerializeField] private GameObject projectilePrefab;      // Prefab do projétil
    [SerializeField] private float projectileSpeed = 10f;      // Velocidade do projétil
    [SerializeField] private float projectileWarningTime = 1f; // Tempo de aviso antes do disparo
    [SerializeField] private int projectilesPerAttack = 2;     // Quantos projéteis por ataque
    [SerializeField] private float projectileInterval = 0.5f;  // Intervalo entre projéteis múltiplos
    [SerializeField] private float projectileDamage = 1;       // Dano do projétil
    [SerializeField] private float projectileLifetime = 5f;    // Tempo de vida do projétil
    [SerializeField] private GameObject projectileWarningPrefab; // Prefab de aviso no canto
    [SerializeField] private float projectileCooldown = 6f;    // Cooldown entre ataques de projétil

    // =====================================
    // VIDA E FASES
    // =====================================

    [Header("VIDA E FASES")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int phase2HealthThreshold = 60;   // Vida para entrar na Fase 2 (mais agressivo)
    [SerializeField] private int phase3HealthThreshold = 30;   // Vida para entrar na Fase 3 (muito agressivo)
    [SerializeField] private bool canDie = true;
    [SerializeField] private float invulnerabilityTime = 0.5f; // Tempo de invulnerabilidade ao tomar dano

    // =====================================
    // DANO POR CONTATO
    // =====================================

    [Header("DANO POR CONTATO")]
    [SerializeField] private int contactDamage = 2;
    [SerializeField] private float contactCooldown = 1f;

    // =====================================
    // ÁUDIO
    // =====================================

    [Header("ÁUDIO")]
    [SerializeField] private AudioClip sfxFlyLoop;
    [SerializeField] private AudioClip sfxGroundFireWarning;
    [SerializeField] private AudioClip sfxGroundFireSpawn;
    [SerializeField] private AudioClip sfxProjectileWarning;
    [SerializeField] private AudioClip sfxProjectileShoot;
    [SerializeField] private AudioClip sfxTakeDamage;
    [SerializeField] private AudioClip sfxDeath;
    [SerializeField] private AudioClip sfxPhaseTransition;

    // =====================================
    // DEBUG
    // =====================================

    [Header("DEBUG")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool forcePhase = 0; // 0=normal, 1=phase2, 2=phase3 (para teste)

    // =====================================
    // CONSTANTES DE ANIMAÇÃO
    // =====================================

    private static readonly int Anim_Flying = Animator.StringToHash("Flying");
    private static readonly int Anim_GroundFire = Animator.StringToHash("GroundFire");
    private static readonly int Anim_ProjectileAttack = Animator.StringToHash("ProjectileAttack");
    private static readonly int Anim_TakeDamage = Animator.StringToHash("TakeDamage");
    private static readonly int Anim_Death = Animator.StringToHash("Death");
    private static readonly int Anim_PhaseTransition = Animator.StringToHash("PhaseTransition");
    private static readonly int Anim_Speed = Animator.StringToHash("Speed");
    private static readonly int Anim_WingFlap = Animator.StringToHash("WingFlap");

    // =====================================
    // COMPONENTES E VARIÁVEIS INTERNAS
    // =====================================

    private Rigidbody2D rb;
    private Collider2D col;
    private BossState currentState = BossState.Intro;
    private AttackPattern nextAttack = AttackPattern.None;

    private int currentHealth;
    private int currentPhase = 1;
    private bool isDead = false;
    private bool isInvulnerable = false;
    private float lastDamageTime = -999f;
    private float lastContactDamageTime = -999f;

    // Voo
    private int currentPatrolIndex = 0;
    private Vector2 targetFlyPosition;
    private float hoverTimer = 0f;
    private bool movingToTarget = false;
    private float turnVelocity = 0f;

    // Cooldowns de ataque
    private float groundFireCooldownTimer = 0f;
    private float projectileCooldownTimer = 0f;
    private float attackDecisionTimer = 0f;
    private float stateTimer = 0f;

    // Referências ativas
    private List<GameObject> activeFireWarnings = new List<GameObject>();
    private List<GameObject> activeFires = new List<GameObject>();
    private List<GameObject> activeProjectileWarnings = new List<GameObject>();

    // Player
    private PlayerController playerController;

    // =====================================
    // PROPRIEDADES PÚBLICAS
    // =====================================

    public BossState CurrentState => currentState;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentPhase => currentPhase;
    public float HealthPercentage => (float)currentHealth / maxHealth;

    // =====================================
    // UNITY LIFECYCLE
    // =====================================

    private void Awake()
    {
        // Componentes obrigatórios
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb == null || col == null)
        {
            Debug.LogError("[BossFase2] Rigidbody2D ou Collider2D ausente!");
            enabled = false;
            return;
        }

        // Configuração de física para boss voador
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 5f;
        rb.linearDamping = 2f; // Resistência do ar para movimento suave

        // Busca componentes opcionais
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0.5f;
        }

        // Busca player se não atribuído
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // Cache do PlayerController
        if (playerTransform != null)
            playerController = playerTransform.GetComponent<PlayerController>();

        // Validação de arrays
        ValidateArrays();

        if (debugLogs)
            Debug.Log("[BossFase2] Awake completo");
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentPhase = CalculatePhase(currentHealth);

        // Posiciona no centro se definido
        if (centerPoint != null)
        {
            transform.position = centerPoint.position;
            targetFlyPosition = centerPoint.position;
        }
        else if (flightPatrolPoints.Length > 0)
        {
            targetFlyPosition = flightPatrolPoints[0].position;
        }

        // Inicia estado de introdução
        StartCoroutine(IntroSequence());

        if (debugLogs)
            Debug.Log($"[BossFase2] Iniciado | Vida: {currentHealth}/{maxHealth} | Fase: {currentPhase}");
    }

    private void Update()
    {
        if (isDead) return;

        // Atualiza timers
        UpdateTimers();

        // Verifica transição de fase
        CheckPhaseTransition();

        // Busca player se perdido
        if (playerTransform == null)
            FindPlayer();

        // Máquina de estados
        StateMachineUpdate();

        // Atualiza animações
        UpdateAnimations();

        // Debug input para teste
        HandleDebugInput();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // Movimento de voo no FixedUpdate para física suave
        if (currentState == BossState.Flying || currentState == BossState.Idle)
        {
            HandleFlightMovement();
        }
    }

    private void OnDestroy()
    {
        // Limpa objetos ativos
        CleanupActiveObjects();
    }

    // =====================================
    // VALIDAÇÃO E SETUP
    // =====================================

    private void ValidateArrays()
    {
        // Se não tem cantos da arena, cria pontos padrão
        if (arenaCorners == null || arenaCorners.Length == 0)
        {
            CreateDefaultArenaCorners();
        }

        // Se não tem pontos de fogo, cria padrão
        if (groundFireSpawnPoints == null || groundFireSpawnPoints.Length == 0)
        {
            CreateDefaultFireSpawnPoints();
        }

        // Se não tem pontos de patrulha aérea, cria padrão
        if (flightPatrolPoints == null || flightPatrolPoints.Length == 0)
        {
            CreateDefaultFlightPatrolPoints();
        }
    }

    private void CreateDefaultArenaCorners()
    {
        // Cria 4 cantos padrão ao redor do boss
        Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
        float arenaWidth = 10f;
        float arenaHeight = 6f;

        arenaCorners = new Transform[4];
        string[] cornerNames = { "Corner_BottomLeft", "Corner_BottomRight", "Corner_TopLeft", "Corner_TopRight" };
        Vector3[] cornerOffsets = {
            new Vector3(-arenaWidth/2, -arenaHeight/2, 0),
            new Vector3(arenaWidth/2, -arenaHeight/2, 0),
            new Vector3(-arenaWidth/2, arenaHeight/2, 0),
            new Vector3(arenaWidth/2, arenaHeight/2, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject corner = new GameObject(cornerNames[i]);
            corner.transform.position = center + cornerOffsets[i];
            corner.transform.SetParent(transform, worldPositionStays: true);
            arenaCorners[i] = corner.transform;
        }
    }

    private void CreateDefaultFireSpawnPoints()
    {
        // Cria pontos de fogo distribuídos pelo chão
        Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
        float arenaWidth = 10f;
        int pointCount = 5;

        groundFireSpawnPoints = new Transform[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            float x = Mathf.Lerp(-arenaWidth/2 + 1f, arenaWidth/2 - 1f, t);

            GameObject point = new GameObject($"FireSpawn_{i}");
            point.transform.position = center + new Vector3(x, -3f, 0); // Chão em y = -3
            point.transform.SetParent(transform, worldPositionStays: true);
            groundFireSpawnPoints[i] = point.transform;
        }
    }

    private void CreateDefaultFlightPatrolPoints()
    {
        // Cria pontos de patrulha aérea
        Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
        float width = 6f;
        float height = 3f;

        flightPatrolPoints = new Transform[4];
        Vector3[] offsets = {
            new Vector3(-width/2, height/2, 0),
            new Vector3(width/2, height/2, 0),
            new Vector3(width/2, -height/2, 0),
            new Vector3(-width/2, -height/2, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject point = new GameObject($"FlightPatrol_{i}");
            point.transform.position = center + offsets[i];
            point.transform.SetParent(transform, worldPositionStays: true);
            flightPatrolPoints[i] = point.transform;
        }
    }

    // =====================================
    // INTRO E FASE TRANSITION
    // =====================================

    private IEnumerator IntroSequence()
    {
        currentState = BossState.Intro;

        // Animação de entrada
        if (animator != null)
        {
            animator.SetTrigger("Intro");
        }

        // Som de entrada
        PlaySound(sfxPhaseTransition);

        // Tempo de intro
        yield return new WaitForSeconds(2f);

        // Vai para idle/voo
        ChangeState(BossState.Idle);
    }

    private void CheckPhaseTransition()
    {
        int newPhase = CalculatePhase(currentHealth);

        if (forcePhase > 0) newPhase = forcePhase;

        if (newPhase > currentPhase)
        {
            currentPhase = newPhase;
            StartCoroutine(PhaseTransitionSequence());
        }
    }

    private int CalculatePhase(int health)
    {
        if (health <= phase3HealthThreshold) return 3;
        if (health <= phase2HealthThreshold) return 2;
        return 1;
    }

    private IEnumerator PhaseTransitionSequence()
    {
        currentState = BossState.Transition;

        // Para movimento
        rb.linearVelocity = Vector2.zero;

        // Animação de transição
        if (animator != null)
        {
            animator.SetTrigger(Anim_PhaseTransition);
            animator.SetInteger("Phase", currentPhase);
        }

        // Som de transição
        PlaySound(sfxPhaseTransition);

        // Efeito visual de fase (piscar, brilhar, etc.)
        yield return StartCoroutine(PhaseTransitionEffect());

        // Ajusta parâmetros baseados na fase
        ApplyPhaseParameters();

        // Volta para voo
        ChangeState(BossState.Flying);

        if (debugLogs)
            Debug.Log($"[BossFase2] Transição para Fase {currentPhase} completa");
    }

    private IEnumerator PhaseTransitionEffect()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color flashColor = currentPhase == 2 ? Color.red : Color.magenta;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (spriteRenderer != null)
            {
                // Piscar
                float flash = Mathf.Sin(t * 20f) * 0.5f + 0.5f;
                spriteRenderer.color = Color.Lerp(originalColor, flashColor, flash);
            }

            yield return null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private void ApplyPhaseParameters()
    {
        switch (currentPhase)
        {
            case 2:
                // Fase 2: Mais rápido, ataques mais frequentes
                flySpeed *= 1.3f;
                groundFireCooldown *= 0.7f;
                projectileCooldown *= 0.7f;
                fireSpawnCount = Mathf.Min(fireSpawnCount + 1, groundFireSpawnPoints.Length);
                projectilesPerAttack = Mathf.Min(projectilesPerAttack + 1, 4);
                break;

            case 3:
                // Fase 3: Muito agressivo
                flySpeed *= 1.5f;
                groundFireCooldown *= 0.5f;
                projectileCooldown *= 0.5f;
                fireSpawnCount = Mathf.Min(fireSpawnCount + 2, groundFireSpawnPoints.Length);
                projectilesPerAttack = Mathf.Min(projectilesPerAttack + 2, 5);
                fireWarningTime *= 0.7f;
                projectileWarningTime *= 0.7f;
                break;
        }

        if (debugLogs)
            Debug.Log($"[BossFase2] Parâmetros da Fase {currentPhase} aplicados");
    }

    // =====================================
    // MÁQUINA DE ESTADOS
    // =====================================

    private void ChangeState(BossState newState)
    {
        if (currentState == newState) return;

        BossState previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        if (debugLogs)
            Debug.Log($"[BossFase2] Estado: {previousState} → {newState}");

        // Lógica de entrada no estado
        OnStateEnter(newState);
    }

    private void OnStateEnter(BossState state)
    {
        switch (state)
        {
            case BossState.Idle:
                // Escolhe próximo ataque
                DecideNextAttack();
                // Pequeno delay antes de atacar
                attackDecisionTimer = Random.Range(1f, 2f);
                break;

            case BossState.Flying:
                // Inicia movimento de voo
                PickNextFlightTarget();
                if (sfxFlyLoop != null && audioSource != null)
                {
                    audioSource.clip = sfxFlyLoop;
                    audioSource.loop = true;
                    audioSource.Play();
                }
                break;

            case BossState.GroundFireAttack:
                StartCoroutine(GroundFireAttackSequence());
                break;

            case BossState.ProjectileAttack:
                StartCoroutine(ProjectileAttackSequence());
                break;

            case BossState.Death:
                StartCoroutine(DeathSequence());
                break;
        }
    }

    private void StateMachineUpdate()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case BossState.Idle:
                // Transição para voo ou ataque
                if (attackDecisionTimer <= 0f)
                {
                    ExecuteNextAttack();
                }
                break;

            case BossState.Flying:
                // Voa continuamente, verifica se deve atacar
                if (CanAttack())
                {
                    ChangeState(BossState.Idle); // Volta para idle para decidir ataque
                }
                break;

            case BossState.GroundFireAttack:
            case BossState.ProjectileAttack:
                // Aguarda corrotinas de ataque terminarem
                break;
        }
    }

    private void UpdateTimers()
    {
        if (groundFireCooldownTimer > 0f) groundFireCooldownTimer -= Time.deltaTime;
        if (projectileCooldownTimer > 0f) projectileCooldownTimer -= Time.deltaTime;
        if (attackDecisionTimer > 0f) attackDecisionTimer -= Time.deltaTime;
        if (isInvulnerable && Time.time > lastDamageTime + invulnerabilityTime)
        {
            isInvulnerable = false;
        }
    }

    private bool CanAttack()
    {
        bool canGroundFire = groundFireCooldownTimer <= 0f;
        bool canProjectile = projectileCooldownTimer <= 0f;
        return canGroundFire || canProjectile;
    }

    private void DecideNextAttack()
    {
        List<AttackPattern> availableAttacks = new List<AttackPattern>();

        if (groundFireCooldownTimer <= 0f)
            availableAttacks.Add(AttackPattern.GroundFire);

        if (projectileCooldownTimer <= 0f)
            availableAttacks.Add(AttackPattern.Projectile);

        // Fase 3: chance de combo
        if (currentPhase == 3 && Random.value < 0.3f && availableAttacks.Count == 2)
        {
            nextAttack = AttackPattern.Combo;
        }
        else if (availableAttacks.Count > 0)
        {
            nextAttack = availableAttacks[Random.Range(0, availableAttacks.Count)];
        }
        else
        {
            nextAttack = AttackPattern.None;
        }
    }

    private void ExecuteNextAttack()
    {
        switch (nextAttack)
        {
            case AttackPattern.GroundFire:
                ChangeState(BossState.GroundFireAttack);
                groundFireCooldownTimer = groundFireCooldown;
                break;

            case AttackPattern.Projectile:
                ChangeState(BossState.ProjectileAttack);
                projectileCooldownTimer = projectileCooldown;
                break;

            case AttackPattern.Combo:
                // Executa os dois em sequência rápida
                StartCoroutine(ComboAttackSequence());
                groundFireCooldownTimer = groundFireCooldown;
                projectileCooldownTimer = projectileCooldown;
                break;

            default:
                // Sem ataques disponíveis, continua voando
                ChangeState(BossState.Flying);
                break;
        }
    }

    // =====================================
    // MOVIMENTO DE VOO
    // =====================================

    private void HandleFlightMovement()
    {
        if (flightPatrolPoints.Length == 0) return;

        Vector2 currentPos = transform.position;
        Vector2 direction = (targetFlyPosition - currentPos).normalized;
        float distance = Vector2.Distance(currentPos, targetFlyPosition);

        // Oscilação vertical (hover) para sensação de voo
        hoverTimer += Time.fixedDeltaTime * hoverFrequency;
        float hoverOffset = Mathf.Sin(hoverTimer) * hoverAmplitude;

        // Target com hover
        Vector2 hoverTarget = targetFlyPosition + Vector2.up * hoverOffset;

        // Suavização de virada
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float currentAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        float smoothedAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref turnVelocity, turnSmoothTime);

        Vector2 smoothedDirection = new Vector2(Mathf.Cos(smoothedAngle * Mathf.Deg2Rad), Mathf.Sin(smoothedAngle * Mathf.Deg2Rad));

        // Aplica velocidade
        Vector2 targetVelocity = smoothedDirection * flySpeed;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, flyAcceleration * Time.fixedDeltaTime);

        // Flip visual baseado na direção horizontal
        UpdateVisualDirection(rb.linearVelocity.x);

        // Animação de bater asas
        if (enableWingFlapAnimation && animator != null)
        {
            animator.SetBool(Anim_WingFlap, true);
            animator.SetFloat(Anim_Speed, rb.linearVelocity.magnitude / flySpeed);
        }

        // Verifica se chegou ao target
        if (distance < 0.5f)
        {
            PickNextFlightTarget();
        }

        // Mantém dentro dos limites de altura
        ClampFlightHeight();
    }

    private void PickNextFlightTarget()
    {
        if (flightPatrolPoints.Length == 0) return;

        // Escolhe ponto aleatório (não o atual)
        int nextIndex;
        do
        {
            nextIndex = Random.Range(0, flightPatrolPoints.Length);
        } while (nextIndex == currentPatrolIndex && flightPatrolPoints.Length > 1);

        currentPatrolIndex = nextIndex;
        targetFlyPosition = flightPatrolPoints[currentPatrolIndex].position;

        // Adiciona variação aleatória na altura
        targetFlyPosition.y += Random.Range(-1f, 1f);
        targetFlyPosition.y = Mathf.Clamp(targetFlyPosition.y, minFlyHeight, maxFlyHeight);
    }

    private void ClampFlightHeight()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minFlyHeight, maxFlyHeight);
        transform.position = pos;
    }

    private void UpdateVisualDirection(float velocityX)
    {
        if (spriteRenderer == null) return;

        if (Mathf.Abs(velocityX) > 0.1f)
        {
            spriteRenderer.flipX = velocityX < 0f;
        }
    }

    // =====================================
    // ATAQUE: FOGO NO CHÃO
    // =====================================

    private IEnumerator GroundFireAttackSequence()
    {
        if (debugLogs)
            Debug.Log("[BossFase2] Iniciando ataque: Fogo no Chão");

        // Animação de preparação
        if (animator != null)
        {
            animator.SetTrigger(Anim_GroundFire);
        }

        // Som de aviso
        PlaySound(sfxGroundFireWarning);

        // Escolhe pontos de spawn aleatórios
        List<Transform> selectedPoints = SelectRandomFirePoints(fireSpawnCount);

        // Cria avisos visuais
        yield return StartCoroutine(ShowFireWarnings(selectedPoints));

        // Tempo de aviso
        yield return new WaitForSeconds(fireWarningTime);

        // Spawn do fogo
        yield return StartCoroutine(SpawnGroundFires(selectedPoints));

        // Som do fogo aparecendo
        PlaySound(sfxGroundFireSpawn);

        // Duração do fogo
        yield return new WaitForSeconds(fireDuration);

        // Fade out do fogo
        yield return StartCoroutine(FadeOutFires());

        // Volta para voo
        ChangeState(BossState.Flying);
    }

    private List<Transform> SelectRandomFirePoints(int count)
    {
        List<Transform> available = new List<Transform>(groundFireSpawnPoints);
        List<Transform> selected = new List<Transform>();

        count = Mathf.Min(count, available.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, available.Count);
            selected.Add(available[idx]);
            available.RemoveAt(idx);
        }

        return selected;
    }

    private IEnumerator ShowFireWarnings(List<Transform> points)
    {
        foreach (Transform point in points)
        {
            GameObject warning = null;

            if (fireWarningPrefab != null)
            {
                warning = Instantiate(fireWarningPrefab, point.position, Quaternion.identity);
            }
            else
            {
                // Cria aviso procedural (círculo piscando no chão)
                warning = CreateProceduralFireWarning(point.position);
            }

            if (warning != null)
            {
                activeFireWarnings.Add(warning);
            }
        }

        // Anima os avisos (piscar)
        float elapsed = 0f;
        while (elapsed < fireWarningTime)
        {
            elapsed += Time.deltaTime;
            float alpha = (Mathf.Sin(elapsed * 10f) + 1f) * 0.5f * 0.8f + 0.2f;

            foreach (GameObject w in activeFireWarnings)
            {
                if (w != null)
                {
                    var sr = w.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }
                }
            }

            yield return null;
        }
    }

    private GameObject CreateProceduralFireWarning(Vector3 position)
    {
        GameObject warning = new GameObject("FireWarning");
        warning.transform.position = position;

        var sr = warning.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(1f);
        sr.color = fireWarningColor;
        sr.sortingOrder = 10;

        // Scale pulsa
        warning.transform.localScale = Vector3.one * 1.5f;

        return warning;
    }

    private Sprite CreateCircleSprite(float radius)
    {
        // Cria um sprite circular simples procedural
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - dist / maxDist);
                pixels[y * size + x] = new Color(1f, 0.5f, 0f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private IEnumerator SpawnGroundFires(List<Transform> points)
    {
        foreach (Transform point in points)
        {
            if (groundFirePrefab != null)
            {
                GameObject fire = Instantiate(groundFirePrefab, point.position, Quaternion.identity);
                activeFires.Add(fire);

                // Configura dano do fogo
                var fireDamageComp = fire.GetComponent<GroundFireDamage>();
                if (fireDamageComp == null)
                {
                    fireDamageComp = fire.AddComponent<GroundFireDamage>();
                }
                fireDamageComp.Initialize(fireDamage, playerLayer, fireFadeTime);
            }
            else
            {
                // Cria fogo procedural
                GameObject fire = CreateProceduralFire(point.position);
                activeFires.Add(fire);
            }
        }

        // Limpa avisos
        foreach (GameObject w in activeFireWarnings)
        {
            if (w != null) Destroy(w);
        }
        activeFireWarnings.Clear();

        yield return null;
    }

    private GameObject CreateProceduralFire(Vector3 position)
    {
        GameObject fire = new GameObject("GroundFire");
        fire.transform.position = position;
        fire.transform.localScale = new Vector3(1.5f, 2f, 1f);

        var sr = fire.AddComponent<SpriteRenderer>();
        sr.sprite = CreateFireSprite();
        sr.color = new Color(1f, 0.4f, 0f, 0.9f);
        sr.sortingOrder = 5;

        // Collider para dano
        var col = fire.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1.5f);

        // Script de dano
        var damage = fire.AddComponent<GroundFireDamage>();
        damage.Initialize(fireDamage, playerLayer, fireFadeTime);

        // Animação de chamas (scale Y pulsa)
        fire.AddComponent<FireAnimation>();

        return fire;
    }

    private Sprite CreateFireSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 4f); // Base no meio-baixo

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distX = Mathf.Abs(pos.x - center.x) / (size * 0.3f);
                float distY = (center.y - pos.y) / (size * 0.4f); // Forma de chama (mais alto que largo)

                if (distY > 0 && distX < distY * 0.8f)
                {
                    float intensity = 1f - distY;
                    float flicker = Mathf.PerlinNoise(pos.x * 0.1f, pos.y * 0.1f + Time.time * 5f) * 0.3f;
                    pixels[y * size + x] = new Color(1f, 0.3f + intensity * 0.5f, 0f, intensity + flicker);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.2f), 100f);
    }

    private IEnumerator FadeOutFires()
    {
        float elapsed = 0f;
        List<SpriteRenderer> fireRenderers = new List<SpriteRenderer>();

        foreach (GameObject fire in activeFires)
        {
            if (fire != null)
            {
                var sr = fire.GetComponent<SpriteRenderer>();
                if (sr != null) fireRenderers.Add(sr);

                // Desativa collider
                var col = fire.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }
        }

        while (elapsed < fireFadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fireFadeTime;
            float alpha = Mathf.Lerp(1f, 0f, t);

            foreach (var sr in fireRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }

            // Faz o fogo "descer"
            foreach (GameObject fire in activeFires)
            {
                if (fire != null)
                {
                    fire.transform.position += Vector3.down * Time.deltaTime * 2f;
                }
            }

            yield return null;
        }

        // Destrói fogos
        foreach (GameObject fire in activeFires)
        {
            if (fire != null) Destroy(fire);
        }
        activeFires.Clear();
    }

    // =====================================
    // ATAQUE: PROJÉTIL
    // =====================================

    private IEnumerator ProjectileAttackSequence()
    {
        if (debugLogs)
            Debug.Log("[BossFase2] Iniciando ataque: Projétil");

        // Animação de preparação
        if (animator != null)
        {
            animator.SetTrigger(Anim_ProjectileAttack);
        }

        for (int i = 0; i < projectilesPerAttack; i++)
        {
            // Escolhe canto aleatório
            Transform corner = arenaCorners[Random.Range(0, arenaCorners.Length)];

            // Mostra aviso no canto
            yield return StartCoroutine(ShowProjectileWarning(corner));

            // Dispara projétil
            yield return StartCoroutine(FireProjectile(corner));

            // Intervalo entre projéteis
            if (i < projectilesPerAttack - 1)
                yield return new WaitForSeconds(projectileInterval);
        }

        // Volta para voo
        ChangeState(BossState.Flying);
    }

    private IEnumerator ShowProjectileWarning(Transform corner)
    {
        GameObject warning = null;

        if (projectileWarningPrefab != null)
        {
            warning = Instantiate(projectileWarningPrefab, corner.position, Quaternion.identity);
        }
        else
        {
            // Cria aviso procedural (mira/laser)
            warning = CreateProceduralProjectileWarning(corner.position);
        }

        if (warning != null)
        {
            activeProjectileWarnings.Add(warning);
        }

        // Som de aviso
        PlaySound(sfxProjectileWarning);

        // Tempo de aviso
        yield return new WaitForSeconds(projectileWarningTime);

        // Remove aviso
        if (warning != null)
        {
            Destroy(warning);
            activeProjectileWarnings.Remove(warning);
        }
    }

    private GameObject CreateProceduralProjectileWarning(Vector3 position)
    {
        GameObject warning = new GameObject("ProjectileWarning");
        warning.transform.position = position;

        // Linha indicando direção (aponta para o centro/player)
        Vector3 targetPos = playerTransform != null ? playerTransform.position : centerPoint.position;
        Vector3 direction = (targetPos - position).normalized;
        float distance = Vector3.Distance(position, targetPos);

        var lr = warning.AddComponent<LineRenderer>();
        lr.startWidth = 0.1f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.yellow;
        lr.endColor = new Color(1f, 1f, 0f, 0f);
        lr.positionCount = 2;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, direction * distance);
        lr.sortingOrder = 10;

        // Pisca
        warning.AddComponent<WarningBlink>();

        return warning;
    }

    private IEnumerator FireProjectile(Transform origin)
    {
        // Calcula direção para o jogador (ou centro se não tiver player)
        Vector3 targetPos = playerTransform != null ? playerTransform.position : centerPoint.position;
        Vector2 direction = (targetPos - origin.position).normalized;

        // Som de disparo
        PlaySound(sfxProjectileShoot);

        if (projectilePrefab != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, origin.position, Quaternion.identity);

            // Configura projétil
            var projScript = projectile.GetComponent<BossProjectile>();
            if (projScript == null)
            {
                projScript = projectile.AddComponent<BossProjectile>();
            }
            projScript.Initialize(direction, projectileSpeed, projectileDamage, projectileLifetime, playerLayer);
        }
        else
        {
            // Cria projétil procedural
            CreateProceduralProjectile(origin.position, direction);
        }

        yield return null;
    }

    private void CreateProceduralProjectile(Vector3 position, Vector2 direction)
    {
        GameObject proj = new GameObject("BossProjectile");
        proj.transform.position = position;
        proj.transform.right = direction;

        var sr = proj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateProjectileSprite();
        sr.color = Color.red;
        sr.sortingOrder = 5;

        var rb2d = proj.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.bodyType = RigidbodyType2D.Dynamic;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb2d.linearVelocity = direction * projectileSpeed;

        var col = proj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        var projScript = proj.AddComponent<BossProjectile>();
        projScript.Initialize(direction, projectileSpeed, projectileDamage, projectileLifetime, playerLayer);

        // Auto-destruição
        Destroy(proj, projectileLifetime);
    }

    private Sprite CreateProjectileSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size/2f) / (size/2f);
                float dy = (y - size/2f) / (size/2f);
                float dist = Mathf.Sqrt(dx*dx + dy*dy);

                if (dist < 1f)
                {
                    float alpha = 1f - dist;
                    pixels[y * size + x] = new Color(1f, 0.2f, 0f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // =====================================
    // COMBO ATTACK (Fase 3)
    // =====================================

    private IEnumerator ComboAttackSequence()
    {
        if (debugLogs)
            Debug.Log("[BossFase2] Iniciando COMBO: Fogo + Projétil");

        // Inicia fogo no chão
        StartCoroutine(GroundFireAttackSequence());

        // Pequeno delay
        yield return new WaitForSeconds(1f);

        // Dispara projéteis enquanto o fogo está ativo
        yield return StartCoroutine(ProjectileAttackSequence());
    }

    // =====================================
    // DANO E MORTE
    // =====================================

    public void TakeDamage(int amount, GameObject source)
    {
        if (isDead || isInvulnerable || amount <= 0) return;

        currentHealth -= amount;
        lastDamageTime = Time.time;
        isInvulnerable = true;

        if (debugLogs)
            Debug.Log($"[BossFase2] Tomou {amount} de dano | Vida: {currentHealth}/{maxHealth}");

        // Animação de dano
        if (animator != null)
        {
            animator.SetTrigger(Anim_TakeDamage);
        }

        // Som de dano
        PlaySound(sfxTakeDamage);

        // Knockback visual leve
        StartCoroutine(DamageFlash());

        // Verifica morte
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        Color original = spriteRenderer.color;
        Color flashColor = Color.white;

        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.05f);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void Die()
    {
        if (isDead || !canDie) return;
        isDead = true;

        if (debugLogs)
            Debug.Log("[BossFase2] MORREU!");

        ChangeState(BossState.Death);
    }

    private IEnumerator DeathSequence()
    {
        // Para tudo
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (col != null) col.enabled = false;

        // Para audio de voo
        if (audioSource != null && audioSource.clip == sfxFlyLoop)
        {
            audioSource.Stop();
        }

        // Som de morte
        PlaySound(sfxDeath);

        // Animação de morte
        if (animator != null)
        {
            animator.SetTrigger(Anim_Death);
        }

        // Limpa objetos ativos
        CleanupActiveObjects();

        // Tempo para animação
        yield return new WaitForSeconds(2f);

        // Destrói
        Destroy(gameObject);
    }

    // =====================================
    // COLISÃO COM PLAYER
    // =====================================

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (Time.time < lastContactDamageTime + contactCooldown) return;

        TryContactDamage(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (Time.time < lastContactDamageTime + contactCooldown) return;

        TryContactDamage(collision.gameObject);
    }

    private void TryContactDamage(GameObject playerObj)
    {
        PlayerController pc = playerObj.GetComponentInParent<PlayerController>();
        if (pc == null) pc = playerObj.GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.TakeDamage(contactDamage, gameObject);
            lastContactDamageTime = Time.time;

            if (debugLogs)
                Debug.Log($"[BossFase2] Dano de contato: {contactDamage}");
        }
    }

    // =====================================
    // ANIMAÇÕES
    // =====================================

    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool(Anim_Flying, currentState == BossState.Flying);
        animator.SetFloat(Anim_Speed, rb.linearVelocity.magnitude / flySpeed);
    }

    // =====================================
    // UTILITÁRIOS
    // =====================================

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void CleanupActiveObjects()
    {
        foreach (GameObject obj in activeFireWarnings)
            if (obj != null) Destroy(obj);
        activeFireWarnings.Clear();

        foreach (GameObject obj in activeFires)
            if (obj != null) Destroy(obj);
        activeFires.Clear();

        foreach (GameObject obj in activeProjectileWarnings)
            if (obj != null) Destroy(obj);
        activeProjectileWarnings.Clear();
    }

    private void HandleDebugInput()
    {
        if (!Application.isEditor) return;

        // Teclas de debug
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TakeDamage(10, gameObject);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            currentHealth = phase2HealthThreshold + 1;
            CheckPhaseTransition();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            currentHealth = phase3HealthThreshold + 1;
            CheckPhaseTransition();
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            DecideNextAttack();
            ExecuteNextAttack();
        }
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Pontos de patrulha de voo
        if (flightPatrolPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var point in flightPatrolPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }
        }

        // Cantos da arena
        if (arenaCorners != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var corner in arenaCorners)
            {
                if (corner != null)
                    Gizmos.DrawWireCube(corner.position, Vector3.one);
            }
        }

        // Pontos de fogo
        if (groundFireSpawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (var point in groundFireSpawnPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, 0.3f);
            }
        }

        // Centro
        if (centerPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(centerPoint.position, 1f);
        }

        // Altura de voo
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
        Gizmos.DrawWireCube(center, new Vector3(12f, maxFlyHeight - minFlyHeight, 1f));
    }
}

// =====================================
// COMPONENTES AUXILIARES
// =====================================

/// <summary>
/// Comportamento do fogo no chão - causa dano ao player
/// </summary>
public class GroundFireDamage : MonoBehaviour
{
    private int damage;
    private LayerMask playerLayer;
    private float fadeTime;
    private float timer;
    private bool fading = false;
    private Collider2D fireCol;

    public void Initialize(int dmg, LayerMask layer, float fade)
    {
        damage = dmg;
        playerLayer = layer;
        fadeTime = fade;
        fireCol = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (fading)
        {
            timer += Time.deltaTime;
            if (timer >= fadeTime)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (fading) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        if (!other.CompareTag("Player")) return;

        var pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) pc = other.GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.TakeDamage(damage, gameObject);
        }
    }

    public void StartFade()
    {
        fading = true;
        timer = 0f;
        if (fireCol != null) fireCol.enabled = false;
    }
}

/// <summary>
/// Animação simples de fogo (pulsar scale Y)
/// </summary>
public class FireAnimation : MonoBehaviour
{
    private Vector3 baseScale;
    private float speed = 3f;
    private float amplitude = 0.2f;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float scaleY = baseScale.y + Mathf.Sin(Time.time * speed) * amplitude;
        transform.localScale = new Vector3(baseScale.x, scaleY, baseScale.z);
    }
}

/// <summary>
/// Pisca o aviso de projétil
/// </summary>
public class WarningBlink : MonoBehaviour
{
    private LineRenderer lr;
    private float speed = 8f;

    private void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (lr != null)
        {
            float alpha = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            Color start = lr.startColor;
            Color end = lr.endColor;
            start.a = alpha;
            end.a = alpha * 0.5f;
            lr.startColor = start;
            lr.endColor = end;
        }
    }
}

/// <summary>
/// Projétil do boss
/// </summary>
public class BossProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private float lifetime;
    private LayerMask playerLayer;
    private float timer;
    private Rigidbody2D rb;

    public void Initialize(Vector2 dir, float spd, int dmg, float life, LayerMask layer)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        lifetime = life;
        playerLayer = layer;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Rotaciona para direção do movimento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        if (!other.CompareTag("Player")) return;

        var pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) pc = other.GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.TakeDamage(damage, gameObject);
            Destroy(gameObject);
        }
    }
}

// =====================================
// INTERFACE IDamageable (caso não exista)
// =====================================

public interface IDamageable
{
    void TakeDamage(int amount, GameObject source);
}