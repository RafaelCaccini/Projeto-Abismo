using UnityEngine;
using System.Collections;

public class InsetoLuz : MonoBehaviour, IDamageable
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("Referências")]
    [SerializeField] private Transform player;

    [SerializeField] private Lampiao lampiao;

    private Rigidbody2D rb;

    // =====================================
    // MOVIMENTO
    // =====================================

    [Header("Movimento")]
    [SerializeField] private float velocidade = 3f;

    [SerializeField] private float distanciaParar = 0.5f;

    // =====================================
    // VIDA
    // =====================================

    [Header("Vida")]
    [SerializeField] private int vida = 3;

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

    private bool playerNoRange = false;

    private bool podeDarDano = true;

    private bool morto = false;

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            PlayerController pc =
                FindFirstObjectByType<PlayerController>();

            if (pc != null)
            {
                player = pc.transform;
            }
        }

        if (lampiao == null)
        {
            lampiao =
                FindFirstObjectByType<Lampiao>();
        }
    }

    // =====================================
    // FIXED UPDATE
    // =====================================

    private void FixedUpdate()
    {
        if (morto)
            return;

        if (player == null || lampiao == null)
            return;

        // Só segue:
        // player no range
        // lampião ligado

        if (
            playerNoRange &&
            lampiao.IsLightOn
        )
        {
            SeguirPlayer();
        }
    }

    // =====================================
    // SEGUIR PLAYER
    // =====================================

    void SeguirPlayer()
    {
        Vector2 direcao =
            (
                player.position -
                transform.position
            ).normalized;

        float distancia =
            Vector2.Distance(
                transform.position,
                player.position
            );

        if (distancia > distanciaParar)
        {
            Vector2 novaPosicao =
                rb.position +
                direcao *
                velocidade *
                Time.fixedDeltaTime;

            rb.MovePosition(novaPosicao);
        }
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
                "🪲 Inseto atacou player"
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
            Debug.Log(
                "☠️ Inseto morreu"
            );
        }

        rb.linearVelocity = Vector2.zero;

        foreach (
            Collider2D c
            in GetComponents<Collider2D>()
        )
        {
            c.enabled = false;
        }

        Destroy(gameObject, 0.1f);
    }

    // =====================================
    // RANGE
    // =====================================

    public void PlayerEntrouRange()
    {
        playerNoRange = true;
    }

    public void PlayerSaiuRange()
    {
        playerNoRange = false;
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            0.3f
        );
    }
}