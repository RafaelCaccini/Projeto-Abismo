using UnityEngine;

public class CeilingSpike : MonoBehaviour
{
    public float damage = 2f;
    public float activeDuration = 1.8f;

    private void OnEnable()
    {
        Invoke(nameof(ReturnToPool), activeDuration);
    }

    private void ReturnToPool()
    {
        SpikeManager.Instance?.ReturnToPool(gameObject, false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.TakeDamage((int)damage, gameObject);
        }
    }
}