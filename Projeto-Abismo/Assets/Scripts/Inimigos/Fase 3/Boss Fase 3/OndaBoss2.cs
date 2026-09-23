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

    public void Inicializar(Vector3 destino, float velocidade, int dano)
    {
        this.destino = destino;
        this.velocidade = velocidade;
        this.dano = dano;
        this.inicializado = true;

        // Aponta na direção do destino
        Vector2 dir = (destino - transform.position).normalized;

        if (rb != null)
            rb.linearVelocity = dir * velocidade;

        // Rotaciona o sprite na direção do movimento
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angulo, Vector3.forward);
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (!inicializado) return;

        // Destrói ao chegar no destino
        if (Vector2.Distance(transform.position, destino) < 0.3f)
            Destroy(gameObject);
    }

    // =============================================
    // COLISÃO
    // =============================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        IDamageable alvo = other.GetComponent<IDamageable>();
        if (alvo == null)
            alvo = other.GetComponentInParent<IDamageable>();

        if (alvo != null)
            alvo.TakeDamage(dano, gameObject);
    }

    // IDamageable — a onda pode ser destruída por ataques
    public void TakeDamage(int dano, GameObject fonte)
    {
        Destroy(gameObject);
    }
}