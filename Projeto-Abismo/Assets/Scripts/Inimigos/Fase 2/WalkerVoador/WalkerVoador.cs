using UnityEngine;

public class WalkerVoador : MonoBehaviour, IDamageable
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("REFERÊNCIAS")]
    [SerializeField] private Transform visual;

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
    [SerializeField] private float distanciaParede = 0.6f;

    [SerializeField] private LayerMask paredeLayer;

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

        // =====================================
        // SEGURANÇA
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

    void Mover()
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
        // RAYCAST
        // =====================================

        RaycastHit2D hit =
            Physics2D.Raycast(
                origem,
                direcaoRay,
                distanciaParede,
                paredeLayer
            );

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
        // PAREDE
        // =====================================

        if (hit.collider != null)
        {
            Debug.Log(
                "🧱 Parede detectada"
            );

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

    void Virar()
    {
        direcao *= -1;

        Debug.Log(
            "🔄 Virou direção: "
            + direcao
        );

        if (visual == null)
            return;

        Vector3 escala =
            visual.localScale;

        escala.x =
            Mathf.Abs(escala.x)
            * direcao;

        visual.localScale =
            escala;
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

    void Morrer()
    {
        if (morto)
            return;

        morto = true;

        Debug.Log(
            "☠️ WalkerVoador morreu"
        );

        rb.linearVelocity = Vector2.zero;

        if (col != null)
        {
            col.enabled = false;
        }

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