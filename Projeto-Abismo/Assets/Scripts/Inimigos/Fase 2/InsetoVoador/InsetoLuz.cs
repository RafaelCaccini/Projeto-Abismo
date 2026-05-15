using UnityEngine;

public class InsetoLuz : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;

    [SerializeField] private Lampiao lampiao;

    private Rigidbody2D rb;

    [Header("Movimento")]
    [SerializeField] private float velocidade = 3f;

    [SerializeField] private float distanciaParar = 0.5f;

    [Header("Dano")]
    [SerializeField] private int dano = 1;

    [SerializeField] private float cooldownDano = 1f;

    private bool playerNoRange = false;

    private bool podeDarDano = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();

            if (pc != null)
                player = pc.transform;
        }

        if (lampiao == null)
        {
            lampiao = FindFirstObjectByType<Lampiao>();
        }
    }

    private void FixedUpdate()
    {
        if (player == null || lampiao == null)
            return;

        // Só segue se:
        // player estiver no range
        // E lampião ligado
        if (playerNoRange && lampiao.IsLightOn)
        {
            SeguirPlayer();
        }
    }

    void SeguirPlayer()
    {
        Vector2 direcao =
            (player.position - transform.position).normalized;

        float distancia =
            Vector2.Distance(transform.position, player.position);

        if (distancia > distanciaParar)
        {
            Vector2 novaPosicao =
                rb.position + direcao * velocidade * Time.fixedDeltaTime;

            rb.MovePosition(novaPosicao);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

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

        Debug.Log("🪲 Inseto atacou player");

        StartCoroutine(CooldownDano());
    }

    System.Collections.IEnumerator CooldownDano()
    {
        podeDarDano = false;

        yield return new WaitForSeconds(cooldownDano);

        podeDarDano = true;
    }

    // RANGE
    public void PlayerEntrouRange()
    {
        playerNoRange = true;
    }

    public void PlayerSaiuRange()
    {
        playerNoRange = false;
    }
}