using UnityEngine;

/// <summary>
/// Dano do Fogo do Boss Fase 2
/// Funciona com BoxCollider2D (Is Trigger = true)
/// Coloque no mesmo GameObject do fogo ou no prefab do fogo
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DanoFogoBoss2 : MonoBehaviour
{
    [Header("CONFIGURAÇÃO DE DANO")]
    [Tooltip("Dano causado por segundo de contato")]
    [SerializeField] private int danoPorSegundo = 2;

    [Tooltip("Intervalo entre aplicações de dano (segundos)")]
    [SerializeField] private float intervaloDano = 0.5f;

    [Tooltip("Layer do jogador")]
    [SerializeField] private LayerMask layerJogador;

    [Tooltip("Tag do jogador (fallback se layer não funcionar)")]
    [SerializeField] private string tagJogador = "Player";

    [Header("VISUAL / FEEDBACK")]
    [Tooltip("Mostra gizmo da área de dano no editor")]
    [SerializeField] private bool mostrarGizmo = true;

    [Tooltip("Cor do gizmo")]
    [SerializeField] private Color corGizmo = new Color(1f, 0.3f, 0f, 0.3f);

    [Header("AUTO-DESTRUIÇÃO (OPCIONAL)")]
    [Tooltip("Destruir automaticamente após X segundos (0 = não destrói)")]
    [SerializeField] private float tempoVida = 0f;

    // Interno
    private BoxCollider2D boxCollider;
    private float proximoDano;
    private bool podeDarDano = true;

    // Referência ao PlayerController (cache)
    private PlayerController jogadorCtrl;

    private void Awake()
    {
        // Garante que o collider é trigger
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        // Configuração inicial
        proximoDano = 0f;

        if (tempoVida > 0f)
        {
            Destroy(gameObject, tempoVida);
        }
    }

    private void Start()
    {
        // Tenta achar o PlayerController na cena para cache
        var jogador = GameObject.FindGameObjectWithTag(tagJogador);
        if (jogador) jogadorCtrl = jogador.GetComponent<PlayerController>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!podeDarDano) return;
        if (!EhJogador(other)) return;

        // Verifica intervalo de dano
        if (Time.time < proximoDano) return;

        // Aplica dano
        AplicarDano(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Dano imediato na entrada (opcional - comente se não quiser)
        if (!podeDarDano) return;
        if (!EhJogador(other)) return;

        AplicarDano(other);
    }

    private bool EhJogador(Collider2D other)
    {
        // Verifica pela LayerMask
        if (((1 << other.gameObject.layer) & layerJogador) != 0) return true;

        // Fallback: verifica pela Tag
        if (other.CompareTag(tagJogador)) return true;

        // Verifica no parent (caso o collider esteja num filho do player)
        if (other.transform.parent && other.transform.parent.CompareTag(tagJogador)) return true;

        return false;
    }

    private void AplicarDano(Collider2D other)
    {
        // Pega PlayerController (suporta hierarquia)
        PlayerController pc = null;

        // Tenta no próprio objeto
        pc = other.GetComponent<PlayerController>();

        // Tenta no parent
        if (!pc) pc = other.GetComponentInParent<PlayerController>();

        // Tenta no cache
        if (!pc && jogadorCtrl) pc = jogadorCtrl;

        if (pc != null)
        {
            pc.TakeDamage(danoPorSegundo, gameObject);
            proximoDano = Time.time + intervaloDano;

            // Debug log
            #if UNITY_EDITOR
            Debug.Log($"[DanoFogoBoss2] Dano aplicado: {danoPorSegundo} | Próximo em: {intervaloDano}s");
            #endif
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"[DanoFogoBoss2] PlayerController não encontrado no objeto: {other.name}");
            #endif
        }
    }

    /// <summary>
    /// Habilita/desabilita o dano (útil para fade in/out)
    /// </summary>
    public void SetDanoAtivo(bool ativo)
    {
        podeDarDano = ativo;
        if (boxCollider) boxCollider.enabled = ativo;
    }

    /// <summary>
    /// Configura dano em runtime
    /// </summary>
    public void Configurar(int dano, float intervalo, LayerMask layer)
    {
        danoPorSegundo = dano;
        intervaloDano = intervalo;
        layerJogador = layer;
    }

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmo || !boxCollider) return;

        Gizmos.color = corGizmo;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
        Gizmos.matrix = Matrix4x4.identity;

        // Desenha bounds mundial também
        Gizmos.color = new Color(corGizmo.r, corGizmo.g, corGizmo.b, 0.5f);
        Gizmos.DrawWireCube(transform.TransformPoint(boxCollider.offset),
            Vector3.Scale(boxCollider.size, transform.lossyScale));
    }

    private void OnValidate()
    {
        // Garante que o collider é trigger no editor também
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider && !boxCollider.isTrigger) boxCollider.isTrigger = true;

        // Validações
        danoPorSegundo = Mathf.Max(0, danoPorSegundo);
        intervaloDano = Mathf.Max(0.05f, intervaloDano);
        tempoVida = Mathf.Max(0f, tempoVida);
    }
}