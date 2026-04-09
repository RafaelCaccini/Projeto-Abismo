using UnityEngine;

public class AttackBlock : MonoBehaviour
{
    public int damage = 1;

    void Start()
    {
        Destroy(gameObject, 0.2f); // destrói rápido (efeito temporário)
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Aplica dano diretamente para componentes de inimigo conhecidos
        var blocker = other.GetComponent<Blocker>();
        if (blocker != null)
        {
            blocker.TakeDamage(damage, gameObject);
            return;
        }

        var walker = other.GetComponent<Walker>();
        if (walker != null)
        {
            walker.TakeDamage(damage);
            return;
        }

        // tenta no pai (caso o collider esteja em filho)
        var parentBlocker = other.GetComponentInParent<Blocker>();
        if (parentBlocker != null)
        {
            parentBlocker.TakeDamage(damage, gameObject);
            return;
        }

        var parentWalker = other.GetComponentInParent<Walker>();
        if (parentWalker != null)
        {
            parentWalker.TakeDamage(damage);
            return;
        }
    }
}