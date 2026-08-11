using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class VoadorWalker : MonoBehaviour, IDamageable
{
    // ======== REFER�NCIAS ========
    [Header("REFER�NCIAS")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    // ======== MOVIMENTO ========
    [Header("MOVIMENTO")]
    [SerializeField] private float velocidade = 3f;
    [SerializeField] private bool startFacingRight = true;

    // ======== PAREDE ========
    [Header("PAREDE")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.6f;

    // ======== COMBATE ========
    [Header("COMBATE")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float danoCooldown = 1f;

    // ======== VIDA ========
    [Header("VIDA")]
    [SerializeField] private int vidaMaxima = 5;
    [SerializeField] private bool podeMorrer = true;

    // ======== MORTE ========
    [Header("MORTE")]
    [SerializeField] private float deathAnimationDuration = 1f;

    // ======== DEBUG ========
    [Header("DEBUG")]
    [SerializeField] private bool mostrarRaycast = true;

    // internal
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform visualTransform;

    private int direcao = 1; // 1 = right, -1 = left
    private float lastDamageTime = -999f;
    private bool morto = false;

    private int vidaAtual;

    private static readonly int AnimatorParam_IsDead = Animator.StringToHash("IsDead");
    private static readonly int AnimatorParam_IsMoving = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError("[VoadorWalker] Rigidbody2D ausente!");
            enabled = false;
            return;
        }

        if (col == null)
        {
            Debug.LogError("[VoadorWalker] Collider2D ausente!");
            enabled = false;
            return;
        }

        // find optional components
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // physics setup
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // initial values
        direcao = startFacingRight ? 1 : -1;
        vidaAtual = vidaMaxima;

        // visual transform
        if (spriteRenderer != null)
            visualTransform = spriteRenderer.transform;
    }

    private void Start()
    {
        UpdateSpriteFlip();
        UpdateAnimatorMoving(true);
    }

    // DEBUG: move manualmente para testar Rigidbody (botão direito no Inspector → "ForceMove")
    [ContextMenu("ForceMove")]
    private void ForceMove()
    {
        rb.position += Vector2.right * 0.1f;
        Debug.Log("[VoadorWalker] ForceMove executado");
    }

    private void FixedUpdate()
    {
        if (morto)
        {
            Debug.Log($"[VoadorWalker] morto=true, pulando movimento");
            return;
        }

        PatrolMove();
        CheckWallAndFlip();

        Debug.Log($"[VoadorWalker] dir={direcao} vel={velocidade} rbVel={rb.linearVelocity} wallCheckDist={wallCheckDistance}");
    }

    private void PatrolMove()
    {
        rb.linearVelocity = new Vector2(direcao * velocidade, rb.linearVelocity.y);
        UpdateAnimatorMoving(true);
    }

    private void CheckWallAndFlip()
    {
        if (col == null) return;

        // Position the raycast at the front edge of the collider,
        // based on the current direction. This avoids hitting the
        // enemy's own collider (which happened with a fixed wallCheck).
        Vector2 origin = (Vector2)col.bounds.center + new Vector2(direcao * (col.bounds.extents.x + 0.05f), 0f);
        Vector2 dir = Vector2.right * direcao;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, wallCheckDistance, wallLayer);
        Debug.Log($"[VoadorWalker] Raycast from {origin} dir {dir} hit={(hit.collider != null ? hit.collider.name : "null")}");

        if (mostrarRaycast)
        {
            Debug.DrawRay(origin, dir * wallCheckDistance, hit.collider != null ? Color.green : Color.red);
        }

        if (hit.collider != null)
        {
            FlipDirection();
        }
    }

    private void FlipDirection()
    {
        direcao *= -1;
        UpdateSpriteFlip();
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = direcao < 0;
    }

    private void UpdateAnimatorMoving(bool moving)
    {
        if (animator == null) return;
        if (morto) return;
        animator.SetBool(AnimatorParam_IsMoving, moving);
    }

    // Damage to player on contact (cooldown enforced)
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (morto) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (Time.time < lastDamageTime + danoCooldown) return;

        var player = collision.gameObject.GetComponent<PlayerController>() ?? collision.gameObject.GetComponentInParent<PlayerController>();
        if (player == null) return;

        player.TakeDamage(dano, gameObject);
        lastDamageTime = Time.time;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // also attempt damage immediately on enter
        if (morto) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (Time.time < lastDamageTime + danoCooldown) return;

        var player = collision.gameObject.GetComponent<PlayerController>() ?? collision.gameObject.GetComponentInParent<PlayerController>();
        if (player == null) return;

        player.TakeDamage(dano, gameObject);
        lastDamageTime = Time.time;
    }

    // IDamageable implementation
    public void TakeDamage(int amount, GameObject source)
    {
        if (morto) return;
        if (!podeMorrer) return;

        vidaAtual -= amount;
        if (vidaAtual <= 0)
        {
            vidaAtual = 0;
            StartCoroutine(HandleDeath());
        }
        else
        {
            // optional hit feedback: play hit animation if exists
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
        }
    }

    private IEnumerator HandleDeath()
    {
        morto = true;

        // stop moving
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // disable colliders
        foreach (var c in GetComponents<Collider2D>())
            c.enabled = false;

        // animator
        if (animator != null)
        {
            animator.SetBool(AnimatorParam_IsDead, true);
            // ensure movement param off
            animator.SetBool(AnimatorParam_IsMoving, false);
            // wait for death animation duration
            yield return new WaitForSeconds(deathAnimationDuration);
        }
        else
        {
            // small delay to allow any audio/effects
            yield return new WaitForSeconds(0.05f);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (col != null)
        {
            Vector2 origin = (Vector2)col.bounds.center + new Vector2(direcao * (col.bounds.extents.x + 0.05f), 0f);
            Vector2 dir = Vector2.right * direcao;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + dir * wallCheckDistance);
        }
    }
}
