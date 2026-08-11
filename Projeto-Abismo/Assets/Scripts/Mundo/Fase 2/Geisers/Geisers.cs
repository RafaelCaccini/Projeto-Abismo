using System.Collections;
using UnityEngine;

// Geiser: periodicamente ativa uma área de fogo horizontal que pode causar dano ao jogador.
public class Geisers : MonoBehaviour
{
    [Header("Temporização")]
    [Tooltip("Tempo que o fogo permanece ligado (segundos)")]
    [SerializeField] private float tempoLigado = 1f;
    [Tooltip("Tempo que o fogo permanece desligado entre ativações (segundos)")]
    [SerializeField] private float tempoDesligado = 2f;

    [Header("Tamanho e Posição do Fogo")]
    [Tooltip("Tamanho (largura x altura) da área do fogo em unidades locais")]
    [SerializeField] private Vector2 flameSize = new Vector2(2f, 1f);
    [Tooltip("Deslocamento local em X da área do fogo (horizontal)")]
    [SerializeField] private float flameOffsetX = 0f;

    [Header("Visual do Fogo")]
    [Tooltip("Sprite utilizado para representar o fogo (opcional)")]
    [SerializeField] private Sprite spriteDoFogo;
    [Tooltip("Escala do visual do fogo (multiplicador no eixo X e Y)")]
    [SerializeField] private Vector2 escalaVisualDoFogo = Vector2.one;
    [Tooltip("Cor aplicada ao sprite do fogo")]
    [SerializeField] private Color corDoFogo = Color.white;

    [Header("Dano")]
    [Tooltip("Quantidade de dano que o fogo causa ao jogador")]
    [SerializeField] private int damageAmount = 1;
    [Tooltip("Tag usada pelo jogador (padrão: Player)")]
    [SerializeField] private string playerTag = "Player";

    [Header("Comportamento")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool debugLogs = false;

    // runtime
    private GameObject flameObj;
    private BoxCollider2D flameCollider;
    private SpriteRenderer flameSprite; // optional visual
    private Coroutine cycleCoroutine;

    private void Awake()
    {
        CreateFlameObject();
        if (debugLogs) Debug.Log($"[Geisers] Awake - flame object created (offsetX={flameOffsetX}, size={flameSize}) on '{gameObject.name}'");
    }

    private void Start()
    {
        if (startOnAwake)
            StartCycle();
        if (debugLogs) Debug.Log($"[Geisers] Start - startOnAwake={startOnAwake}");
    }

    private void CreateFlameObject()
    {
        // Create child object used as hitbox/visual for flame
        flameObj = new GameObject("Geiser_Flame");
        flameObj.transform.SetParent(transform, false);
        flameObj.transform.localPosition = new Vector3(flameOffsetX, 0f, 0f);


        flameCollider = flameObj.AddComponent<BoxCollider2D>();
        flameCollider.isTrigger = true;
        flameCollider.size = flameSize;

        // SpriteRenderer para visual do fogo (opcional). Pode ser configurado pelo Inspector.
        flameSprite = flameObj.AddComponent<SpriteRenderer>();
        flameSprite.enabled = false;
        flameSprite.sprite = spriteDoFogo;
        flameSprite.color = corDoFogo;
        flameSprite.sortingOrder = 100; // manter na frente
        // aplica escala visual configurável
        flameObj.transform.localScale = new Vector3(escalaVisualDoFogo.x, escalaVisualDoFogo.y, 1f);

        // relay component to forward trigger to this Geisers instance
        var relay = flameObj.AddComponent<FlameRelay>();
        relay.Initialize(this);

        // start disabled: the whole flame object stays inactive until the cycle turns it on
        flameObj.SetActive(false);
        flameCollider.enabled = false;
        if (debugLogs) Debug.Log($"[Geisers] CreateFlameObject - child '{flameObj.name}' created (sprite={(spriteDoFogo!=null?spriteDoFogo.name:"none")})");
    }

    public void StartCycle()
    {
        if (cycleCoroutine != null) return;
        cycleCoroutine = StartCoroutine(CycleRoutine());
        if (debugLogs) Debug.Log("[Geisers] StartCycle called - coroutine started");
    }

    public void StopCycle()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
        SetFlameActive(false);
        if (debugLogs) Debug.Log("[Geisers] StopCycle called - coroutine stopped and flame deactivated");
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(tempoDesligado);
            SetFlameActive(true);
            yield return new WaitForSeconds(tempoLigado);
            SetFlameActive(false);
        }
    }

    private void SetFlameActive(bool on)
    {
        if (flameObj != null)
            flameObj.SetActive(on);

        // ensure components match configured values when activated
        if (flameCollider != null)
            flameCollider.enabled = on;
        if (flameSprite != null)
            flameSprite.enabled = on;
        if (debugLogs) Debug.Log($"[Geisers] SetFlameActive -> {(on?"ON":"OFF")} (obj={(flameObj!=null?flameObj.name:"null")})");
    }

    // called from relay when player enters while flame active
    internal void OnFlameHit(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            if (debugLogs) Debug.Log($"[Geisers] OnFlameHit - collider '{other.name}' does not match playerTag '{playerTag}'");
            return;
        }

        var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(damageAmount, gameObject);
            if (debugLogs) Debug.Log($"[Geisers] Damaged player for {damageAmount} (collider={other.name})");
        }
    }

    private void OnValidate()
    {
        // keep sizes >= 0
        flameSize.x = Mathf.Max(0f, flameSize.x);
        flameSize.y = Mathf.Max(0f, flameSize.y);
        tempoLigado = Mathf.Clamp(tempoLigado, 0f, Mathf.Infinity);
        tempoDesligado = Mathf.Max(tempoLigado, 0.01f);
        escalaVisualDoFogo.x = Mathf.Max(0.01f, escalaVisualDoFogo.x);
        escalaVisualDoFogo.y = Mathf.Max(0.01f, escalaVisualDoFogo.y);

        // if editing in inspector, update runtime objects if they exist
        if (flameObj != null)
        {
            flameObj.transform.localPosition = new Vector3(flameOffsetX, 0f, 0f);
            flameObj.transform.localScale = new Vector3(escalaVisualDoFogo.x, escalaVisualDoFogo.y, 1f);
        }
        if (flameCollider != null)
        {
            flameCollider.size = flameSize;
        }
        if (flameSprite != null)
        {
            flameSprite.sprite = spriteDoFogo;
            flameSprite.color = corDoFogo;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Vector3 center = transform.position + new Vector3(flameOffsetX, 0f, 0f);
        Gizmos.DrawWireCube(center, new Vector3(flameSize.x, flameSize.y, 0.1f));
    }

    // Relay component used to forward trigger events to Geisers
    private class FlameRelay : MonoBehaviour
    {
        private Geisers owner;
        public void Initialize(Geisers g) { owner = g; }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (owner == null) return;
            owner.OnFlameHit(other);
        }
    }
}
