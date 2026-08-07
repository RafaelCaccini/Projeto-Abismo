using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HeadSlideOnTop : MonoBehaviour
{
    // =====================================
    // CONFIGURAÇÃO
    // =====================================

    [Header("Head Slide")]
    [Tooltip("Velocidade horizontal imediata aplicada ao player quando pisa no topo")]
    [SerializeField] private float slideSpeed = 6f;

    [Tooltip("Impulso horizontal adicional (ForceMode2D.Impulse) aplicado ao player")]
    [SerializeField] private float slideImpulse = 3f;

    [Tooltip("Pequeno ajuste vertical para garantir separação (0 = sem ajuste)")]
    [SerializeField] private float verticalNudge = 0.0f;

    [Tooltip("Tolerância em unidades para considerar contato no topo do collider")]
    [SerializeField] private float topTolerance = 0.02f;

    [Tooltip("Tag do alvo (normalmente 'Player')")]
    [SerializeField] private string targetTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // =====================================
    // COMPONENTES
    // =====================================

    private Collider2D col;

    // =====================================
    // START
    // =====================================

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (col == null)
        {
            Debug.LogError("[HeadSlideOnTop] Collider2D não encontrado no GameObject: " + name);
            enabled = false;
            return;
        }
    }

    // =====================================
    // DETECÇÃO DE TOPO (DISPARA AO ENTRAR EM COLISÃO)
    // =====================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // filtra por tag
        if (!collision.gameObject.CompareTag(targetTag))
            return;

        if (col == null)
            return;

        // calcula Y do topo do collider do inimigo
        float topY = col.bounds.max.y;

        bool playerOnTop = false;

        // percorre pontos de contato e exige que o ponto esteja no topo (com tolerância)
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.point.y >= topY - topTolerance && contact.normal.y > 0.1f)
            {
                playerOnTop = true;
                break;
            }
        }

        if (!playerOnTop)
            return;

        // obtém Rigidbody2D do player (suporta hierarquias)
        Rigidbody2D playerRb = collision.gameObject.GetComponentInParent<Rigidbody2D>();
        if (playerRb == null)
            playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (playerRb == null)
            return;

        // direção para empurrar: para o lado mais próximo (longe do centro do inimigo)
        float dir = collision.transform.position.x >= transform.position.x ? 1f : -1f;
        if (dir == 0f) dir = 1f;

        // aplica velocidade horizontal imediata (mantendo componente vertical)
        Vector2 currentVel = playerRb.linearVelocity;
        float newVy = currentVel.y;

        // aplica nudge vertical opcional (só aumenta para evitar "grounded" estável)
        if (verticalNudge != 0f && newVy <= verticalNudge)
            newVy = verticalNudge;

        playerRb.linearVelocity = new Vector2(dir * slideSpeed, newVy);

        // aplica impulso adicional para reforçar separação/escorregamento
        if (slideImpulse != 0f)
            playerRb.AddForce(new Vector2(dir * slideImpulse, 0f), ForceMode2D.Impulse);

        if (debugLogs)
        {
            Debug.Log($"[HeadSlideOnTop] {collision.gameObject.name} pisou no topo de {name}. Forçando slide: dir={(dir > 0 ? "direita" : "esquerda")}, speed={slideSpeed}, impulse={slideImpulse}");
        }
    }
}