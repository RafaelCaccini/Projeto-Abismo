using System.Collections;
using UnityEngine;

public class Blocker : MonoBehaviour, IDamageable
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("REFERÊNCIAS")]
    [SerializeField] private Transform pontoFogo;

    [SerializeField] private GameObject projetilPrefab;

    [SerializeField] private Transform player;

    [SerializeField] private Animator animator;

    // =====================================
    // ATAQUE
    // =====================================

    [Header("ATAQUE")]
    [SerializeField] private float attackCooldown = 2f;

    [SerializeField] private float projectileSpeed = 8f;

    [SerializeField] private float arcForce = 8f;

    [SerializeField] private float attackRange = 10f;

    // =====================================
    // TROCA ATAQUE
    // =====================================

    [Header("TROCA DE ATAQUE")]
    [SerializeField] private float tempoTrocaAtaque = 3f;

    // =====================================
    // VIDA
    // =====================================

    [Header("VIDA")]
    [SerializeField] private int maxLife = 3;

    // =====================================
    // LUZ
    // =====================================

    [Header("LUZ")]
    [SerializeField] private bool precisaDeLuz = true;

    // =====================================
    // DEBUG
    // =====================================

    [Header("DEBUG")]
    [SerializeField] private bool debugLogs = true;

    // =====================================
    // CONTROLE
    // =====================================

    private float timer;

    private float timerTroca;

    private int currentLife;

    private bool isDead;

    private bool luzAtiva;

    private TipoAtaque ataqueAtual;

    // =====================================
    // ENUM
    // =====================================

    enum TipoAtaque
    {
        Reto,
        Arco
    }

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        GarantirPontoFogo();
    }

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        currentLife = maxLife;

        FindPlayer();

        EscolherNovoAtaque();

        AtualizarAnimator();
    }

    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        if (isDead)
            return;

        if (pontoFogo == null)
        {
            Debug.LogError(
                "[Blocker] PontoFogo sumiu"
            );

            return;
        }

        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        AtualizarLuz();

        AtualizarAnimator();

        float distance =
            Vector2.Distance(
                transform.position,
                player.position
            );

        bool podeAtacar =
            distance <= attackRange &&
            (!precisaDeLuz || luzAtiva);

        if (podeAtacar)
        {
            AttackLoop();
        }

        // TROCA DE ATAQUE

        timerTroca -= Time.deltaTime;

        if (timerTroca <= 0f)
        {
            EscolherNovoAtaque();
        }
    }

    // =====================================
    // PONTO FOGO
    // =====================================

    void GarantirPontoFogo()
    {
        Transform found =
            transform.Find("PontoFogo");

        if (found != null)
        {
            pontoFogo = found;
        }
        else
        {
            Debug.LogError(
                "[Blocker] CRIA um filho 'PontoFogo'"
            );

            enabled = false;
        }
    }

    // =====================================
    // PLAYER
    // =====================================

    void FindPlayer()
    {
        GameObject p =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (p != null)
        {
            player = p.transform;
        }
    }

    // =====================================
    // LUZ
    // =====================================

    void AtualizarLuz()
    {
        luzAtiva = false;

        if (player == null)
            return;

        PlayerController pc =
            player.GetComponent<PlayerController>();

        if (pc == null)
            return;

        luzAtiva = pc.LuzAtiva;
    }

    // =====================================
    // ANIMATOR
    // =====================================

    void AtualizarAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool(
            "LightOn",
            luzAtiva
        );
    }

    // =====================================
    // ESCOLHER ATAQUE
    // =====================================

    void EscolherNovoAtaque()
    {
        ataqueAtual =
            (Random.value > 0.5f)
            ? TipoAtaque.Reto
            : TipoAtaque.Arco;

        timerTroca =
            tempoTrocaAtaque;

        if (debugLogs)
        {
            Debug.Log(
                "[Blocker] Ataque atual: "
                + ataqueAtual
            );
        }
    }

    // =====================================
    // LOOP ATAQUE
    // =====================================

    void AttackLoop()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Attack();

            timer = attackCooldown;
        }
    }

    // =====================================
    // ATAQUE
    // =====================================

    void Attack()
    {
        if (
            projetilPrefab == null ||
            pontoFogo == null ||
            player == null
        )
            return;

        if (animator != null)
        {
            animator.SetTrigger(
                "Attack"
            );
        }

        if (ataqueAtual == TipoAtaque.Arco)
        {
            ShootArc();
        }
        else
        {
            ShootStraight();
        }
    }

    // =====================================
    // TIRO RETO
    // =====================================

    void ShootStraight()
    {
        Vector2 dir =
            (
                player.position -
                pontoFogo.position
            ).normalized;

        GameObject proj =
            Instantiate(
                projetilPrefab,
                pontoFogo.position,
                Quaternion.identity
            );

        ProjetilBlocker p =
            proj.GetComponent<ProjetilBlocker>();

        if (p != null)
        {
            p.Launch(
                dir,
                projectileSpeed,
                0f
            );
        }
    }

    // =====================================
    // TIRO ARCO
    // =====================================

    void ShootArc()
    {
        Vector2 dir =
            (
                player.position -
                pontoFogo.position
            ).normalized;

        Vector2 arcDir =
            new Vector2(
                dir.x,
                dir.y + 0.5f
            ).normalized;

        GameObject proj =
            Instantiate(
                projetilPrefab,
                pontoFogo.position,
                Quaternion.identity
            );

        ProjetilBlocker p =
            proj.GetComponent<ProjetilBlocker>();

        if (p != null)
        {
            p.Launch(
                arcDir,
                arcForce,
                0.8f
            );
        }
    }

    // =====================================
    // TOMAR DANO
    // =====================================

    public void TakeDamage(
        int amount,
        GameObject source
    )
    {
        if (isDead || amount <= 0)
            return;

        if (player == null)
            return;

        PlayerController pc =
            player.GetComponent<PlayerController>();

        if (
            precisaDeLuz &&
            (
                pc == null ||
                !pc.LuzAtiva
            )
        )
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[Blocker] ignorou dano (sem luz)"
                );
            }

            return;
        }

        currentLife -= amount;

        Debug.Log(
            "[" + name + "] tomou "
            + amount +
            " | HP: "
            + currentLife
        );

        if (currentLife <= 0)
        {
            Die();
        }
    }

    // =====================================
    // MORTE
    // =====================================

    void Die()
    {
        if (isDead)
            return;

        StartCoroutine(
            DeathRoutine()
        );
    }

    IEnumerator DeathRoutine()
    {
        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger(
                "Death"
            );
        }

        foreach (
            Collider2D c
            in GetComponents<Collider2D>()
        )
        {
            c.enabled = false;
        }

        yield return new WaitForSeconds(
            1.2f
        );

        Destroy(gameObject);
    }
}