using System.Collections;
using UnityEngine;

public class InimigoExplosivo : MonoBehaviour, IDamageable
{
    // =============================================
    // REFERÊNCIAS
    // =============================================

    [Header("Referências")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    // =============================================
    // DETECÇÃO
    // =============================================

    [Header("Detecção")]
    [SerializeField] private float raioDeteccao = 8f;
    [SerializeField] private float raioExplosao = 3f;
    [SerializeField] private string tagPlayer = "Player";

    // =============================================
    // MOVIMENTO
    // =============================================

    [Header("Movimento")]
    [SerializeField] private float velocidadePerseguicao = 3f;
    [SerializeField] private float distanciaParaExplodir = 0.8f;

    // =============================================
    // EXPLOSÃO
    // =============================================

    [Header("Explosão")]
    [SerializeField] private int danoExplosao = 2;
    [SerializeField] private float tempoAteExplodir = 1.5f;
    [SerializeField] private GameObject efeitoExplosaoPrefab;
    [SerializeField] private AudioClip somExplosao;
    [SerializeField] private AudioClip somDeteccao;

    // =============================================
    // VIDA
    // =============================================

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 2;
    [SerializeField] private float duracaoAnimacaoMorte = 0.5f;

    // =============================================
    // FEEDBACK VISUAL
    // =============================================

    [Header("Feedback Visual")]
    [SerializeField] private Color corNormal = Color.white;
    [SerializeField] private Color corPerigo = Color.red;
    [SerializeField] private float velocidadePiscada = 8f;

    // =============================================
    // DEBUG
    // =============================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool mostrarGizmos = true;

    // =============================================
    // ESTADO INTERNO
    // =============================================

    private enum Estado { Patrulhando, Perseguindo, Explodindo, Morto }
    private Estado estadoAtual = Estado.Patrulhando;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Transform player;
    private SpriteRenderer sr;

    private int vidaAtual;
    private bool explodindo = false;
    private bool morto = false;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        if (visual != null)
            sr = visual.GetComponent<SpriteRenderer>();
        else
            sr = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        vidaAtual = vidaMaxima;

        BuscarPlayer();
    }

    // =============================================
    // START
    // =============================================

    private void Start()
    {
        // Configura Rigidbody2D
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (morto) return;

        if (player == null)
            BuscarPlayer();

        switch (estadoAtual)
        {
            case Estado.Patrulhando: HandlePatrulha(); break;
            case Estado.Perseguindo: HandlePerseguicao(); break;
            case Estado.Explodindo: break;
        }

        AtualizarAnimacoes();
    }

    // =============================================
    // FIXED UPDATE
    // =============================================

    private void FixedUpdate()
    {
        if (morto || explodindo) return;

        if (estadoAtual == Estado.Perseguindo)
            MoverParaPlayer();
    }

    // =============================================
    // PATRULHA
    // =============================================

    private void HandlePatrulha()
    {
        if (player == null) return;

        float distancia = Vector2.Distance(transform.position, player.position);

        if (distancia <= raioDeteccao)
        {
            estadoAtual = Estado.Perseguindo;

            if (somDeteccao != null && audioSource != null)
                audioSource.PlayOneShot(somDeteccao);

            if (debugLogs)
                Debug.Log("[InimigoExplosivo] Player detectado — perseguindo!");
        }
    }

    // =============================================
    // PERSEGUIÇÃO
    // =============================================

    private void HandlePerseguicao()
    {
        if (player == null) return;

        float distancia = Vector2.Distance(transform.position, player.position);

        // Saiu do range — volta a patrulhar
        if (distancia > raioDeteccao * 1.5f)
        {
            estadoAtual = Estado.Patrulhando;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (debugLogs)
                Debug.Log("[InimigoExplosivo] Player fugiu — voltando a patrulhar.");

            return;
        }

        // Perto o suficiente para explodir
        if (distancia <= distanciaParaExplodir && !explodindo)
            StartCoroutine(SequenciaExplosao());
    }

    // =============================================
    // MOVIMENTO
    // =============================================

    private void MoverParaPlayer()
    {
        if (player == null) return;

        Vector2 direcao = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = new Vector2(direcao.x * velocidadePerseguicao, rb.linearVelocity.y);

        // Flip do visual
        if (visual != null)
        {
            Vector3 escala = visual.localScale;
            escala.x = Mathf.Abs(escala.x) * (direcao.x >= 0f ? 1f : -1f);
            visual.localScale = escala;
        }
    }

    // =============================================
    // SEQUÊNCIA DE EXPLOSÃO
    // =============================================

    private IEnumerator SequenciaExplosao()
    {
        explodindo = true;
        estadoAtual = Estado.Explodindo;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (debugLogs)
            Debug.Log("[InimigoExplosivo] Iniciando sequência de explosão!");

        if (animator != null)
            animator.SetTrigger("Explodindo");

        // Pisca em vermelho até explodir
        StartCoroutine(PiscarPerigo());

        yield return new WaitForSeconds(tempoAteExplodir);

        Explodir();
    }

    // =============================================
    // EXPLOSÃO
    // =============================================

    private void Explodir()
    {
        if (debugLogs)
            Debug.Log("[InimigoExplosivo] BOOM!");

        // Efeito visual
        if (efeitoExplosaoPrefab != null)
            Instantiate(efeitoExplosaoPrefab, transform.position, Quaternion.identity);

        // Som
        if (somExplosao != null)
            AudioSource.PlayClipAtPoint(somExplosao, transform.position);

        // Dano em área
        AplicarDanoArea();

        // Desativa collider para não dar dano extra
        if (col != null)
            col.enabled = false;

        Destroy(gameObject);
    }

    // =============================================
    // DANO EM ÁREA
    // =============================================

    private void AplicarDanoArea()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, raioExplosao);

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!hit.CompareTag(tagPlayer)) continue;

            IDamageable alvo = hit.GetComponent<IDamageable>();
            if (alvo == null)
                alvo = hit.GetComponentInParent<IDamageable>();

            if (alvo != null)
            {
                alvo.TakeDamage(danoExplosao, gameObject);

                if (debugLogs)
                    Debug.Log($"[InimigoExplosivo] Dano de {danoExplosao} aplicado em {hit.name}");
            }
        }
    }

    // =============================================
    // PISCADA DE PERIGO
    // =============================================

    private IEnumerator PiscarPerigo()
    {
        if (sr == null) yield break;

        float timer = 0f;

        while (explodindo && !morto)
        {
            timer += Time.deltaTime * velocidadePiscada;
            float t = (Mathf.Sin(timer) + 1f) / 2f;
            sr.color = Color.Lerp(corNormal, corPerigo, t);
            yield return null;
        }

        if (sr != null)
            sr.color = corNormal;
    }

    // =============================================
    // ANIMAÇÕES
    // =============================================

    private void AtualizarAnimacoes()
    {
        if (animator == null) return;

        bool correndo = estadoAtual == Estado.Perseguindo && !explodindo;
        animator.SetBool("IsRun", correndo);
    }

    // =============================================
    // VIDA / DANO
    // =============================================

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto) return;

        vidaAtual -= dano;

        if (debugLogs)
            Debug.Log($"[InimigoExplosivo] Tomou {dano} de dano | Vida: {vidaAtual}/{vidaMaxima}");

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;
            StartCoroutine(MorrerSemExplodir());
            return;
        }

        // Feedback de dano
        if (animator != null)
            animator.SetTrigger("Hit");
    }

    // =============================================
    // MORTE SEM EXPLOSÃO (levou dano suficiente)
    // =============================================

    private IEnumerator MorrerSemExplodir()
    {
        morto = true;
        estadoAtual = Estado.Morto;

        StopAllCoroutines();
        StartCoroutine(MorrerSemExplodir_Internal());
        yield break;
    }

    private IEnumerator MorrerSemExplodir_Internal()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        if (sr != null)
            sr.color = corNormal;

        if (animator != null)
            animator.SetTrigger("Morrer");

        if (debugLogs)
            Debug.Log("[InimigoExplosivo] Morreu sem explodir.");

        yield return new WaitForSeconds(duracaoAnimacaoMorte);

        Destroy(gameObject);
    }

    // =============================================
    // BUSCAR PLAYER
    // =============================================

    private void BuscarPlayer()
    {
        var go = GameObject.FindWithTag(tagPlayer);
        if (go != null)
            player = go.transform;
    }

    // =============================================
    // GIZMOS
    // =============================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos) return;

        // Raio de detecção
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioDeteccao);

        // Raio de explosão
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioExplosao);

        // Distância para explodir
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, distanciaParaExplodir);
    }
}