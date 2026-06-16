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

        HandleFleeTimer();
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
    // DAMAGE PLAYER
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

        TryDamage(other.gameObject);
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