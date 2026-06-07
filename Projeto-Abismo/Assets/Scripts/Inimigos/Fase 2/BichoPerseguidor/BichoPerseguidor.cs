using UnityEngine;
using System.Collections;

public class InsetoCacador : MonoBehaviour, IDamageable
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
    [SerializeField] private float velocidadeNormal = 3f;

    [SerializeField] private float velocidadeLonge = 12f;

    [SerializeField] private float distanciaVelocidadeAlta = 15f;

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
    // CONTROLE
    // =====================================

    private bool podeDarDano = true;

    private bool morto = false;

    private bool playerPerto = false;

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
    // UPDATE
    // =====================================

    private void Update()
    {
        if (morto)
            return;

        VirarParaPlayer();
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

        if (lampiao == null)
            return;

        if (!lampiao.IsLightOn)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }

        SeguirPlayer();
    }

    // =====================================
    // SEGUIR PLAYER
    // =====================================

    private void SeguirPlayer()
    {
        float distancia =
            Vector2.Distance(
                transform.position,
                player.position
            );

        if (distancia <= distanciaParar)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }

        float velocidadeAtual;

        if (playerPerto)
        {
            velocidadeAtual =
                velocidadeNormal;
        }
        else
        {
            velocidadeAtual =
                distancia >
                distanciaVelocidadeAlta
                ? velocidadeLonge
                : velocidadeNormal;
        }

        Vector2 direcao =
            (
                player.position -
                transform.position
            ).normalized;

        rb.linearVelocity =
            direcao *
            velocidadeAtual;
    }

    // =====================================
    // RANGE PLAYER
    // =====================================

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (
            other.CompareTag("Player")
        )
        {
            playerPerto = true;
        }
    }

    private void OnTriggerExit2D(
        Collider2D other
    )
    {
        if (
            other.CompareTag("Player")
        )
        {
            playerPerto = false;
        }
    }

    // =====================================
    // FLIP
    // =====================================

    private void VirarParaPlayer()
    {
        if (player == null)
            return;

        Vector3 scale =
            transform.localScale;

        if (
            player.position.x >
            transform.position.x
        )
        {
            scale.x =
                Mathf.Abs(scale.x);
        }
        else
        {
            scale.x =
                -Mathf.Abs(scale.x);
        }

        transform.localScale =
            scale;
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

        StartCoroutine(
            CooldownDano()
        );
    }

    private IEnumerator CooldownDano()
    {
        podeDarDano = false;

        yield return new WaitForSeconds(
            cooldownDano
        );

        podeDarDano = true;
    }

    // =====================================
    // VIDA
    // =====================================

    public void TakeDamage(
        int amount,
        GameObject source
    )
    {
        if (morto)
            return;

        vida -= amount;

        if (vida <= 0)
        {
            Morrer();
        }
    }

    // =====================================
    // MORTE
    // =====================================

    private void Morrer()
    {
        morto = true;

        rb.linearVelocity =
            Vector2.zero;

        Collider2D[] colliders =
            GetComponents<Collider2D>();

        foreach (
            Collider2D c
            in colliders
        )
        {
            c.enabled = false;
        }

        Destroy(gameObject, 0.1f);
    }
}