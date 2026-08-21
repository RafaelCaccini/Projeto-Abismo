using System.Collections;
using UnityEngine;

// Mantemos o nome da classe para não quebrar referências do Unity
public class EnemyDashAttack : MonoBehaviour, IDamageable
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("REFERÊNCIAS")]
    [Tooltip("Transform do Player. Se nulo, será buscado por Tag 'Player'.")]
    [SerializeField] private Transform player;

    [Tooltip("Transform que contém o Sprite/Visual. Deve ser filho do inimigo.")]
    [SerializeField] private Transform visual;

    [SerializeField] private Animator animator;

    // =====================================
    // DETECÇÃO
    // =====================================

    [Header("DETECÇÃO")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private LayerMask playerLayer = 1 << 8; // exemplo: default Player layer

    // =====================================
    // MOVIMENTO / PERSEGUIÇÃO
    // =====================================

    [Header("MOVIMENTO")]
    [SerializeField] private float pursuitSpeed = 2f;

    // =====================================
    // DASH
    // =====================================

    [Header("DASH")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCooldown = 1.5f;
    [SerializeField] private int dashDamage = 15;

    [Header("Hitbox Dash")]
    [SerializeField] private Vector2 dashHitboxSize = new Vector2(1.2f, 0.8f);
    [SerializeField] private Vector2 dashHitboxOffset = new Vector2(0.8f, 0f);

    // =====================================
    // CORPO A CORPO
    // =====================================

    [Header("CORPO A CORPO")]
    [SerializeField] private Vector2 meleeHitboxSize = new Vector2(1.2f, 1.0f);
    [SerializeField] private Vector2 meleeHitboxOffset = new Vector2(0.9f, 0f);
    [SerializeField] private int meleeDamage = 5;
    [SerializeField] private float meleeCooldown = 1f;

    // =====================================
    // VIDA / MORTE
    // =====================================

    [Header("VIDA")]
    [SerializeField] private int maxLife = 3;
    [SerializeField] private float deathDelay = 0.1f;

    // =====================================
    // GROUND CHECK
    // =====================================

    [Header("GROUND CHECK")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    // =====================================
    // FÍSICA
    // =====================================

    [Header("FÍSICA")]
    [SerializeField] private float gravityScale = 1f;

    // =====================================
    // DEBUG / GIZMOS
    // =====================================

    [Header("DEBUG")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool showGizmos = true;

    [Header("Gizmos Cores")]
    [SerializeField] private Color gizmoDetection = Color.yellow;
    [SerializeField] private Color gizmoBody = Color.blue;
    [SerializeField] private Color gizmoMelee = Color.red;
    [SerializeField] private Color gizmoDash = new Color(0.5f, 0f, 0.5f);

    // =====================================
    // Internals
    // =====================================

    private Rigidbody2D rb;
    private int currentLife;

    private bool isDead = false;

    // Dash state
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.zero;
    private bool dashAlreadyHit = false;

    // Melee state
    private float meleeTimer = 0f;

    // CONTACT DAMAGE (adicionado)
    private float lastContactDamageTime = 0f; // controla cooldown de dano por contato

    // Hitbox children
    private GameObject hitboxMeleeObj;
    private GameObject hitboxDashObj;
    private BoxCollider2D hitboxMeleeCollider;
    private BoxCollider2D hitboxDashCollider;

    // Reference to main physical collider (if exists)
    private Collider2D mainCollider;

    // Cached player collider (optional)
    private Collider2D playerColliderCache;

    // =====================================
    // Unity events
    // =====================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError("[EnemyDashAttack] Rigidbody2D ausente no GameObject.");
            enabled = false;
            return;
        }

        // physics defaults for flying ground enemy
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        currentLife = maxLife;

        EnsurePlayerReference();

        // Prepare hitbox children (tries to reuse existing children)
        CreateOrFindHitboxes();
    }

    private void Start()
    {
        // Ensure dash collider initially disabled
        if (hitboxDashCollider != null)
            hitboxDashCollider.enabled = false;

        // Melee collider is used only for visualization; detection will use OverlapBox.
        if (hitboxMeleeCollider != null)
            hitboxMeleeCollider.enabled = false;
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (player == null)
            EnsurePlayerReference();

        // Timers
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.fixedDeltaTime;

        if (dashTimer > 0f)
            dashTimer -= Time.fixedDeltaTime;

        if (meleeTimer > 0f)
            meleeTimer -= Time.fixedDeltaTime;

        // Detection: use a circle to decide behavior (not the hitboxes)
        bool playerDetected = PlayerIsWithinDetection();

        // If dash active, move accordingly
        if (isDashing)
        {
            PerformDashStep();
            return; // during dash we don't do pursuit/melee checks
        }

        // If player detected, attempt dash if available, otherwise pursue and attempt melee
        if (playerDetected)
        {
            float distSq = ((Vector2)player.position - (Vector2)rb.position).sqrMagnitude;

            // Start dash if cooldown ready
            if (dashCooldownTimer <= 0f)
            {
                StartDash();
                return;
            }

            // Attempt melee using precise overlap box (not simple distance)
            if (TryMelee())
            {
                return; // melee performed this frame
            }

            // Pursue otherwise
            PursuePlayer();
        }
    }

    private void OnDestroy()
    {
        // Ensure child hitboxes removed if created at runtime to avoid leaks in editor play mode
        // (only remove if we created them in this instance)
    }

    // =====================================
    // Movement / Pursuit
    // =====================================

    private void PursuePlayer()
    {
        if (player == null) return;
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        // preserva componente Y (gravidade)
        rb.linearVelocity = new Vector2(dir.x * pursuitSpeed, rb.linearVelocity.y);
        UpdateVisualFacing(dir.x);
    }

    private void UpdateVisualFacing(float dx)
    {
        if (visual == null) return;
        Vector3 s = visual.localScale;
        s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
        visual.localScale = s;
    }

    // =====================================
    // DASH
    // =====================================

    private void StartDash()
    {
        if (player == null) return;
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashAlreadyHit = false;
        dashDirection = ((Vector2)player.position - rb.position).normalized;
        if (hitboxDashCollider != null) hitboxDashCollider.enabled = true;
        // aplica velocidade de dash (mantém Y atual)
        rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, rb.linearVelocity.y);
    }

    private void PerformDashStep()
    {
        // durante dash mantemos velocity (pode atualizar caso queira homing leve)
        rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, rb.linearVelocity.y);
        UpdateVisualFacing(dashDirection.x);
        if (dashTimer <= 0f) EndDash();
    }

    private void EndDash()
    {
        isDashing = false;
        dashTimer = 0f;
        if (hitboxDashCollider != null) hitboxDashCollider.enabled = false;
        dashAlreadyHit = false;
        // zera só componente X (preserva Y para cair)
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // Called from HitboxRelay when dash hitbox triggers
    internal void OnDashHit(Collider2D other)
    {
        if (isDead || !isDashing || dashAlreadyHit) return;

        if (!other.CompareTag("Player")) return;

        var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        pc.TakeDamage(dashDamage, gameObject);
        dashAlreadyHit = true;

        // disable dash hitbox immediately to prevent further hits this dash
        if (hitboxDashCollider != null)
            hitboxDashCollider.enabled = false;

        if (debugLogs)
            Debug.Log("[EnemyDashAttack] Dash hit player: " + dashDamage);
    }

    // =====================================
    // Melee using OverlapBox
    // =====================================

    private bool TryMelee()
    {
        if (meleeTimer > 0f) return false;
        if (player == null) return false;

        Vector2 boxCenter = rb.position + RotateOffsetByFacing(meleeHitboxOffset);

        // Debug: visualizar onde está checando
        if (debugLogs)
            Debug.Log($"[EnemyDashAttack] Melee OverlapBox em: {boxCenter}, tamanho: {meleeHitboxSize}");

        // check player via OverlapBox usando LayerMask
        Collider2D hit = Physics2D.OverlapBox(boxCenter, meleeHitboxSize, 0f, playerLayer);
        
        // Se não encontrou na layer, tenta buscar por tag
        if (hit == null)
        {
            Collider2D[] allHits = Physics2D.OverlapBoxAll(boxCenter, meleeHitboxSize, 0f);
            foreach (Collider2D c in allHits)
            {
                if (c != null && c.CompareTag("Player"))
                {
                    hit = c;
                    if (debugLogs)
                        Debug.Log($"[EnemyDashAttack] Hit encontrado por tag: {c.name}");
                    break;
                }
            }
        }
        else
        {
            if (debugLogs)
                Debug.Log($"[EnemyDashAttack] Hit encontrado por layer: {hit.name}");
        }

        if (hit != null)
        {
            var pc = hit.GetComponent<PlayerController>() ?? hit.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(meleeDamage, gameObject);
                meleeTimer = meleeCooldown;

                if (debugLogs)
                    Debug.Log("[EnemyDashAttack] Melee hit player: " + meleeDamage);

                return true;
            }
        }

        return false;
    }

    private Vector2 RotateOffsetByFacing(Vector2 offset)
    {
        // visual scale x sign controls facing
        float sign = 1f;
        if (visual != null)
            sign = visual.localScale.x >= 0f ? 1f : -1f;
        else
            sign = 1f;

        return new Vector2(offset.x * sign, offset.y);
    }

    // =====================================
    // Hitbox creation / helpers
    // =====================================

    private void CreateOrFindHitboxes()
    {
        // Melee hitbox (visual aid)
        Transform tMelee = transform.Find("Hitbox_Melee");
        if (tMelee != null)
        {
            hitboxMeleeObj = tMelee.gameObject;
            hitboxMeleeCollider = hitboxMeleeObj.GetComponent<BoxCollider2D>();
        }
        else
        {
            hitboxMeleeObj = new GameObject("Hitbox_Melee");
            hitboxMeleeObj.transform.SetParent(transform, false);
            hitboxMeleeObj.transform.localPosition = meleeHitboxOffset;
            hitboxMeleeCollider = hitboxMeleeObj.AddComponent<BoxCollider2D>();
            hitboxMeleeCollider.isTrigger = true; // visual only; we don't rely on its triggers for damage
        }

        // Dash hitbox (trigger used for damage)
        Transform tDash = transform.Find("Hitbox_Dash");
        if (tDash != null)
        {
            hitboxDashObj = tDash.gameObject;
            hitboxDashCollider = hitboxDashObj.GetComponent<BoxCollider2D>();
        }
        else
        {
            hitboxDashObj = new GameObject("Hitbox_Dash");
            hitboxDashObj.transform.SetParent(transform, false);
            hitboxDashObj.transform.localPosition = dashHitboxOffset;
            hitboxDashCollider = hitboxDashObj.AddComponent<BoxCollider2D>();
            hitboxDashCollider.isTrigger = true;

            // Add relay component to forward trigger events to this enemy
            var relay = hitboxDashObj.AddComponent<HitboxRelay>();
            relay.Initialize(this, HitboxRelay.HitboxType.Dash);
        }

        // ensure sizes and offsets are applied
        if (hitboxMeleeCollider != null)
        {
            hitboxMeleeCollider.size = meleeHitboxSize;
            hitboxMeleeObj.transform.localPosition = meleeHitboxOffset;
        }

        if (hitboxDashCollider != null)
        {
            hitboxDashCollider.size = dashHitboxSize;
            hitboxDashObj.transform.localPosition = dashHitboxOffset;
        }
    }

    // =====================================
    // Detection helpers
    // =====================================

    private bool PlayerIsWithinDetection()
    {
        if (player == null) return false;
        float d2 = ((Vector2)player.position - rb.position).sqrMagnitude;
        return d2 <= detectionRadius * detectionRadius;
    }

    private void EnsurePlayerReference()
    {
        if (player != null) return;

        var go = GameObject.FindWithTag("Player");
        if (go != null) player = go.transform;
    }

    // =====================================
    // IDamageable
    // =====================================

    public void TakeDamage(int amount, GameObject source)
    {
        if (isDead) return;

        currentLife -= amount;
        if (debugLogs)
            Debug.Log($"[EnemyDashAttack] Took {amount} dmg, life={currentLife}");

        if (currentLife <= 0)
            StartCoroutine(DieRoutine());
        else
        {
            if (animator != null)
                animator.SetTrigger("Hit");
        }
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;

        // stop movement
        rb.linearVelocity = Vector2.zero;

        // disable all colliders (main + children)
        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        // animator
        if (animator != null)
            animator.SetBool("IsDead", true);

        if (debugLogs)
            Debug.Log("[EnemyDashAttack] Dying");

        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }

    // =====================================
    // HitboxRelay callback for dash
    // =====================================

    // Helper component to attach to hitbox child to forward events to parent enemy
    private class HitboxRelay : MonoBehaviour
    {
        public enum HitboxType { Dash }

        private EnemyDashAttack owner;
        private HitboxType type;

        public void Initialize(EnemyDashAttack owner, HitboxType t)
        {
            this.owner = owner;
            this.type = t;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (owner == null) return;

            if (type == HitboxType.Dash)
            {
                owner.OnDashHit(other);
            }
        }
    }

    // =====================================
    // Gizmos
    // =====================================

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Detection circle
        Gizmos.color = gizmoDetection;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Main collider (approximate)
        if (mainCollider != null)
        {
            Gizmos.color = gizmoBody;
            Bounds b = mainCollider.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        // Melee box (uses offset rotated by facing)
        Gizmos.color = gizmoMelee;
        Vector2 meleeCenter = (Application.isPlaying && rb != null) ? (Vector2)rb.position + RotateOffsetByFacing(meleeHitboxOffset) : (Vector2)transform.position + meleeHitboxOffset;
        Gizmos.DrawWireCube(meleeCenter, meleeHitboxSize);

        // Dash box
        Gizmos.color = gizmoDash;
        Vector2 dashCenter = (Application.isPlaying && rb != null) ? (Vector2)rb.position + RotateOffsetByFacing(dashHitboxOffset) : (Vector2)transform.position + dashHitboxOffset;
        Gizmos.DrawWireCube(dashCenter, dashHitboxSize);

        // dash direction arrow
        if (Application.isPlaying && isDashing)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine((Vector2)rb.position, (Vector2)rb.position + dashDirection);
        }
    }

    // Substitua o método IsGrounded atual por este (usa tag "Ground" em vez de LayerMask)
    private bool IsGrounded()
    {
        if (groundCheck == null) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, 0.1f);
        foreach (var c in hits)
        {
            if (c != null && c.CompareTag("Ground"))
                return true;
        }
        return false;
    }

    // =====================================
    // DAMAGE ON CONTACT (adicionado)
    // =====================================
    private void OnCollisionStay2D(Collision2D other)
    {
        if (isDead) return;

        // durante dash o hitbox de dash já cuida do dano
        if (isDashing) return;

        if (!other.gameObject.CompareTag("Player")) return;

        // usa cooldown separado para evitar spam de dano por contato
        if (Time.time < lastContactDamageTime + meleeCooldown) return;

        // tenta obter PlayerController no próprio collider ou em um ancestor
        var pc = other.gameObject.GetComponent<PlayerController>() ?? other.gameObject.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        pc.TakeDamage(meleeDamage, gameObject);
        lastContactDamageTime = Time.time;

        if (debugLogs)
            Debug.Log("[EnemyDashAttack] Contact melee hit player: " + meleeDamage);
    }

    // fallback para triggers (caso o jogador tenha trigger collider)
    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        if (isDashing) return;
        if (!other.CompareTag("Player")) return;
        if (Time.time < lastContactDamageTime + meleeCooldown) return;

        var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        pc.TakeDamage(meleeDamage, gameObject);
        lastContactDamageTime = Time.time;

        if (debugLogs)
            Debug.Log("[EnemyDashAttack] Contact melee hit (trigger) player: " + meleeDamage);
    }
}
