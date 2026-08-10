using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class WalkerVoador : MonoBehaviour, IDamageable
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("REFERÊNCIAS")]
    [Tooltip("SpriteRenderer que será flipado horizontalmente. Se nulo, busca automaticamente no filho.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Ponto de origem do raycast que detecta paredes à frente.")]
    [SerializeField] private Transform wallCheck;

    // =====================================
    // MOVIMENTO
    // =====================================

    [Header("MOVIMENTO")]
    [SerializeField] private float velocidade = 3f;

    // =====================================
    // PAREDE
    // =====================================

    [Header("PAREDE")]
    [Tooltip("LayerMask para detecção por layer (ex: wallLayer).")]
    [SerializeField] private LayerMask paredeLayer;

    [Tooltip("Tag usada como fallback quando a LayerMask não bate. Deixe vazio para desativar.")]
    [SerializeField] private string wallTag = "Wall";

    [SerializeField] private float distanciaParede = 0.6f;

    // =====================================
    // DANO
    // =====================================

    [Header("DANO")]
    [SerializeField] private int dano = 1;

    [SerializeField] private float cooldownDano = 1f;

    // =====================================
    // VIDA
    // =====================================

    [Header("VIDA")]
    [SerializeField] private int vida = 5;

    [SerializeField] private bool podeMorrer = true;

    // =====================================
    // DEBUG
    // =====================================

    [Header("DEBUG")]
    [SerializeField] private bool mostrarRaycast = true;

    // =====================================
    // COMPONENTES
    // =====================================

    private Rigidbody2D rb;

    private Collider2D col;

    // =====================================
    // CONTROLE
    // =====================================

    private int direcao = 1;

    private float ultimoDano;

    private bool morto;

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        col = GetComponent<Collider2D>();

        spriteRenderer =
            spriteRenderer != null
            ? spriteRenderer
            : GetComponentInChildren<SpriteRenderer>();

        if (
            wallCheck == null
        )
        {
            wallCheck = new GameObject(
                "WallCheck"
            ).transform;

            wallCheck.SetParent(
                transform,
                worldPositionStays: false
            );

            wallCheck.localPosition = Vector3.zero;
        }

        // =====================================
        // SEGURANÇA RB
        // =====================================

        if (rb != null)
        {
            rb.gravityScale = 0f;

            rb.freezeRotation = true;

            rb.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            rb.bodyType =
                RigidbodyType2D.Dynamic;
        }

        Debug.Log("🦇 WalkerVoador iniciado");
    }

    // =====================================
    // FIXED UPDATE
    // =====================================

    private void FixedUpdate()
    {
        if (morto)
            return;

        Mover();
    }

    // =====================================
    // MOVIMENTO
    // =====================================

    private void Mover()
    {
        if (
            rb == null
            || wallCheck == null
        )
            return;

        Vector2 origem =
            wallCheck.position;

        Vector2 direcaoRay =
            Vector2.right * direcao;

        // =====================================
        // DETECÇÃO DE PAREDE (LAYER + TAG)
        // =====================================

        RaycastHit2D hit =
            Physics2D.Raycast(
                origem,
                direcaoRay,
                distanciaParede,
                paredeLayer
            );

        // Se a LayerMask não bate, tenta por tag
        if (
            hit.collider == null
            && !string.IsNullOrEmpty(wallTag)
        )
        {
            RaycastHit2D tagHit =
                Physics2D.Raycast(
                    origem,
                    direcaoRay,
                    distanciaParede
                );

            if (
                tagHit.collider != null
                && tagHit.collider.CompareTag(wallTag)
            )
            {
                hit = tagHit;
            }
        }

        // =====================================
        // DEBUG
        // =====================================

        if (mostrarRaycast)
        {
            Debug.DrawRay(
                origem,
                direcaoRay
                * distanciaParede,
                hit.collider != null
                    ? Color.green
                    : Color.red
            );
        }

        // =====================================
        // PAREDE ENCONTRADA
        // =====================================

        if (hit.collider != null)
        {
            Debug.Log("🧱 Parede detectada");

            Virar();

            return;
        }

        // =====================================
        // MOVIMENTO
        // =====================================

        rb.linearVelocity =
            new Vector2(
                direcao * velocidade,
                0f
            );
    }

    // =====================================
    // VIRAR
    // =====================================

    private void Virar()
    {
        direcao *= -1;

        Debug.Log(
            "🔄 Virou direção: "
            + direcao
        );

        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            direcao < 0;
    }

    // =====================================
    // DANO PLAYER
    // =====================================

    private void OnCollisionStay2D(
        Collision2D other
    )
    {
        if (morto)
            return;

        if (
            !other.gameObject.CompareTag(
                "Player"
            )
        )
            return;

        if (
            Time.time <
            ultimoDano + cooldownDano
        )
            return;

        PlayerController player =
            other.gameObject.GetComponent<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning(
                "❌ PlayerController não encontrado"
            );

            return;
        }

        player.TakeDamage(
            dano,
            gameObject
        );

        ultimoDano = Time.time;

        Debug.Log(
            "💥 WalkerVoador causou dano"
        );
    }

    // =====================================
    // TOMAR DANO
    // =====================================

    public void TakeDamage(
        int amount,
        GameObject source
    )
    {
        if (morto)
            return;

        Debug.Log(
            "💥 WalkerVoador recebeu dano"
        );

        // =====================================
        // IMORTAL
        // =====================================

        if (!podeMorrer)
        {
            Debug.Log(
                "🛡️ WalkerVoador imortal"
            );

            return;
        }

        vida -= amount;

        Debug.Log(
            "🦇 Vida restante: "
            + vida
        );

        if (vida <= 0)
        {
            Morrer();
        }
    }

    // =====================================
    // MORRER
    // =====================================

    private void Morrer()
    {
        if (morto)
            return;

        morto = true;

        Debug.Log(
            "☠️ WalkerVoador morreu"
        );

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (col != null)
            col.enabled = false;

        Destroy(gameObject, 0.05f);
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        if (wallCheck == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            wallCheck.position,
            wallCheck.position
            + Vector3.right
            * direcao
            * distanciaParede
        );
    }
}
