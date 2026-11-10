using UnityEngine;

/// <summary>
/// Script para ondas do Boss Fase 3.
/// Suporta dois tipos:
/// - Comum: Menor, pode ser desviada com pulo
/// - Roxa: Maior, NÃO pode ser desviada com pulo - precisa de dash
/// </summary>
public class OndaBoss2 : MonoBehaviour, IDamageable
{
    // =============================================
    // TIPOS DE ONDA
    // =============================================

    public enum TipoOnda
    {
        Comum,  // Desviavel com pulo
        Roxa    // Precisa de dash
    }

    // =============================================
    // ESTADO
    // =============================================

    private Vector3 destino;
    private float velocidade;
    private int dano;
    private TipoOnda tipoOnda = TipoOnda.Comum;
    private bool inicializado = false;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

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
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (col != null)
            col.isTrigger = true;
    }

    // =============================================
    // INICIALIZAR
    // =============================================

    /// <summary>
    /// Inicializa a onda com destino, velocidade, dano e tipo
    /// </summary>
    public void Inicializar(Vector3 destino, float velocidade, int dano, TipoOnda tipo = TipoOnda.Comum)
    {
        this.destino = destino;
        this.velocidade = velocidade;
        this.dano = dano;
        this.tipoOnda = tipo;
        this.inicializado = true;

        // Calcula a direcao ate o destino (sempre da direita para esquerda)
        Vector2 direcao = ((Vector2)destino - (Vector2)transform.position).normalized;

        // Inicia o movimento da onda
        if (rb != null)
        {
            rb.linearVelocity = direcao * velocidade;
        }

        // Orienta o sprite na direcao do movimento
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angulo, Vector3.forward);

        // Configura visual baseado no tipo
        ConfigurarVisual(tipo);
    }

    // Sobrecarga para compatibilidade com codigo antigo
    public void Inicializar(Vector3 destino, float velocidade, int dano)
    {
        Inicializar(destino, velocidade, dano, TipoOnda.Comum);
    }

    private void ConfigurarVisual(TipoOnda tipo)
    {
        if (sr == null) return;

        if (tipo == TipoOnda.Roxa)
        {
            // Onda roxa: cor roxa/magenta, maior
            sr.color = new Color(0.7f, 0.2f, 0.9f, 0.95f);
            transform.localScale *= 1.4f;
        }
        else
        {
            // Onda comum: cor azul/ciano, menor, semi-transparente
            sr.color = new Color(0.2f, 0.7f, 1f, 0.7f);
        }
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (!inicializado) return;

        // Verifica se a onda chegou no limite esquerdo (apenas distância X, pois movimento é horizontal)
        float distanciaX = Mathf.Abs(transform.position.x - destino.x);

        if (distanciaX <= 0.5f)
        {
            // Para a onda antes de destrui-la
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            Destroy(gameObject);
        }
    }

    // =============================================
    // COLISAO COM O JOGADOR
    // =============================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        // A onda so causa dano ao objeto com a tag Player
        if (!other.CompareTag("Player"))
        {
            if (Debug.isDebugBuild)
                Debug.Log($"[OndaBoss2] Colisão com: {other.name} (tag: {other.tag}) - não é Player");
            return;
        }

        if (Debug.isDebugBuild)
            Debug.Log($"[OndaBoss2] Colisão com PLAYER detectada! Tipo: {tipoOnda}");

        // Procura o componente de dano no proprio objeto
        IDamageable alvo = other.GetComponent<IDamageable>();

        // Se nao encontrar, procura nos objetos pais
        if (alvo == null)
            alvo = other.GetComponentInParent<IDamageable>();

        // Aplica o dano ao jogador
        if (alvo != null)
        {
            // Para onda roxa, verifica se o player esta dando dash
            if (tipoOnda == TipoOnda.Roxa)
            {
                var playerCtrl = other.GetComponentInParent<PlayerController>();
                if (playerCtrl != null)
                {
                    bool isDashing = playerCtrl.IsDashing;
                    if (Debug.isDebugBuild)
                        Debug.Log($"[OndaBoss2] PlayerController encontrado. IsDashing: {isDashing}");

                    if (isDashing)
                    {
                        // Player esta dando dash - DESTROI a onda e passa sem dano!
                        if (Debug.isDebugBuild)
                            Debug.Log("[OndaBoss2] >>> DASH DETECTADO! Destruindo onda, sem dano! <<<");
                        DestruirOndaComEfeito();
                        return;
                    }
                }
                else
                {
                    if (Debug.isDebugBuild)
                        Debug.LogWarning("[OndaBoss2] PlayerController NÃO encontrado no player!");
                }
            }

            if (Debug.isDebugBuild)
                Debug.Log($"[OndaBoss2] Aplicando dano: {dano}");
            alvo.TakeDamage(dano, gameObject);
        }
        else
        {
            if (Debug.isDebugBuild)
                Debug.LogWarning("[OndaBoss2] IDamageable não encontrado no player!");
        }
    }

    // =============================================
    // DESTRUIR ONDA (quando player da dash)
    // =============================================

    private void DestruirOndaComEfeito()
    {
        // Para o movimento IMEDIATAMENTE
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Desativa collider IMEDIATAMENTE para não dar dano múltiplo
        if (col != null)
            col.enabled = false;

        // Efeito visual simples (piscar e sumir)
        if (sr != null)
        {
            StartCoroutine(FadeOutEDestruir());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator FadeOutEDestruir()
    {
        float tempo = 0f;
        float duracao = 0.15f;
        Color corOriginal = sr.color;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = tempo / duracao;
            sr.color = new Color(corOriginal.r, corOriginal.g, corOriginal.b, Mathf.Lerp(corOriginal.a, 0f, t));
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    // =============================================
    // IMUNIDADE A ATAQUES DO PLAYER
    // =============================================

    /// <summary>
    /// As ondas nao podem ser destruidas por ataques do player.
    /// Elas so desaparecem ao chegar ao destino (limite esquerdo).
    /// </summary>
    public void TakeDamage(int dano, GameObject fonte)
    {
        // Ondas sao imunes a dano do player
        return;
    }

    // =============================================
    // PROPRIEDADES PUBLICAS
    // =============================================

    public TipoOnda Tipo => tipoOnda;
    public bool EhOndaRoxa => tipoOnda == TipoOnda.Roxa;
    public bool EhOndaComum => tipoOnda == TipoOnda.Comum;
    public int Dano => dano;
    public float Velocidade => velocidade;
}