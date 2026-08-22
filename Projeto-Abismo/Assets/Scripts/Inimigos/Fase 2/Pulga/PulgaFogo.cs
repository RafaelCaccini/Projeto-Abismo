using UnityEngine;
using System.Collections;

public class PulgaFogo : MonoBehaviour, IDamageable
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    private Rigidbody2D rb;

    private Transform player;

    private Collider2D col;

    private Animator animator;

    // =====================================
    // MOVIMENTO
    // =====================================

    [Header("Movimento")]
    [SerializeField] private float forcaPuloX = 4f;

    [SerializeField] private float forcaPuloY = 7f;

    [SerializeField] private float tempoEntrePulos = 1f;

    [SerializeField] private bool iniciarViradoDireita = true;

    // =====================================
    // DETECÇÃO
    // =====================================

    [Header("Detecção")]
    [SerializeField] private float rangeAtivacao = 8f;

    [SerializeField] private LayerMask wallLayer;

    // =====================================
    // VIDA
    // =====================================

    [Header("Vida")]
    [SerializeField] private int vida = 3;

    // =====================================
    // MORTE
    // =====================================

    [Header("Morte")]
    [SerializeField] private float duracaoAnimacaoMorte = 1f;
    [SerializeField] private string triggerMorte = "Morrer";

    // =====================================
    // DANO
    // =====================================

    [Header("Dano")]
    [SerializeField] private int dano = 1;

    [SerializeField] private float cooldownDano = 1f;

    // =====================================
    // DEBUG
    // =====================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // =====================================
    // CONTROLE
    // =====================================

    private bool olhandoDireita;

    private bool podeDarDano = true;

    private bool morto = false;

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        col = GetComponent<Collider2D>();

        animator = GetComponentInChildren<Animator>();

        if (rb == null)
        {
            Debug.LogError(
                "[PulgaFogo] Rigidbody2D não encontrado!"
            );
        }

        if (animator == null)
        {
            Debug.LogError(
                "[PulgaFogo] Animator não encontrado!"
            );
        }
    }

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        BuscarPlayer();

        olhandoDireita =
            iniciarViradoDireita;

        AtualizarDirecaoVisual(
            olhandoDireita ? 1f : -1f
        );

        StartCoroutine(
            RotinaPulos()
        );
    }

    // =====================================
    // BUSCAR PLAYER
    // =====================================

    void BuscarPlayer()
    {
        PlayerController pc =
            FindFirstObjectByType<PlayerController>();

        if (pc != null)
        {
            player = pc.transform;

            if (debugLogs)
            {
                Debug.Log(
                    "✅ PLAYER ENCONTRADO"
                );
            }
        }
    }

    // =====================================
    // ROTINA PULOS
    // =====================================

    IEnumerator RotinaPulos()
    {
        yield return new WaitForSeconds(1f);

        while (!morto)
        {
            yield return new WaitForSeconds(
                tempoEntrePulos
            );

            // tenta achar player de novo

            if (player == null)
            {
                BuscarPlayer();
                continue;
            }

            // DISTÂNCIA PLAYER

            float distancia =
                Vector2.Distance(
                    transform.position,
                    player.position
                );

            if (debugLogs)
            {
                Debug.Log(
                    "📏 Distância Player: "
                    + distancia
                );
            }

            // PLAYER FORA RANGE

            if (distancia > rangeAtivacao)
            {
                if (debugLogs)
                {
                    Debug.Log(
                        "🚫 Player fora do range"
                    );
                }

                continue;
            }

            // DETECÇÃO CHÃO

            Vector2 origemRay =
                new Vector2(
                    col.bounds.center.x,
                    col.bounds.min.y + 0.05f
                );

            bool noChao =
                Physics2D.Raycast(
                    origemRay,
                    Vector2.down,
                    0.2f,
                    wallLayer
                );

            // DEBUG CHÃO

            if (debugLogs)
            {
                Debug.Log(
                    "🟢 No chão: "
                    + noChao
                );
            }

            // NÃO ESTÁ NO CHÃO

            if (!noChao)
            {
                continue;
            }

            // PULAR

            FazerPulo();
        }
    }

    // =====================================
    // PULO
    // =====================================

    void FazerPulo()
    {
        // chance trocar direção

        if (Random.value > 0.5f)
        {
            olhandoDireita =
                !olhandoDireita;
        }

        float direcao =
            olhandoDireita ? 1f : -1f;

        AtualizarDirecaoVisual(
            direcao
        );

        // dispara animação de pulo
        if (animator != null)
            animator.SetTrigger("Pular");

        // RESET VELOCIDADE

        rb.linearVelocity =
            Vector2.zero;

        // FORÇA PULO

        rb.AddForce(
            new Vector2(
                direcao * forcaPuloX,
                forcaPuloY
            ),
            ForceMode2D.Impulse
        );

        if (debugLogs)
        {
            Debug.Log(
                "🔥 PULGA PULOU"
            );
        }
    }

    // =====================================
    // VISUAL
    // =====================================

    void AtualizarDirecaoVisual(
        float direcao
    )
    {
        Vector3 escala =
            transform.localScale;

        if (direcao > 0)
        {
            escala.x =
                Mathf.Abs(escala.x);
        }
        else
        {
            escala.x =
                -Mathf.Abs(escala.x);
        }

        transform.localScale =
            escala;
    }

    // =====================================
    // DANO PLAYER
    // =====================================

    private void OnCollisionStay2D(
        Collision2D collision
    )
    {
        if (morto)
            return;

        if (!podeDarDano)
            return;

        if (
            !collision.gameObject.CompareTag(
                "Player"
            )
        )
            return;

        PlayerController pc =
            collision.gameObject
            .GetComponent<PlayerController>();

        if (pc == null)
            return;

        pc.TakeDamage(
            dano,
            gameObject
        );

        if (debugLogs)
        {
            Debug.Log(
                "🔥 Pulga atacou"
            );
        }

        StartCoroutine(
            CooldownDano()
        );
    }

    IEnumerator CooldownDano()
    {
        podeDarDano = false;

        yield return new WaitForSeconds(
            cooldownDano
        );

        podeDarDano = true;
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

        vida -= amount;

        if (debugLogs)
        {
            Debug.Log(
                "💥 Pulga tomou "
                + amount +
                " | Vida: "
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

        // Para todas as rotinas, principalmente os pulos
        StopAllCoroutines();

        // Para completamente o movimento
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Desativa todos os colliders, inclusive filhos
        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>();

        foreach (Collider2D c in colliders)
        {
            c.enabled = false;
        }

        // Dispara animação de morte
        if (animator != null)
        {
            animator.ResetTrigger(triggerMorte);
            animator.SetTrigger(triggerMorte);
        }

        if (debugLogs)
        {
            Debug.Log(
                "☠️ Pulga morreu - iniciando animação de morte"
            );
        }

        // Espera a animação terminar
        StartCoroutine(
            FinalizarMorte()
        );
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            rangeAtivacao
        );

        if (col != null)
        {
            Vector2 origemRay =
                new Vector2(
                    col.bounds.center.x,
                    col.bounds.min.y + 0.05f
                );

            Gizmos.color = Color.red;

            Gizmos.DrawRay(
                origemRay,
                Vector2.down * 0.2f
            );
        }
    }
    IEnumerator FinalizarMorte()
    {
        yield return new WaitForSeconds(
            duracaoAnimacaoMorte
        );

        if (debugLogs)
        {
            Debug.Log(
                "☠️ Animação de morte terminou - destruindo Pulga"
            );
        }

        Destroy(gameObject);
    }
}