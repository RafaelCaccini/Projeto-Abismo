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
        if (other.CompareTag("Enemy"))
        {
            Walker enemy = other.GetComponent<Walker>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}