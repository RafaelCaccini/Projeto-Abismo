using UnityEngine;

public class PiranhaEnemy : MonoBehaviour
{
    // =============================================
    // MOVIMENTO
    // =============================================

    [Header("Movimento")]
    [SerializeField] private float velocidade = 5f;
    [SerializeField] private float aceleracao = 8f;

    // =============================================
    // DETECÇÃO
    // =============================================

    [Header("Detecção")]
    [SerializeField] private float raioDeteccao = 15f;

    // =============================================
    // ATAQUE
    // =============================================

    [Header("Ataque")]
    [SerializeField] private float distanciaAtaque = 1.5f;
    [SerializeField] private int dano = 1;
    [SerializeField] private float cooldownDano = 1f;

    // =============================================
    // VIDA
    // =============================================

    [Header("Vida")]
    [SerializeField] private int vida = 3;

    // =============================================
    // ÁGUA
    // =============================================

    [Header("Água")]
    [SerializeField] private LayerMask layerAgua;

    // =============================================
    // DEBUG
    // =============================================

    [Header("Debug")]
    [SerializeField] private bool logs = false;
    [SerializeField] private bool gizmos = true;

    // =============================================
    // REFERÊNCIAS
    // =============================================

    private Transform player;
    private Rigidbody2D rb;

    private float velocidadeAtual;
    private float proximoDano;

    private bool perseguindo;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        ProcurarPlayer();

        if (player == null)
        {
            Parar();
            return;
        }

        float distancia = Vector2.Distance(
            transform.position,
            player.position
        );

        // =========================================
        // PLAYER FORA DO RAIO
        // =========================================

        if (distancia > raioDeteccao)
        {
            if (perseguindo && logs)
                Debug.Log("[Piranha] Player saiu do raio de detecção.");

            perseguindo = false;
            Parar();

            return;
        }

        // =========================================
        // PLAYER DENTRO DO RAIO
        // =========================================

        if (!perseguindo && logs)
            Debug.Log("[Piranha] Player detectado!");

        perseguindo = true;

        // =========================================
        // DISTÂNCIA DE ATAQUE
        // =========================================

        if (distancia <= distanciaAtaque)
        {
            Parar();
            Atacar();

            return;
        }

        // =========================================
        // PERSEGUIÇÃO
        // =========================================

        Perseguir();
    }

    // =============================================
    // PROCURAR PLAYER
    // =============================================

    private void ProcurarPlayer()
    {
        GameObject objetoPlayer = GameObject.FindGameObjectWithTag("Player");

        if (objetoPlayer == null)
        {
            player = null;
            return;
        }

        player = objetoPlayer.transform;
    }

    // =============================================
    // PERSEGUIR
    // =============================================

    private void Perseguir()
    {
        if (player == null)
            return;

        Vector2 direcao = (player.position - transform.position);

        if (direcao.sqrMagnitude <= 0.001f)
        {
            Parar();
            return;
        }

        direcao.Normalize();

        // Acelera até a velocidade máxima
        velocidadeAtual = Mathf.MoveTowards(
            velocidadeAtual,
            velocidade,
            aceleracao * Time.deltaTime
        );

        Vector2 movimento = direcao * velocidadeAtual * Time.deltaTime;

        // Movimento 2D
        if (rb != null)
        {
            rb.MovePosition(rb.position + movimento);
        }
        else
        {
            transform.position += (Vector3)movimento;
        }

        VirarParaPlayer();
    }

    // =============================================
    // PARAR
    // =============================================

    private void Parar()
    {
        velocidadeAtual = Mathf.MoveTowards(
            velocidadeAtual,
            0f,
            aceleracao * Time.deltaTime
        );

        if (rb != null && !perseguindo)
            rb.linearVelocity = Vector2.zero;
    }

    // =============================================
    // VIRAR
    // =============================================

    private void VirarParaPlayer()
    {
        if (player == null)
            return;

        float direcaoX = player.position.x - transform.position.x;

        if (Mathf.Abs(direcaoX) < 0.01f)
            return;

        Vector3 escala = transform.localScale;

        if (direcaoX > 0f)
            escala.x = Mathf.Abs(escala.x);
        else
            escala.x = -Mathf.Abs(escala.x);

        transform.localScale = escala;
    }

    // =============================================
    // ATAQUE
    // =============================================

    private void Atacar()
    {
        if (player == null)
            return;

        if (Time.time < proximoDano)
            return;

        proximoDano = Time.time + cooldownDano;

        IDamageable alvo = player.GetComponent<IDamageable>();

        if (alvo != null)
        {
            alvo.TakeDamage(dano, gameObject);

            if (logs)
            {
                Debug.Log(
                    $"[Piranha] MORDEU O PLAYER | Dano: {dano}"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[Piranha] Player não possui IDamageable."
            );
        }
    }

    // =============================================
    // RECEBER DANO
    // =============================================

    public void TakeDamage(int quantidade)
    {
        if (quantidade <= 0)
            return;

        vida -= quantidade;

        if (logs)
        {
            Debug.Log(
                $"[Piranha] Recebeu {quantidade} de dano. " +
                $"Vida: {vida}"
            );
        }

        if (vida <= 0)
        {
            Morrer();
        }
    }

    // =============================================
    // MORTE
    // =============================================

    private void Morrer()
    {
        if (logs)
            Debug.Log("[Piranha] Morreu.");

        Destroy(gameObject);
    }

    // =============================================
    // GIZMOS
    // =============================================

    private void OnDrawGizmosSelected()
    {
        if (!gizmos)
            return;

        // Raio de detecção
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            raioDeteccao
        );

        // Distância de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            distanciaAtaque
        );
    }
}