using UnityEngine;
using System.Collections;

public class CavaleiroFlamejante : MonoBehaviour, IDamageable
{
    [Header("REFERÊNCIAS")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform visual;

    [SerializeField] private GameObject colliderEspada;
    [SerializeField] private GameObject colliderEscudo;

    private Rigidbody2D rb;

    // =====================================
    // MOVIMENTO
    // =====================================

    [Header("MOVIMENTO")]
    [SerializeField] private float velocidade = 5f;

    // =====================================
    // DETECÇÃO
    // =====================================

    [Header("DETECÇÃO")]
    [SerializeField] private float rangeDeteccao = 8f;

    [SerializeField] private Vector2 offsetDeteccao;

    // =====================================
    // ATAQUE
    // =====================================

    [Header("ATAQUE")]
    [SerializeField] private float rangeAtaque = 2f;

    [SerializeField] private Vector2 offsetAtaque;

    [SerializeField] private int danoEspada = 3;

    [SerializeField] private float tempoAtaque = 0.4f;

    [SerializeField] private float cooldownAtaque = 1.5f;

    // =====================================
    // VIDA
    // =====================================

    [Header("VIDA")]
    [SerializeField] private int vidaMaxima = 10;

    private int vidaAtual;

    // =====================================
    // ESCUDO
    // =====================================

    [Header("ESCUDO")]
    [SerializeField] private int vidaEscudo = 3;

    private int vidaAtualEscudo;

    private bool escudoQuebrado;

    // =====================================
    // CONTROLE
    // =====================================

    private bool atacando;

    private bool podeAtacar = true;

    private bool perseguindo;

    private float ultimaDirecao = 1f;

    // =====================================
    // FEEDBACK
    // =====================================

    private SpriteRenderer spriteRenderer;

    private Color corOriginal;

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.freezeRotation = true;

        if (visual != null)
        {
            spriteRenderer =
                visual.GetComponent<SpriteRenderer>();
        }
    }

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        if (player == null)
        {
            PlayerController pc =
                FindFirstObjectByType<PlayerController>();

            if (pc != null)
            {
                player = pc.transform;
            }
        }

        vidaAtual = vidaMaxima;

        vidaAtualEscudo = vidaEscudo;

        escudoQuebrado = false;

        if (spriteRenderer != null)
        {
            corOriginal = spriteRenderer.color;
        }

        if (colliderEspada != null)
        {
            colliderEspada.SetActive(false);
        }

        if (colliderEscudo != null)
        {
            colliderEscudo.SetActive(true);
        }

        Debug.Log("🔥 Cavaleiro iniciado");
    }

    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        if (player == null)
            return;

        Vector2 centroDeteccao =
            (Vector2)transform.position
            + offsetDeteccao;

        Vector2 centroAtaque =
            (Vector2)transform.position
            + offsetAtaque;

        float distanciaDeteccao =
            Vector2.Distance(
                centroDeteccao,
                player.position
            );

        float distanciaAtaque =
            Vector2.Distance(
                centroAtaque,
                player.position
            );

        // =====================================
        // COMEÇOU A PERSEGUIR
        // =====================================

        if (
            !perseguindo &&
            distanciaDeteccao <= rangeDeteccao
        )
        {
            perseguindo = true;
        }

        // =====================================
        // NÃO DETECTOU
        // =====================================

        if (!perseguindo)
        {
            rb.linearVelocity =
                new Vector2(
                    0,
                    rb.linearVelocity.y
                );

            return;
        }

        // =====================================
        // DIREÇÃO
        // =====================================

        float direcao =
            player.position.x >
            transform.position.x
            ? 1f
            : -1f;

        // =====================================
        // FLIP
        // =====================================

        if (direcao != ultimaDirecao)
        {
            Vector3 escala =
                visual.localScale;

            escala.x =
                Mathf.Abs(escala.x)
                * direcao;

            visual.localScale =
                escala;

            ultimaDirecao = direcao;
        }

        // =====================================
        // ATAQUE
        // =====================================

        if (
            distanciaAtaque <= rangeAtaque
        )
        {
            rb.linearVelocity =
                new Vector2(
                    0,
                    rb.linearVelocity.y
                );

            if (
                podeAtacar &&
                !atacando
            )
            {
                StartCoroutine(
                    RotinaAtaque()
                );
            }
        }
        else
        {
            // =====================================
            // PERSEGUIR
            // =====================================

            if (!atacando)
            {
                rb.linearVelocity =
                    new Vector2(
                        direcao * velocidade,
                        rb.linearVelocity.y
                    );
            }
        }
    }

    // =====================================
    // ATAQUE
    // =====================================

    IEnumerator RotinaAtaque()
    {
        atacando = true;

        podeAtacar = false;

        rb.linearVelocity =
            new Vector2(
                0,
                rb.linearVelocity.y
            );

        Debug.Log("⚔️ ATAQUE");

        if (colliderEspada != null)
        {
            colliderEspada.SetActive(true);
        }

        yield return new WaitForSeconds(
            tempoAtaque
        );

        if (colliderEspada != null)
        {
            colliderEspada.SetActive(false);
        }

        atacando = false;

        yield return new WaitForSeconds(
            cooldownAtaque
        );

        podeAtacar = true;
    }

    // =====================================
    // TOMAR DANO
    // =====================================

    public void TakeDamage(
        int amount,
        GameObject source
    )
    {
        // =====================================
        // ESCUDO
        // =====================================

        if (!escudoQuebrado)
        {
            vidaAtualEscudo -= amount;

            Debug.Log(
                "🛡️ Escudo restante: "
                + vidaAtualEscudo
            );

            if (vidaAtualEscudo <= 0)
            {
                QuebrarEscudo();
            }

            StartCoroutine(PiscarDano());

            return;
        }

        // =====================================
        // VIDA
        // =====================================

        vidaAtual -= amount;

        Debug.Log(
            "💥 Vida restante: "
            + vidaAtual
        );

        StartCoroutine(PiscarDano());

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    // =====================================
    // ESCUDO
    // =====================================

    void QuebrarEscudo()
    {
        escudoQuebrado = true;

        Debug.Log("💥 Escudo quebrado");

        if (colliderEscudo != null)
        {
            colliderEscudo.SetActive(false);
        }
    }

    public void RegenerarEscudo()
    {
        if (!escudoQuebrado)
            return;

        escudoQuebrado = false;

        vidaAtualEscudo = vidaEscudo;

        Debug.Log("✨ Escudo regenerado");

        if (colliderEscudo != null)
        {
            colliderEscudo.SetActive(true);
        }
    }

    // =====================================
    // MORTE
    // =====================================

    void Morrer()
    {
        Debug.Log("☠️ MORREU");

        Destroy(gameObject);
    }

    // =====================================
    // PISCAR DANO
    // =====================================

    IEnumerator PiscarDano()
    {
        if (spriteRenderer == null)
            yield break;

        spriteRenderer.color =
            Color.red;

        yield return new WaitForSeconds(
            0.1f
        );

        spriteRenderer.color =
            corOriginal;
    }

    // =====================================
    // TRIGGERS
    // =====================================

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        // =====================================
        // DANO PLAYER
        // =====================================

        if (
            other.CompareTag("Player") &&
            colliderEspada != null &&
            colliderEspada.activeInHierarchy
        )
        {
            IDamageable dmg =
                other.GetComponent<IDamageable>();

            if (dmg != null)
            {
                dmg.TakeDamage(
                    danoEspada,
                    gameObject
                );

                Debug.Log("⚔️ Espada acertou");
            }
        }

        // =====================================
        // ATAQUE PLAYER
        // =====================================

        if (
            other.CompareTag("PlayerAttack")
        )
        {
            if (!escudoQuebrado)
            {
                TakeDamage(
                    1,
                    other.gameObject
                );
            }
            else
            {
                TakeDamage(
                    2,
                    other.gameObject
                );
            }
        }
    }

    // =====================================
    // LUZ
    // =====================================

    private void OnTriggerStay2D(
        Collider2D other
    )
    {
        if (
            other.CompareTag("LuzLampiao")
        )
        {
            RegenerarEscudo();
        }
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            (Vector2)transform.position
            + offsetDeteccao,
            rangeDeteccao
        );

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            (Vector2)transform.position
            + offsetAtaque,
            rangeAtaque
        );
    }
}