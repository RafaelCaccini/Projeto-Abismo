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

    [Header("Posição do centro (offset local)")]
    [SerializeField] private float flameOffsetX = 0f;
    [SerializeField] private float flameOffsetY = 0f;

    [Header("Range")]
    [Tooltip("Raio em que o geiser 'vê' o player (apenas alerta)")]
    [SerializeField] private float visionRadius = 3f;
    [Tooltip("Raio em que o geiser causa dano quando ativo (<= visionRadius)")]
    [SerializeField] private float damageRadius = 1.2f;

    [Header("Dano")]
    [SerializeField] private int damageAmount = 1;
    [Tooltip("Intervalo entre ticks de dano enquanto o jogador permanece na área (segundos)")]
    [SerializeField] private float damageInterval = 0.6f;
    [SerializeField] private string playerTag = "Player";

    [Header("Visual (opcional)")]
    [Tooltip("Sprite mostrado quando o geiser está ativo")]
    [SerializeField] private Sprite spriteDoFogo;
    [Tooltip("Escala do sprite visual")]
    [SerializeField] private Vector2 escalaVisualDoFogo = Vector2.one;
    [Tooltip("Cor do sprite visual")]
    [SerializeField] private Color corDoFogo = Color.white;
    [Tooltip("Sorting order do sprite visual")]
    [SerializeField] private int sortingOrder = 100;

    [Header("Comportamento / Debug")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool debugLogs = false;

    private Coroutine cycleCoroutine;
    private Coroutine damageCoroutine;

    // estado do player relativo às ranges (usado para log apenas ao mudar)
    private bool prevInVision = false;
    private bool prevInDamage = false;

    private PlayerController player;

    // visual runtime
    private GameObject flameVisualObj;
    private SpriteRenderer flameSpriteRenderer;

    private void Awake()
    {
        // cache do player (tentativa inicial)
        player = GameObject.FindGameObjectWithTag(playerTag)?.GetComponent<PlayerController>();

        // cria objeto visual (se houver sprite) — visual separado da lógica de dano
        CreateOrUpdateVisual();
    }

    private void Start()
    {
        if (startOnAwake) StartCycle();
        if (debugLogs) Debug.Log("[Geisers] Start - ciclo inicializado");
    }

    public void StartCycle()
    {
        if (cycleCoroutine != null) return;
        cycleCoroutine = StartCoroutine(CycleRoutine());
        if (debugLogs) Debug.Log("[Geisers] StartCycle chamado");
    }

    public void StopCycle()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
        StopDamageRoutine();
        SetVisualActive(false);
        if (debugLogs) Debug.Log("[Geisers] StopCycle chamado");
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(tempoDesligado);
            Activate(true);
            yield return new WaitForSeconds(tempoLigado);
            Activate(false);
        }
    }

    private void Activate(bool on)
    {
        if (debugLogs) Debug.Log($"[Geisers] {(on ? "ATIVADO" : "DESATIVADO")}");
        SetVisualActive(on);

        if (on)
        {
            // checa presença imediata e inicia ticks
            if (damageCoroutine == null)
                damageCoroutine = StartCoroutine(DamageRoutine());
            // exibe status inicial
            EvaluateAndLogState(forceLog: true);
        }
        else
        {
            StopDamageRoutine();
            prevInVision = prevInDamage = false;
        }
    }

    private void StopDamageRoutine()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    // rotina principal: aplica dano em ticks se player estiver dentro o damageRadius
    private IEnumerator DamageRoutine()
    {
        while (true)
        {
            EvaluateAndLogState();
            if (player != null)
            {
                Vector2 center = (Vector2)transform.position + new Vector2(flameOffsetX, flameOffsetY);
                float dist = Vector2.Distance(center, player.transform.position);

                bool inDamage = dist <= damageRadius;
                if (inDamage)
                {
                    // aplica dano via PlayerController para garantir que o player receba corretamente
                    player.TakeDamage(damageAmount, gameObject);
                    if (debugLogs) Debug.Log($"[Geisers] DANO aplicado ({damageAmount}) a {player.name}");
                }
            }
            yield return new WaitForSeconds(damageInterval);
        }
    }

    // verifica estados de visão/dano e registra mudanças no console
    private void EvaluateAndLogState(bool forceLog = false)
    {
        if (player == null)
        {
            // tenta reapontar
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) player = go.GetComponent<PlayerController>();
            if (player == null)
            {
                if (debugLogs) Debug.LogWarning("[Geisers] Player não encontrado para avaliação de range");
                return;
            }
        }

        Vector2 center = (Vector2)transform.position + new Vector2(flameOffsetX, flameOffsetY);
        float dist = Vector2.Distance(center, player.transform.position);

        bool inVision = dist <= visionRadius;
        bool inDamage = dist <= damageRadius;

        if (forceLog || inVision != prevInVision)
        {
            if (inVision)
                Debug.Log("[Geisers] Player entrou na visão");
            else
                Debug.Log("[Geisers] Player saiu da visão");
        }

        if (forceLog || inDamage != prevInDamage)
        {
            if (inDamage)
                Debug.Log("[Geisers] Player entrou na RANGE DE DANO");
            else
                Debug.Log("[Geisers] Player saiu da RANGE DE DANO");
        }

        prevInVision = inVision;
        prevInDamage = inDamage;
    }

    private void CreateOrUpdateVisual()
    {
        // remove visual antigo se houver
        if (flameVisualObj != null)
        {
            DestroyImmediate(flameVisualObj);
            flameVisualObj = null;
            flameSpriteRenderer = null;
        }

        if (spriteDoFogo == null)
            return;

        flameVisualObj = new GameObject("Geiser_Visual");
        flameVisualObj.transform.SetParent(transform, false);
        flameVisualObj.transform.localPosition = new Vector3(flameOffsetX, flameOffsetY, 0f);

        flameSpriteRenderer = flameVisualObj.AddComponent<SpriteRenderer>();
        flameSpriteRenderer.sprite = spriteDoFogo;
        flameSpriteRenderer.color = corDoFogo;
        flameSpriteRenderer.sortingOrder = sortingOrder;
        flameVisualObj.transform.localScale = new Vector3(escalaVisualDoFogo.x, escalaVisualDoFogo.y, 1f);

        flameVisualObj.SetActive(false);
    }

    private void SetVisualActive(bool on)
    {
        if (flameVisualObj != null)
            flameVisualObj.SetActive(on);
    }

    private void OnValidate()
    {
        // segurança para valores no inspector
        tempoLigado = Mathf.Max(0f, tempoLigado);
        tempoDesligado = Mathf.Max(0f, tempoDesligado);
        visionRadius = Mathf.Max(0f, visionRadius);
        damageRadius = Mathf.Clamp(damageRadius, 0f, visionRadius);
        damageInterval = Mathf.Max(0.05f, damageInterval);

        // atualizar visual em edição
        if (Application.isEditor)
        {
            // se sprite foi alterado, recria visual
            if (spriteDoFogo != null && flameVisualObj == null)
                CreateOrUpdateVisual();

            if (flameVisualObj != null)
            {
                flameVisualObj.transform.localPosition = new Vector3(flameOffsetX, flameOffsetY, 0f);
                flameVisualObj.transform.localScale = new Vector3(escalaVisualDoFogo.x, escalaVisualDoFogo.y, 1f);
            }

            if (flameSpriteRenderer != null)
            {
                flameSpriteRenderer.sprite = spriteDoFogo;
                flameSpriteRenderer.color = corDoFogo;
                flameSpriteRenderer.sortingOrder = sortingOrder;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // desenha centro e os dois raios
        Vector3 center = transform.position + new Vector3(flameOffsetX, flameOffsetY, 0f);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Gizmos.DrawWireSphere(center, visionRadius);

        Gizmos.color = new Color(1f, 0.2f, 0f, 0.6f);
        Gizmos.DrawWireSphere(center, damageRadius);
    }
}