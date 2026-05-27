using UnityEngine;

public class AttackBlock : MonoBehaviour
{
    public int damage = 1;

    void Start()
    {
        Destroy(gameObject, 0.2f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // =====================================
        // BLOCKER
        // =====================================

        var blocker = other.GetComponent<Blocker>();

        if (blocker != null)
        {
            blocker.TakeDamage(
                damage,
                gameObject
            );

            return;
        }

        // =====================================
        // WALKER
        // =====================================

        var walker = other.GetComponent<Walker>();

        if (walker != null)
        {
            walker.TakeDamage(
                damage,
                gameObject
            );

            return;
        }

        // =====================================
        // BLOCKER NO PAI
        // =====================================

        var parentBlocker =
            other.GetComponentInParent<Blocker>();

        if (parentBlocker != null)
        {
            parentBlocker.TakeDamage(
                damage,
                gameObject
            );

            return;
        }

        // =====================================
        // WALKER NO PAI
        // =====================================

        var parentWalker =
            other.GetComponentInParent<Walker>();

        if (parentWalker != null)
        {
            parentWalker.TakeDamage(
                damage,
                gameObject
            );

            return;
        }
    }
}