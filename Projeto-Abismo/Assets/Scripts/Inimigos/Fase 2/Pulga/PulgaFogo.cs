using UnityEngine;
using System.Collections;

public class PulgaFogo : MonoBehaviour
{
    private Rigidbody2D rb;

    private Transform player;

    [Header("Movimento")]
    [SerializeField] private float forcaPuloX = 4f;

    [SerializeField] private float forcaPuloY = 7f;

    [SerializeField] private float tempoEntrePulos = 1f;

    [SerializeField] private bool iniciarViradoDireita = true;

    [Header("Detecção")]
    [SerializeField] private float rangeAtivacao = 8f;

    [Header("Dano")]
    [SerializeField] private int dano = 1;

    [SerializeField] private float cooldownDano = 1f;

    private bool olhandoDireita;

    private bool podeDarDano = true;

    private bool estaNoChao;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        PlayerController pc =
            FindFirstObjectByType<PlayerController>();

        if (pc != null)
            player = pc.transform;

        olhandoDireita = iniciarViradoDireita;

        AtualizarDirecaoVisual(
            olhandoDireita ? 1f : -1f
        );

        StartCoroutine(RotinaPulos());
    }

    IEnumerator RotinaPulos()
    {
        while (true)
        {
            yield return new WaitForSeconds(tempoEntrePulos);

            if (player == null)
                continue;

            float distancia =
                Vector2.Distance(
                    transform.position,
                    player.position
                );

            // Fora do range
            if (distancia > rangeAtivacao)
                continue;

            // Não está no chão
            if (!estaNoChao)
                continue;

            FazerPulo();
        }
    }

    void FazerPulo()
    {
        // Chance de trocar direção
        if (Random.value > 0.5f)
        {
            olhandoDireita = !olhandoDireita;
        }

        float direcao =
            olhandoDireita ? 1f : -1f;

        AtualizarDirecaoVisual(direcao);

        // Reseta velocidade vertical
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            0f
        );

        // Impulso
        rb.AddForce(
            new Vector2(
                direcao * forcaPuloX,
                forcaPuloY
            ),
            ForceMode2D.Impulse
        );

        Debug.Log("🔥 Pulga pulou");
    }

    void AtualizarDirecaoVisual(float direcao)
    {
        Vector3 escala = transform.localScale;

        if (direcao > 0)
            escala.x = Mathf.Abs(escala.x);
        else if (direcao < 0)
            escala.x = -Mathf.Abs(escala.x);

        transform.localScale = escala;
    }

    // DETECTA CHÃO
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            estaNoChao = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            estaNoChao = false;
        }
    }

    // DANO
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!podeDarDano)
            return;

        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerController pc =
            collision.gameObject.GetComponent<PlayerController>();

        if (pc == null)
            return;

        pc.TakeDamage(dano, gameObject);

        Debug.Log("🔥 Pulga atacou");

        StartCoroutine(CooldownDano());
    }

    IEnumerator CooldownDano()
    {
        podeDarDano = false;

        yield return new WaitForSeconds(cooldownDano);

        podeDarDano = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            rangeAtivacao
        );
    }
}