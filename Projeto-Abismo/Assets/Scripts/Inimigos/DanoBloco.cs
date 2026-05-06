using UnityEngine;

public class DamageBlock : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.5f;

    private float lastHitTime;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    void TryDamage(GameObject obj)
    {
        if (Time.time < lastHitTime + hitCooldown)
            return;

        PlayerController player = obj.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            player.TakeDamage(damage, gameObject);
            lastHitTime = Time.time;
        }
    }
}