using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    // =====================================
    // CONFIGURAÇÕES
    // =====================================

    [Header("Configurações")]
    [SerializeField] private bool usarAnimacao = false;

    // =====================================
    // CONTROLE
    // =====================================

    private Animator anim;
    private Collider2D col;
    private bool ativado = false;

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        // Garante que o collider seja um trigger
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning(
                "[Checkpoint] " + gameObject.name +
                " - Collider2D.isTrigger foi ativado automaticamente."
            );
        }

        if (usarAnimacao)
        {
            anim = GetComponent<Animator>();
        }
    }

    // =====================================
    // TRIGGER
    // =====================================

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (ativado)
            return;

        if (!other.CompareTag("Player"))
            return;

        Ativar();
    }

    // =====================================
    // ATIVAÇÃO
    // =====================================

    void Ativar()
    {
        ativado = true;

        // Informa ao GameManager qual é o checkpoint ativo
        if (GameManager.Instance != null)
        {
            GameManager.Instance
                .SetCheckpoint(transform.position);
        }
        else
        {
            Debug.LogError(
                "[Checkpoint] GameManager.Instance é NULL."
            );
        }

        // Feedback visual (animação)
        if (usarAnimacao && anim != null)
        {
            anim.SetBool("Ativado", true);
        }

        // Desativa o trigger para evitar reativação
        // (jogador que respawnar dentro não reativa)
        col.enabled = false;

        Debug.Log(
            "[Checkpoint] " +
            gameObject.name +
            " ativado."
        );
    }

    // =====================================
    // API
    // =====================================

    public bool Ativado =>
        ativado;

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmos()
    {
        Gizmos.color = ativado
            ? Color.green
            : Color.yellow;

        Gizmos.DrawSphere(
            transform.position,
            0.3f
        );
    }
}