using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("dano")]
    public int dano = 1;

    private void OnTriggerEnter2D(Collider2D col)
    {
        TentarDarDano(col.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        TentarDarDano(col.gameObject);
    }

    void TentarDarDano(GameObject alvo)
    {
        IDamageable dmg = alvo.GetComponent<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(dano, gameObject);
            Debug.Log($"[Spike] deu {dano} de dano em {alvo.name}");
        }
    }
}