using System.Collections;
using UnityEngine;

public class OndaBoss2 : MonoBehaviour, IDamageable
{
    public enum TipoOnda
    {
        [InspectorName("Azul (desvia pulando)")] Comum,
        [InspectorName("Roxa (quebra com dash)")] Roxa
    }

    // =============================================
    // CONFIGURAÇÃO DO PREFAB
    // =============================================

    [Header("Tipo desta onda (configure em cada prefab)")]
    [SerializeField] private TipoOnda tipoOnda = TipoOnda.Comum;

    [Header("Dano")]
    [Tooltip("Dano se o player NÃO pular a onda azul.")]
    [SerializeField] private int danoOndaAzul = 2;

    [Tooltip("Dano se o player NÃO der dash na onda roxa.")]
    [SerializeField] private int danoOndaRoxa = 4;

    [Header("Direção")]
    [Tooltip("Desmarcado: vai reto até onde o player estava ao nascer. Marcado: vai curvando atrás do player.")]
    [SerializeField] private bool seguirJogador = false;

    [Tooltip("Quão rápido a onda curva quando 'Seguir Jogador' está marcado (graus por segundo).")]
    [SerializeField] private float giroGrausPorSegundo = 90f;

    [Header("Tempo de vida")]
    [SerializeField] private float tempoMaximoVida = 9f;
    [SerializeField] private float distanciaMaxima = 40f;

    [Header("Visual")]
    [Tooltip("Aplica cor azul/roxa automaticamente. Desmarque se o sprite do prefab já tiver a cor certa.")]
    [SerializeField] private bool aplicarCorPadrao = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // =============================================
    // ESTADO
    // =============================================

    private Vector2 direcao = Vector2.left;
    private float velocidade;
    private Transform player;

    private bool inicializado;
    private bool jaCausouDano;
    private bool destruindo;
    private float tempoVivo;
    private float distanciaPercorrida;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

    private int DanoAtual => tipoOnda == TipoOnda.Roxa ? danoOndaRoxa : danoOndaAzul;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        if (col != null)
            col.isTrigger = true;

        if (aplicarCorPadrao && sr != null)
        {
            sr.color = tipoOnda == TipoOnda.Roxa
                ? new Color(0.7f, 0.2f, 0.9f, 0.95f)
                : new Color(0.2f, 0.7f, 1f, 0.7f);
        }
    }

    // =============================================
    // INICIALIZAR
    // O BossFase3 continua chamando assim. O 'destino', o 'dano' e o 'tipo'
    // são ignorados: valem o tipo e o dano configurados neste prefab.
    // Só a velocidade vem do boss.
    // =============================================

    public void Inicializar(Vector3 destino, float velocidade, int dano, TipoOnda tipo = TipoOnda.Comum)
    {
        this.velocidade = velocidade;
        this.tipoOnda = tipo; // APLICA o tipo passado pelo boss!

        // Sobrescreve o dano configurado no prefab com o valor passado pelo boss
        if (tipo == TipoOnda.Roxa)
            danoOndaRoxa = dano;
        else
            danoOndaAzul = dano;

        // ONDA SEMPRE VAI PURAMENTE HORIZONTAL (esquerda) - trava no Y do spawn
        direcao = Vector2.left;

        inicializado = true;

        if (debugLogs)
            Debug.Log($"[OndaBoss2] Onda {tipoOnda} | Dano: {DanoAtual} | Direção: HORIZONTAL ESQUERDA (Y travado: {transform.position.y:F2})", this);
    }

    // Compatibilidade com código antigo
    public void Inicializar(Vector3 destino, float velocidade, int dano)
        => Inicializar(destino, velocidade, dano, TipoOnda.Comum);

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (!inicializado || destruindo) return;

        if (seguirJogador && player != null)
        {
            Vector3 alvo = (Vector3)((Vector2)player.position - (Vector2)transform.position);

            Vector3 nova = Vector3.RotateTowards(
                (Vector3)direcao,
                alvo.normalized,
                giroGrausPorSegundo * Mathf.Deg2Rad * Time.deltaTime,
                0f
            );

            direcao = ((Vector2)nova).normalized;
        }

        Vector3 passo = (Vector3)(direcao * velocidade * Time.deltaTime);
        transform.position += passo;

        distanciaPercorrida += passo.magnitude;
        tempoVivo += Time.deltaTime;

        if (distanciaPercorrida >= distanciaMaxima)
        {
            Destruir("distância máxima");
            return;
        }

        if (tempoVivo >= tempoMaximoVida)
            Destruir("tempo máximo de vida");
    }

    // =============================================
    // COLISÃO COM O PLAYER
    // =============================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugLogs && other.CompareTag("Player")) Debug.Log($"[OndaBoss2] OnTriggerEnter - Tag: {other.tag}, Tipo: {tipoOnda}");
        Processar(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Processar(other);
    }

    private void Processar(Collider2D other)
    {
        if (destruindo) return;
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        bool playerEstaDandoDash = player.IsDashing;

        // 1º: DASH SEMPRE tem prioridade - quebra onda roxa, sem dano
        if (tipoOnda == TipoOnda.Roxa && playerEstaDandoDash)
        {
            if (debugLogs) Debug.Log($"[OndaBoss2] Dash na onda roxa — onda quebrada! IsDashing={playerEstaDandoDash}", this);
            DestruirComEfeito();
            return;
        }

        // 2º: Se já causou dano nessa onda, não aplica de novo
        if (jaCausouDano) return;

        // 3º: Aplica dano (azul sempre, roxa sem dash)
        jaCausouDano = true;
        if (debugLogs) Debug.Log($"[OndaBoss2] APLICANDO DANO: {DanoAtual} | Tipo: {tipoOnda} | Player: {player.name}", this);
        player.TakeDamage(DanoAtual, gameObject);

        if (debugLogs)
            Debug.Log($"[OndaBoss2] Onda {tipoOnda} causou {DanoAtual} de dano. Dash={playerEstaDandoDash}", this);
    }

    // =============================================
    // DESTRUIÇÃO
    // =============================================

    private void Destruir(string motivo)
    {
        if (debugLogs)
            Debug.Log($"[OndaBoss2] Onda {tipoOnda} destruída por: {motivo}", this);

        Destroy(gameObject);
    }

    private void DestruirComEfeito()
    {
        destruindo = true;

        if (col != null) col.enabled = false;

        if (sr != null) StartCoroutine(FadeOut());
        else Destroy(gameObject);
    }

    private IEnumerator FadeOut()
    {
        float t = 0f, duracao = 0.15f;
        Color c = sr.color;
        Vector3 escalaInicial = transform.localScale;

        while (t < duracao)
        {
            t += Time.deltaTime;
            float k = t / duracao;
            sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, k));
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, k);
            yield return null;
        }

        Destroy(gameObject);
    }

    // Ondas são imunes a ataques do player
    public void TakeDamage(int dano, GameObject fonte) { }

    // =============================================
    // PROPRIEDADES PÚBLICAS
    // =============================================

    public TipoOnda Tipo => tipoOnda;
    public bool EhOndaRoxa => tipoOnda == TipoOnda.Roxa;
    public bool EhOndaComum => tipoOnda == TipoOnda.Comum;
    public int Dano => DanoAtual;
    public float Velocidade => velocidade;
}