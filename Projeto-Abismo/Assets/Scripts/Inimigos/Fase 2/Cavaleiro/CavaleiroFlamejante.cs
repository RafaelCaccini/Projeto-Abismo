using UnityEngine;
using System.Collections;

public class EnemyDashAttack : MonoBehaviour, IDamageable
{
    // =====================================
    // REFERÊNCIAS
    // =====================================


    [Header("Referências")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform visual;

    private Rigidbody2D rb;

    // =====================================
    // DETECÇÃO
    // =====================================

    [Header("Detecção")]
    [SerializeField] private float raioDeteccao = 12f;
    [SerializeField] private float raioCorpoACorpo = 2f;

    // =====================================
    // DASH
    // =====================================

    [Header("Dash")]
    [SerializeField] private float velocidadeDash = 14f;
    [SerializeField] private float duracaoDash = 0.3f;
    [SerializeField] private float cooldownDash = 1.5f;
    [SerializeField] private int danoDash = 15;

    // =====================================
    // CORPO A CORPO
    // =====================================

    [Header("Corpo a Corpo")]
    [SerializeField] private int danoCorpoACorpo = 5;
    [SerializeField] private float cooldownCorpoACorpo = 1f;

    // =====================================
    // PERSEGUIÇÃO
    // =====================================

    [Header("Perseguição")]
    [SerializeField] private float velocidadePerseguicao = 2f;

    // =====================================
    // VIDA
    // =====================================

    [Header("Vida")]
    [SerializeField] private int vida = 3;

    // =====================================
    // GIZMOS
    // =====================================

    [Header("Gizmos")]
    [SerializeField] private Color corDeteccao = Color.yellow;
    [SerializeField] private Color corCorpoACorpo = Color.red;

    // =====================================
    // DEBUG
    // =====================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // =====================================
    // CONTROLE
    // =====================================

    private float dashDuracaoAtual;
    private float dashCooldownAtual;
    private float corpoACorpoTimer;

    private bool isDashing;
    private bool podeDarDanoDash = true;
    private bool morto = false;

    private Vector2 direcaoDash;

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        dashCooldownAtual = 0f;
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        Debug.Log(
            "[EnemyDashAttack] Start | Player: "
            + (player != null ? player.name : "NULL")
            + " | Rigidbody2D: "
            + (rb != null ? "OK (BodyType=" + rb.bodyType + ")" : "NULL")
            + " | Componente ativo: " + this.enabled
            + " | RaioDeteccao: " + raioDeteccao
        );
    }

    // =====================================
    // FIXED UPDATE
    // =====================================

    private void FixedUpdate()
    {
        if (morto)
            return;

        if (player == null)
            return;

        // VIRAR SPRITE
        if (visual != null)
        {
            Vector3 escala = visual.localScale;

            if (player.position.x > transform.position.x)
            {
                escala.x = Mathf.Abs(escala.x);
            }
            else
            {
                escala.x = -Mathf.Abs(escala.x);
            }

            visual.localScale = escala;
        }

        dashDuracaoAtual -= Time.fixedDeltaTime;
        dashCooldownAtual -= Time.fixedDeltaTime;
        corpoACorpoTimer -= Time.fixedDeltaTime;

        float distancia =
            Vector2.Distance(
                transform.position,
                player.position
            );

        if (distancia <= raioCorpoACorpo)
        {
            isDashing = false;
            rb.linearVelocity = Vector2.zero;

            AtacarCorpoACorpo();
            return;
        }

        if (distancia <= raioDeteccao)
        {
            if (isDashing)
            {
                ExecutarDash();
            }
            else if (dashCooldownAtual <= 0f)
            {
                IniciarDash();
            }
            else
            {
                Perseguir();
            }
        }
    }

    // =====================================
    // PERSEGUIR
    // =====================================

    void Perseguir()
    {
        Vector2 direcao =
            (player.position - transform.position)
            .normalized;

        Vector2 novaPosicao =
            rb.position +
            direcao *
            velocidadePerseguicao *
            Time.fixedDeltaTime;

        rb.MovePosition(novaPosicao);
    }

    // =====================================
    // DASH
    // =====================================

    void IniciarDash()
    {
        isDashing = true;

        podeDarDanoDash = true;

        direcaoDash =
            (player.position - transform.position)
            .normalized;

        dashDuracaoAtual = duracaoDash;

        if (debugLogs)
        {
            Debug.Log("⚡ Inseto iniciou dash");
        }
    }

    void ExecutarDash()
    {
        Vector2 novaPosicao =
            rb.position +
            direcaoDash *
            velocidadeDash *
            Time.fixedDeltaTime;

        rb.MovePosition(novaPosicao);

        if (dashDuracaoAtual <= 0f)
        {
            isDashing = false;

            dashCooldownAtual = cooldownDash;

            rb.linearVelocity = Vector2.zero;
        }
    }

    // =====================================
    // DANO DASH (COLISÃO)
    // =====================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (morto)
            return;

        if (!isDashing)
            return;

        if (!podeDarDanoDash)
            return;

        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerController pc =
            collision.gameObject
            .GetComponent<PlayerController>();

        if (pc == null)
            return;

        pc.TakeDamage(danoDash, gameObject);

        podeDarDanoDash = false;

        if (debugLogs)
        {
            Debug.Log("💥 Dash acertou player | Dano: " + danoDash);
        }

        isDashing = false;
        dashCooldownAtual = cooldownDash;
        rb.linearVelocity = Vector2.zero;
    }

    // =====================================
    // DANO CORPO A CORPO
    // =====================================

    void AtacarCorpoACorpo()
    {
        if (corpoACorpoTimer > 0f)
            return;

        PlayerController pc =
            player.GetComponent<PlayerController>();

        if (pc == null)
            return;

        pc.TakeDamage(danoCorpoACorpo, gameObject);

        corpoACorpoTimer = cooldownCorpoACorpo;

        if (debugLogs)
        {
            Debug.Log("👊 Inseto atacou corpo a corpo | Dano: " + danoCorpoACorpo);
        }
    }

    // =====================================
    // TOMAR DANO
    // =====================================

    public void TakeDamage(int amount, GameObject source)
    {
        if (morto)
            return;

        vida -= amount;

        if (debugLogs)
        {
            Debug.Log(
                "🪲 Inseto tomou "
                + amount +
                " de dano | Vida: "
                + vida
            );
        }

        if (vida <= 0)
        {
            Morrer();
        }
    }

    // =====================================
    // MORTE
    // =====================================

    void Morrer()
    {
        if (morto)
            return;

        morto = true;

        if (debugLogs)
        {
            Debug.Log("☠️ Inseto morreu");
        }

        rb.linearVelocity = Vector2.zero;

        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            c.enabled = false;
        }

        Destroy(gameObject, 0.1f);
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected() 
    {
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        Vector3 centro = transform.position;

        if (col != null)
        {
            centro += (Vector3)col.offset;
        }

        Gizmos.color = corDeteccao;
        Gizmos.DrawWireSphere(centro, raioDeteccao);

        Gizmos.color = corCorpoACorpo;
        Gizmos.DrawWireSphere(centro, raioCorpoACorpo);
    }

    
}