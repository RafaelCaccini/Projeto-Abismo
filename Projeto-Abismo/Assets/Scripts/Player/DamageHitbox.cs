using UnityEngine;

public class DamageHitbox : MonoBehaviour
{
    public PlayerAttack Owner;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Owner?.RegisterHit(other);
    }
}