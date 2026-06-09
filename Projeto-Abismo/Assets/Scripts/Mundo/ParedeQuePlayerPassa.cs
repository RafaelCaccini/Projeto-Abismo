using UnityEngine;

public class ParedeAntiEnemy : MonoBehaviour
{
    private Collider2D paredeCollider;

    private void Awake()
    {
        paredeCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Bounds parede = paredeCollider.bounds;
        Bounds inimigo = other.bounds;

        Vector3 pos = other.transform.position;

        // veio da esquerda
        if (inimigo.center.x < parede.center.x)
        {
            pos.x =
                parede.min.x -
                inimigo.extents.x -
                0.05f;
        }
        else
        {
            // veio da direita
            pos.x =
                parede.max.x +
                inimigo.extents.x +
                0.05f;
        }

        other.transform.position = pos;

        Rigidbody2D rb =
            other.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("BLOQUEEI: " + other.name);
    }
}