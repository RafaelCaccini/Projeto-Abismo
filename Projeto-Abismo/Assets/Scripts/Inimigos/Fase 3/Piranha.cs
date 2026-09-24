using System.Collections;
using UnityEngine;

public class PiranhaEnemy : MonoBehaviour, IDamageable
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
    // VISUAL (configurável pelo Inspector)
    // =============================================
    [Header("Visual")]
    [Tooltip("Arraste aqui o SpriteRenderer que representa o visual do inimigo (opcional).")]
    [SerializeField] private SpriteRenderer visualRenderer;

    // =============================================
    // REFERÊNCIAS
    // =============================================

    private Transform player;
    private Rigidbody2D rb;

    private float velocidadeAtual;
    private float proximoDano;

    private bool perseguindo;
    private bool facingRight = true; // controla direção visual

    // estado interno de morte
    private bool morto = false;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // usa o SpriteRenderer configurado no Inspector; se vazio busca automaticamente
        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<SpriteRenderer>();

        if (visualRenderer == null)
        {
            visualRenderer = GetComponent<SpriteRenderer>();
        }

        if (visualRenderer == null)
        {
            Debug.LogWarning("[Piranha] SpriteRenderer não encontrado! Verifique o GameObject ou arraste no Inspector.");
        }

        // garante que o objeto tenha tag "Enemy" (facilita interações como ParedeAntiEnemy / Lampião)
        if (!gameObject.CompareTag("Enemy"))
        {
            // só seta se não estiver marcado (evita sobrescrever configuração intencional)
            gameObject.tag = "Enemy";
        }
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (morto) return;

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

        // ATUALIZA FACING UMA VEZ SE A DIREÇÃO MUDA
        AtualizarFacing(direcao.x);
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
    // ATUALIZAR FACING (usando SpriteRenderer.flipX)
    // =============================================

    private void AtualizarFacing(float direcaoX)
    {
        if (visualRenderer == null)
            return;

        // Apenas atualiza se a direção mudou significativamente
        if (direcaoX > 0.1f && !facingRight)
        {
            facingRight = true;
            visualRenderer.flipX = false;
        }
        else if (direcaoX < -0.1f && facingRight)
        {
            facingRight = false;
            visualRenderer.flipX = true;
        }
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
    // RECEBER DANO - implementação correta de IDamageable
    // =============================================
    public void TakeDamage(int amount, GameObject source)
    {
        if (morto) return;
        if (amount <= 0) return;

        vida -= amount;

        if (logs)
        {
            Debug.Log($"[Piranha] Recebeu {amount} de dano de {source?.name ?? "desconhecido"}. Vida: {vida}");
        }

        if (vida <= 0)
        {
            StartCoroutine(DieRoutine());
        }
    }

    // Mantive sobrecarga compatível caso algo chame sem source
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, null);
    }

    // rotina de morte para limpar estado e evitar "sprite sumindo"
    private IEnumerator DieRoutine()
    {
        if (morto) yield break;
        morto = true;

        // interrompe rotinas de movimento/dano
        StopAllCoroutines();

        // pára física
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // desativa colliders (inclui filhos) para evitar interação posterior
        foreach (var c in GetComponentsInChildren<Collider2D>())
        {
            if (c != null) c.enabled = false;
        }

        // opcional: desativa renderer para garantir que não "some" por hierarquia mal posicionada
        if (visualRenderer != null)
            visualRenderer.enabled = false;

        // pequena espera para animação/efeitos (se houver)
        yield return new WaitForSeconds(0.05f);

        Destroy(gameObject);
    }

    // =============================================
    // MORTE
    // =============================================

    private void Morrer()
    {
        // compatibilidade: mantém método antigo (se algo chamar)
        StartCoroutine(DieRoutine());
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