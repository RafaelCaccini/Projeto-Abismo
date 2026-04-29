using UnityEngine;

public class FloorSpike : MonoBehaviour
{
    public float damage = 2f;
    public float activeDuration = 1.8f;

    private void OnEnable()
    {
        Invoke(nameof(ReturnToPool), activeDuration);
    }

    private void OnDisable()
    {
        CancelInvoke(); // evita bug com pooling
    }

    private void ReturnToPool()
    {
        SpikeManager.Instance?.ReturnToPool(gameObject, true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.TakeDamage((int)damage, gameObject); // ✔️ CORRETO
        }
    }
}