
using UnityEngine;

public class OndaBoss2 : MonoBehaviour, IDamageable
{
    // =============================================
    // ESTADO
    // =============================================

    private Vector3 destino;
    private float velocidade;
    private int dano;
    private bool inicializado = false;

    private Rigidbody2D rb;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
    }

    // =============================================
    // INICIALIZAR
    // =============================================

    public void Inicializar(
        Vector3 destino,
        float velocidade,
        int dano)
    {
        this.destino = destino;
        this.velocidade = velocidade;
        this.dano = dano;
        inicializado = true;

        // Calcula a direção até o destino.
        Vector2 direcao =
            ((Vector2)destino - (Vector2)transform.position).normalized;

        // Inicia o movimento da onda.
        if (rb != null)
        {
            rb.linearVelocity = direcao * velocidade;
        }

        // Orienta o sprite na direção do movimento.
        float angulo =
            Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.AngleAxis(angulo, Vector3.forward);
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (!inicializado)
            return;

        // Verifica se a onda chegou perto do destino.
        float distancia = Vector2.Distance(
            transform.position,
            destino
        );

        if (distancia <= 0.3f)
        {
            // Para a onda antes de destruí-la.
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            Destroy(gameObject);
        }
    }

    // =============================================
    // COLISÃO COM O JOGADOR
    // =============================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        // A onda só causa dano ao objeto com a tag Player.
        if (!other.CompareTag("Player"))
            return;

        // Procura o componente de dano no próprio objeto.
        IDamageable alvo = other.GetComponent<IDamageable>();

        // Se não encontrar, procura nos objetos pais.
        if (alvo == null)
            alvo = other.GetComponentInParent<IDamageable>();

        // Aplica o dano ao jogador.
        if (alvo != null)
            alvo.TakeDamage(dano, gameObject);
    }

    // =============================================
    // IMUNIDADE A ATAQUES
    // =============================================

    public void TakeDamage(int dano, GameObject fonte)
    {
        // A onda não recebe dano e não é destruída por ataques.
        // Ela só desaparece ao chegar ao destino.
        return;
    }
}
