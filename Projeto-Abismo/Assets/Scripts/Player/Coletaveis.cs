using UnityEngine;

// Manager simples de coletáveis
public class Coletaveis : MonoBehaviour
{
    public static Coletaveis Instance { get; private set; }

    // estado público apenas leitura
    public int TotalCount { get; private set; }
    public int CollectedCount { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        // conta todos os Coletavel presentes na cena
        TotalCount = FindObjectsByType<Coletavel>(FindObjectsSortMode.None).Length;
        CollectedCount = 0;

        Debug.Log($"[Coletaveis] Total na cena: {TotalCount}");
    }

    // chamado por cada Coletavel quando coletado
    public void RegisterCollect()
    {
        CollectedCount++;
        Debug.Log($"[Coletaveis] Coletado: {CollectedCount}/{TotalCount}");

        if (TotalCount > 0 && CollectedCount >= TotalCount)
        {
            Debug.Log("[Coletaveis] Todos coletados!");
            // TODO: notificar UI / abrir portas / tocar som
        }
    }
}

// Componente simples para anexar ao pickup
[RequireComponent(typeof(Collider2D))]
public class Coletavel : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private bool debugLogs = true;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"[Coletavel] {name} precisa de Collider2D.");
            enabled = false;
            return;
        }

        if (!col.isTrigger)
        {
            col.isTrigger = true;
            if (debugLogs) Debug.LogWarning($"[Coletavel] {name}: Collider2D.isTrigger ativado automaticamente.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // debug: registre o que colidiu para diagnosticar problemas comuns
        if (debugLogs)
        {
            string attachedRb = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject.name : "null";
            Debug.Log($"[Coletavel] OnTriggerEnter2D other={other.name} tag={other.tag} attachedRigidbody={attachedRb}");
        }

        // aceita tag Player ou presença de PlayerController no pai/rigidbody
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc == null && other.attachedRigidbody != null)
            pc = other.attachedRigidbody.GetComponentInParent<PlayerController>();

        if (!other.CompareTag(playerTag) && pc == null)
        {
            if (debugLogs) Debug.Log($"[Coletavel] Ignorado: não é player (tag='{playerTag}') nem possui PlayerController.");
            return;
        }

        if (Coletaveis.Instance != null)
            Coletaveis.Instance.RegisterCollect();
        else if (debugLogs)
            Debug.LogWarning("[Coletavel] Manager Coletaveis.Instance não encontrado.");

        // efeito simples: destruir / desativar imediatamente
        if (destroyOnCollect) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}