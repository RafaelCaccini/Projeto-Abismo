using UnityEngine;

// =========================================================
// INIMIGO VOADOR
// =========================================================
public class InimigoVoador : MonoBehaviour, IDamageable
{
    // =============================================
    // DISPARO
    // =============================================

    [Header("Disparo")]
    [SerializeField] private GameObject prefabProjetil;
    [SerializeField] private Transform pontoDisparo;
    [SerializeField] private float intervaloAtaque = 2f;
    [SerializeField] private float velocidadeProjetil = 10f;
    [SerializeField] private int danoProjetil = 1;

    // =============================================
    // DETECÇÃO
    // =============================================

    [Header("Detecção")]
    [SerializeField] private float alcanceVisao = 12f;

    // =============================================
    // VIDA
    // =============================================

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 1;
    [SerializeField] private int danoPorContato = 1;

    // =============================================
    // DEBUG
    // =============================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // =============================================
    // ESTADO INTERNO
    // =============================================

    private int vidaAtual;
    private Transform playerTransform;
    private float cronometroAtaque;
    private bool morto = false;

    // =============================================
    // START
    // =============================================

    private void Start()
    {
        vidaAtual = vidaMaxima;

        // Busca o player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"[{name}] Player não encontrado! Verifique a tag 'Player'.");
        }

        // Busca ponto de disparo automaticamente se não atribuído
        if (pontoDisparo == null)
        {
            Transform filho = transform.Find("PontoDisparo");
            pontoDisparo = filho != null ? filho : transform;

            if (debugLogs)
                Debug.Log($"[{name}] PontoDisparo auto-atribuído: {pontoDisparo.name}");
        }

        // Valida prefab
        if (prefabProjetil == null)
            Debug.LogError($"[{name}] ⚠️ Prefab Projetil NÃO atribuído no Inspector!");
        else if (debugLogs)
            Debug.Log($"[{name}] ✅ Iniciado | Vida: {vidaAtual} | Alcance: {alcanceVisao}");
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (morto) return;
        if (playerTransform == null) return;
        if (prefabProjetil == null) return;

        float distancia = Vector2.Distance(transform.position, playerTransform.position);

        if (distancia <= alcanceVisao)
        {
            cronometroAtaque += Time.deltaTime;

            if (cronometroAtaque >= intervaloAtaque)
            {
                Atirar();
                cronometroAtaque = 0f;
            }
        }
        else
        {
            // Reseta o timer quando o player sai do alcance
            cronometroAtaque = 0f;
        }
    }

    // =============================================
    // ATIRAR
    // =============================================

    private void Atirar()
    {
        if (prefabProjetil == null || playerTransform == null) return;

        Vector3 origem = pontoDisparo != null ? pontoDisparo.position : transform.position;
        Vector2 direcao = ((Vector2)playerTransform.position - (Vector2)origem).normalized;

        // Rotaciona o projetil para apontar na direção certa
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        Quaternion rotacao = Quaternion.AngleAxis(angulo, Vector3.forward);

        GameObject projetilObj = Instantiate(prefabProjetil, origem, rotacao);

        // Tenta pegar o script do prefab
        ProjetilInimigoVoador tiro = projetilObj.GetComponent<ProjetilInimigoVoador>();

        if (tiro != null)
        {
            tiro.Inicializar(direcao, velocidadeProjetil, danoProjetil);
        }
        else
        {
            // Fallback: usa Rigidbody2D se não tiver o script
            Rigidbody2D rb = projetilObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direcao * velocidadeProjetil;
            }
            else
            {
                Debug.LogError($"[{name}] ⚠️ Prefab do projetil não tem ProjetilInimigoVoador nem Rigidbody2D!");
            }
        }

        if (debugLogs)
            Debug.Log($"[{name}] 🔥 Atirou em direção ao player | Dir: {direcao}");
    }

    // =============================================
    // IDamageable — recebe dano de qualquer fonte
    // =============================================

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto) return;

        vidaAtual -= dano;

        if (debugLogs)
            Debug.Log($"[{name}] 💥 Tomou {dano} | Vida: {vidaAtual}/{vidaMaxima} | Fonte: {fonte.name}");

        if (vidaAtual <= 0)
            Morrer();
    }

    // =============================================
    // CONTATO COM PLAYER
    // =============================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (morto) return;
        if (!collision.CompareTag("Player")) return;

        IDamageable player = collision.GetComponent<IDamageable>();
        if (player == null)
            player = collision.GetComponentInParent<IDamageable>();

        if (player != null)
            player.TakeDamage(danoPorContato, gameObject);
    }

    // =============================================
    // MORTE
    // =============================================

    private void Morrer()
    {
        morto = true;

        if (debugLogs)
            Debug.Log($"[{name}] ☠️ Morreu!");

        Destroy(gameObject);
    }

    // =============================================
    // GIZMOS
    // =============================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcanceVisao);

        if (pontoDisparo != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pontoDisparo.position, 0.15f);
        }
    }
}

// =========================================================
// PROJETIL DO INIMIGO VOADOR
// Coloque esse script no Prefab do projetil
// =========================================================
public class ProjetilInimigoVoador : MonoBehaviour
{
    // =============================================
    // ESTADO
    // =============================================

    private Vector2 direcao;
    private float velocidade;
    private int dano;
    private bool inicializado = false;

    [SerializeField] private float tempoDeVida = 5f;
    [SerializeField] private string tagChao = "Ground";

    // =============================================
    // INICIALIZAR
    // =============================================

    public void Inicializar(Vector2 direcao, float velocidade, int dano)
    {
        this.direcao = direcao;
        this.velocidade = velocidade;
        this.dano = dano;
        this.inicializado = true;

        // Rotaciona o sprite na direção do movimento
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angulo, Vector3.forward);

        // Destrói após tempo de vida
        Destroy(gameObject, tempoDeVida);
    }

    // =============================================
    // UPDATE — move via transform
    // =============================================

    private void Update()
    {
        if (!inicializado) return;

        transform.position += (Vector3)(direcao * velocidade * Time.deltaTime);
    }

    // =============================================
    // COLISÃO
    // =============================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignora o próprio inimigo
        if (collision.CompareTag("Enemy")) return;

        // Dano no player
        if (collision.CompareTag("Player"))
        {
            IDamageable player = collision.GetComponent<IDamageable>();
            if (player == null)
                player = collision.GetComponentInParent<IDamageable>();

            if (player != null)
                player.TakeDamage(dano, gameObject);

            Destroy(gameObject);
            return;
        }

        // Destrói ao bater no chão ou parede
        if (collision.CompareTag(tagChao) ||
            collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}