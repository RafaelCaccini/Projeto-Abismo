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
        IDamageable dmg;

        // Tenta obter a interface no próprio objeto; se não existir, tenta nos pais
        if (!alvo.TryGetComponent<IDamageable>(out dmg))
            dmg = alvo.GetComponentInParent<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(dano, gameObject);
            Debug.Log($"[Spike] deu {dano} de dano em {alvo.name}");
        }
    }
}