using UnityEngine;

public class BossProjetil : MonoBehaviour
{
    public int dano = 1;

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (other.CompareTag("Player"))
        {
            IDamageable alvo =
                other.GetComponent<IDamageable>();

            if (alvo != null)
            {
                alvo.TakeDamage(
                    dano,
                    gameObject
                );
            }

            Destroy(gameObject);
        }

        if (
            !other.CompareTag("Boss") &&
            !other.CompareTag("Spike")
        )
        {
            Destroy(gameObject);
        }
    }
}