using UnityEngine;

public class Blocker : MonoBehaviour, IDamageable
{
    [Header("Referências")]
    [SerializeField] private Transform pontoFogo;
    [SerializeField] private GameObject projetilPrefab;
    [SerializeField] private Transform player;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float arcForce = 8f;
    [SerializeField] private float attackRange = 10f;

    [Header("Troca de Ataque")]
    [SerializeField] private float tempoTrocaAtaque = 3f;

    [Header("Vida")]
    [SerializeField] private int maxLife = 3;

    [Header("Luz")]
    [SerializeField] private bool precisaDeLuz = true;

    private float timer;
    private float timerTroca;
    private int currentLife;
    private bool isDead;

    private TipoAtaque ataqueAtual;

    enum TipoAtaque
    {
        Reto,
        Arco
    }

    void Awake()
    {
        GarantirPontoFogo();
    }

    void Start()
    {
        currentLife = maxLife;
        FindPlayer();
        EscolherNovoAtaque();
    }

    void Update()
    {
        if (isDead) return;

        if (pontoFogo == null)
        {
            Debug.LogError("[Blocker] PontoFogo sumiu. Corrige no prefab.");
            return;
        }

        if (player == null)
            FindPlayer();

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
            AttackLoop();

        // troca de ataque por tempo
        timerTroca -= Time.deltaTime;

        if (timerTroca <= 0f)
            EscolherNovoAtaque();
    }

    void GarantirPontoFogo()
    {
        // 🔥 só pega, não cria, não move
        Transform found = transform.Find("PontoFogo");

        if (found != null)
        {
            pontoFogo = found;
        }
        else
        {
            Debug.LogError("[Blocker] CRIA um filho 'PontoFogo' no PREFAB");
            enabled = false;
        }
    }

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void EscolherNovoAtaque()
    {
        ataqueAtual = (Random.value > 0.5f) ? TipoAtaque.Reto : TipoAtaque.Arco;
        timerTroca = tempoTrocaAtaque;

        Debug.Log($"[Blocker] Ataque atual: {ataqueAtual}");
    }

    void AttackLoop()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Attack();
            timer = attackCooldown;
        }
    }

    void Attack()
    {
        if (projetilPrefab == null || pontoFogo == null || player == null)
            return;

        if (ataqueAtual == TipoAtaque.Arco)
            ShootArc();
        else
            ShootStraight();
    }

    void ShootStraight()
    {
        Vector2 dir = (player.position - pontoFogo.position).normalized;

        var proj = Instantiate(projetilPrefab, pontoFogo.position, Quaternion.identity);
        var p = proj.GetComponent<ProjetilBlocker>();

        if (p != null)
            p.Launch(dir, projectileSpeed, 0f);
    }

    void ShootArc()
    {
        Vector2 dir = (player.position - pontoFogo.position).normalized;
        Vector2 arcDir = new Vector2(dir.x, dir.y + 0.5f).normalized;

        var proj = Instantiate(projetilPrefab, pontoFogo.position, Quaternion.identity);
        var p = proj.GetComponent<ProjetilBlocker>();

        if (p != null)
            p.Launch(arcDir, arcForce, 0.8f);
    }

    public void TakeDamage(int amount, GameObject source)
    {
        if (isDead || amount <= 0) return;

        if (player == null) return;

        var pc = player.GetComponent<PlayerController>();

        if (precisaDeLuz && (pc == null || !pc.LuzAtiva))
        {
            Debug.Log("[Blocker] ignorou dano (sem luz)");
            return;
        }

        currentLife -= amount;

        Debug.Log($"[{name}] tomou {amount} | HP: {currentLife}");

        if (currentLife <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        foreach (var c in GetComponents<Collider2D>())
            c.enabled = false;

        Destroy(gameObject);
    }
}