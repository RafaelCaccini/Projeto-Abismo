using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class AreaDeteccaoInseto : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private float raioDeteccao = 4f;

    private CircleCollider2D circle;

    private InsetoLuz inseto;

    private void Awake()
    {
        circle = GetComponent<CircleCollider2D>();

        inseto = GetComponentInParent<InsetoLuz>();

        // Garante trigger
        circle.isTrigger = true;

        // Atualiza raio
        circle.radius = raioDeteccao;
    }

    private void OnValidate()
    {
        circle = GetComponent<CircleCollider2D>();

        if (circle != null)
        {
            circle.isTrigger = true;
            circle.radius = raioDeteccao;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        inseto.PlayerEntrouRange();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        inseto.PlayerSaiuRange();
    }
}