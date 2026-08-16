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

        rb.bodyType =
            RigidbodyType2D.Dynamic;

        rb.gravityScale = 3f;

        rb.freezeRotation = true;

        if (lampScript == null)
        {
            lampScript =
                FindFirstObjectByType<Lampiao>();
        }
    }

    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        if (morreu)
            return;

        DetectLamp();
        UpdateAnimations();
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
        if (lampScript == null)
        {
            if (isFleeing)
                StopFleeing();
            return;
        }

        // Luz desligada → para de fugir
        if (!lampScript.IsLightOn)
        {
            if (isFleeing)
                StopFleeing();
            return;
        }

        float dist =
            Vector2.Distance(
                transform.position,
                lampScript.transform.position
            );

        // Dentro do raio de detecção → foge
        if (dist <= lightRadius)
        {
            StartFleeing();
        }
        // Fora do raio e já estava fugindo → para (chegou longe o suficiente)
        else if (isFleeing)
        {
            StopFleeing();
        }
    }

    // =====================================
    // MOVE
    // =====================================

    private void Move()
    {
        int currentDir;
        float speed;

        if (isFleeing)
        {
            // RECALCULA direção de fuga toda frame: sempre para longe da luz
            fleeDirection =
                lampScript.transform.position.x >
                transform.position.x
                    ? -1
                    : 1;

            currentDir = fleeDirection;
            speed = runSpeed;
        }
        else
        {
            fleeDirection = 0;
            currentDir = patrolDirection;
            speed = walkSpeed;
        }

        if (currentDir == 0)
        {
            currentDir = patrolDirection;
        }

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
                // EM FUGA: NÃO inverte direção.
                // Fica parado contra a parede, mantendo o estado de fuga.
                // Só conseguirá se mover quando a luz trocar de lado
                // ou quando a parede não bloquear mais o caminho de fuga.
                rb.linearVelocity =
                    new Vector2(
                        0f,
                        rb.linearVelocity.y
                    );
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
        // Direção inicial (será recalculada toda frame em Move)
        if (lampScript != null)
        {
            fleeDirection =
                lampScript.transform.position.x >
                transform.position.x
                    ? -1
                    : 1;
        }

        isFleeing = true;
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
        // EM FUGA: ignora o player completamente
        if (isFleeing)
            return;

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
        // EM FUGA: ignora interações com o player (nem head slide)
        if (isFleeing)
            return;

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

    public void TakeDamage(
        int damageAmount,
        GameObject source
    )
    {
        life -= damageAmount;

        Debug.Log(
            $"Walker tomou {damageAmount} de {source.name} | Vida: {life}"
        );

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

        // stop movement and interactions
        if (col != null)
            col.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // ensure Running flag off
        if (animator != null)
            animator.SetBool("Running", false);

        // play death state directly (avoid transitions back)
        if (animator != null)
        {
            // try to play the death state by name, fallback to trigger
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