using System.Collections;
using UnityEngine;

public class Walker : MonoBehaviour, IDamageable
{
    // =====================================
    // MOVEMENT
    // =====================================

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;

    [SerializeField] private float runSpeed = 6f;

    // =====================================
    // STATS
    // =====================================

    [Header("Stats")]
    [SerializeField] private int life = 3;

    [SerializeField] private int damage = 1;

    // =====================================
    // COMBAT
    // =====================================

    [Header("Combat")]
    [SerializeField] private float damageCooldown = 1f;

    // =====================================
    // SOUNDS
    // =====================================

    [Header("Sounds")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip somAndar;

    [Tooltip("Distância em que o som toca no volume máximo")]
    [SerializeField] private float raioVolumeMaximo = 3f;

    [Tooltip("Distância a partir da qual o som não é mais ouvido")]
    [SerializeField] private float raioAudicao = 8f;

    private Transform playerTransform;
    // =====================================
    // LIGHT DETECTION
    // =====================================

    [Header("Light Detection")]
    [SerializeField] private Lampiao lampScript;

    [SerializeField] private float lightRadius = 5f;

    // =====================================
    // WALL DETECTION
    // =====================================

    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance = 0.5f;

    [SerializeField] private LayerMask wallLayer;

    // =====================================
    // PLAYER INTERACTION (HEAD SLIDE)
    // =====================================

    [Header("Player Interaction")]
    [Tooltip("Velocidade horizontal imediata aplicada ao player ao tocar o topo do Walker (substitui componente X da velocidade)")]
    [SerializeField] private float headSlideSpeed = 6f;

    [Tooltip("Impulso horizontal adicional (ForceMode2D.Impulse) aplicado ao player ao tocar o topo")]
    [SerializeField] private float headSlideForce = 4f;

    // =====================================
    // REFERENCES
    // =====================================

    [Header("References")]
    [SerializeField] private Animator animator;

    // =====================================
    // COMPONENTS
    // =====================================

    private Rigidbody2D rb;

    private SpriteRenderer sr;

    private Collider2D col;

    // =====================================
    // MOVEMENT CONTROL
    // =====================================

    private int patrolDirection = 1;

    private int fleeDirection = 0;

    private bool isFleeing = false;

    // =====================================
    // FLEE
    // =====================================

    private float fleeTimer = 0f;

    private const float fleeDuration = 7f;

    // =====================================
    // DAMAGE
    // =====================================

    private float lastDamageTime;
    private bool morreu = false;

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        if (lampScript == null)
        {
            lampScript = FindFirstObjectByType<Lampiao>();
        }

        // configura o audio source pra ser controlado manualmente (ignora Spatial Blend 3D)
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f; // volume controlado por script, não pelo Unity
            audioSource.loop = true;
            audioSource.clip = somAndar;
            audioSource.volume = 0f;
            audioSource.Play();
        }

        // acha o player pra calcular distância manualmente
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }

    private void UpdateSomAndar()
    {
        if (audioSource == null || playerTransform == null)
            return;

        // distância 2D real, ignorando o eixo Z (câmera/parallax não interfere)
        float dist = Vector2.Distance(transform.position, playerTransform.position);

        float volume;

        if (dist <= raioVolumeMaximo)
        {
            volume = 1f;
        }
        else if (dist >= raioAudicao)
        {
            volume = 0f;
        }
        else
        {
            // interpola suavemente entre os dois raios
            float t = (dist - raioVolumeMaximo) / (raioAudicao - raioVolumeMaximo);
            volume = Mathf.Lerp(1f, 0f, t);
        }

        audioSource.volume = volume;
    }

    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        if (morreu)
            return;

        HandleFleeTimer();
        DetectLamp();
        UpdateAnimations();
        UpdateSomAndar();
    }

    // =====================================
    // FIXED UPDATE
    // =====================================

    private void FixedUpdate()
    {
        if (morreu)
            return;

        Move();
        UpdateFlip();
    }

    // =====================================
    // DETECT LIGHT
    // =====================================

    private void DetectLamp()
    {
        if (isFleeing)
            return;

        if (lampScript == null)
            return;

        if (!lampScript.IsLightOn)
            return;

        float dist =
            Vector2.Distance(
                transform.position,
                lampScript.transform.position
            );

        if (dist <= lightRadius)
        {
            StartFleeing();
        }
    }

    // =====================================
    // HANDLE FLEE
    // =====================================

    private void HandleFleeTimer()
    {
        if (!isFleeing)
            return;

        fleeTimer -= Time.deltaTime;

        if (fleeTimer <= 0f)
        {
            StopFleeing();
        }
    }

    // =====================================
    // MOVE
    // =====================================

    private void Move()
    {
        int currentDir =
            isFleeing
            ? fleeDirection
            : patrolDirection;

        if (currentDir == 0)
        {
            currentDir = patrolDirection;
        }

        float speed =
            isFleeing
            ? runSpeed
            : walkSpeed;

        Vector2 rayOrigin =
            (Vector2)col.bounds.center +
            new Vector2(
                currentDir *
                (col.bounds.extents.x + 0.1f),
                0f
            );

        Vector2 rayDirection =
            new Vector2(currentDir, 0f);

        RaycastHit2D hit =
            Physics2D.Raycast(
                rayOrigin,
                rayDirection,
                wallCheckDistance,
                wallLayer
            );

        Debug.DrawRay(
            rayOrigin,
            rayDirection * wallCheckDistance,
            Color.red
        );

        if (hit.collider != null)
        {
            if (isFleeing)
            {
                fleeDirection *= -1;
            }
            else
            {
                patrolDirection *= -1;
            }

            return;
        }

        rb.linearVelocity =
            new Vector2(
                currentDir * speed,
                rb.linearVelocity.y
            );
    }

    // =====================================
    // FLIP
    // =====================================

    private void UpdateFlip()
    {
        if (rb.linearVelocity.x == 0f)
            return;

        sr.flipX =
            rb.linearVelocity.x < 0f;
    }

    // =====================================
    // ANIMATIONS
    // =====================================

    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        animator.SetBool(
            "Running",
            isFleeing
        );
    }

    // =====================================
    // TRIGGER LIGHT
    // =====================================

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (lampScript == null)
            return;

        if (
            other.gameObject ==
            lampScript.LightArea
        )
        {
            StartFleeing();
        }
    }

    // =====================================
    // START FLEE
    // =====================================

    private void StartFleeing()
    {
        if (lampScript == null)
            return;

        fleeDirection =
            (
                lampScript.transform.position.x >
                transform.position.x
            )
            ? -1
            : 1;

        isFleeing = true;

        fleeTimer = fleeDuration;
    }

    // =====================================
    // STOP FLEE
    // =====================================

    private void StopFleeing()
    {
        isFleeing = false;

        fleeDirection = 0;
    }

    // =====================================
    // DAMAGE PLAYER (mantém sistema existente)
    // =====================================

    private void OnCollisionStay2D(
        Collision2D other
    )
    {
        if (
            !other.gameObject.CompareTag(
                "Player"
            )
        )
            return;

        // mantém sistema de dano existente (sem alterar)
        TryDamage(other.gameObject);
    }

    // =====================================
    // HEAD SLIDE — AÇÃO IMEDIATA QUANDO O PLAYER PISA NA PARTE SUPERIOR DA BOX COLLIDER
    // =====================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        if (col == null)
            return;

        // Considera contato no "topo da box" quando ANY ponto de contato estiver próximo ou acima do bounds.max.y
        bool playerOnTop = false;
        float topY = col.bounds.max.y;
        const float epsilon = 0.02f; // tolerância para colisões ligeiramente internas

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.point.y >= topY - epsilon)
            {
                playerOnTop = true;
                break;
            }
        }

        if (!playerOnTop)
            return;

        // obtém o Rigidbody2D do player (suporta hierarquias)
        Rigidbody2D playerRb =
            collision.gameObject.GetComponentInParent<Rigidbody2D>();

        if (playerRb == null)
            playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (playerRb == null)
            return;

        // direção para empurrar: para o lado mais próximo (longe do centro do Walker)
        float dir = collision.transform.position.x >= transform.position.x ? 1f : -1f;
        if (dir == 0f) dir = 1f;

        // aplica velocidade imediata horizontal mantendo a componente vertical intacta
        playerRb.linearVelocity = new Vector2(dir * headSlideSpeed, playerRb.linearVelocityY);

        // aplica impulso horizontal configurável para garantir separação e "escorregamento" físico
        if (headSlideForce != 0f)
            playerRb.AddForce(new Vector2(dir * headSlideForce, 0f), ForceMode2D.Impulse);

        Debug.Log($"Walker -> Player pisou no topo. Forçando slide para {(dir > 0f ? "direita" : "esquerda")} (speed={headSlideSpeed}, impulse={headSlideForce})");
    }

    private void TryDamage(
        GameObject target
    )
    {
        if (
            Time.time <
            lastDamageTime +
            damageCooldown
        )
            return;

        PlayerController player =
            target.GetComponentInParent
            <PlayerController>();

        if (player == null)
            return;

        player.TakeDamage(
            damage,
            gameObject
        );

        lastDamageTime = Time.time;

        Debug.Log(
            $"Walker causou {damage} de dano em {player.name}"
        );
    }

    // =====================================
    // DAMAGE WALKER
    // =====================================

    public void TakeDamage(int damageAmount, GameObject source)
    {
        life -= damageAmount;

        Debug.Log($"Walker tomou {damageAmount} de {source.name} | Vida: {life}");

        if (life <= 0)
        {
            Die();
        }
    }

    // =====================================
    // DIE
    // =====================================

    private void Die()
    {
        if (morreu)
            return;

        morreu = true;

        Debug.Log("Walker morreu");

        if (col != null)
            col.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (animator != null)
            animator.SetBool("Running", false);

        // para o som de movimento
        if (audioSource != null)
            audioSource.Stop();

        if (animator != null)
        {
            try
            {
                animator.Play("Morrendo", 0, 0f);
            }
            catch { animator.SetTrigger("Morrer"); }

            StartCoroutine(HandleDeathAndDestroy());
        }
        else
        {
            Destroy(gameObject, 1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, raioVolumeMaximo);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioAudicao);
    }

    private IEnumerator HandleDeathAndDestroy()
    {
        // determine death clip length
        float length = 1f;
        var ac = animator.runtimeAnimatorController;
        if (ac != null)
        {
            foreach (var clip in ac.animationClips)
            {
                var name = clip.name.ToLower();
                if (name.Contains("morr") || name.Contains("morrendo") || name.Contains("morrer") || name.Contains("death"))
                {
                    length = clip.length;
                    break;
                }
            }
        }

        // wait the clip duration
        yield return new WaitForSeconds(length);

        // pause animator so it doesn't transition to other states
        if (animator != null)
            animator.speed = 0f;

        // keep object a short moment on last frame then destroy
        Destroy(gameObject, 0.2f);
    }
}