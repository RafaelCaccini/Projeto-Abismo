using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackDuration = 0.12f;

    private Collider2D damageCollider;
    private HashSet<int> hitIDs = new HashSet<int>();
    private bool isAttacking;

    void Awake()
    {
        Transform t = transform.Find("Dano");

        if (t != null)
        {
            damageCollider = t.GetComponent<Collider2D>();
            damageCollider.enabled = false;

            var hitbox = t.GetComponent<DamageHitbox>();
            if (hitbox == null)
                hitbox = t.gameObject.AddComponent<DamageHitbox>();

            hitbox.Owner = this;
        }
        else
        {
            Debug.LogError("SEM HITBOX 'Dano' NO PLAYER.");
        }
    }

    [SerializeField] private Transform attackPointRight;
    [SerializeField] private Transform attackPointLeft;

    public void PerformAttack(bool facingRight, Vector2 offset)
    {
        if (isAttacking) return;

        Transform point = facingRight ? attackPointRight : attackPointLeft;

        damageCollider.transform.position = point.position;

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        hitIDs.Clear();

        damageCollider.enabled = true;

        yield return new WaitForSeconds(attackDuration);

        damageCollider.enabled = false;
        isAttacking = false;
    }

    public void RegisterHit(Collider2D other)
    {
        if (!isAttacking) return;

        int id = other.GetInstanceID();
        if (hitIDs.Contains(id)) return;

        hitIDs.Add(id);

        // 🔥 AQUI TÁ A MUDANÇA IMPORTANTE
        IDamageable dmg = other.GetComponent<IDamageable>();

        if (dmg == null)
            dmg = other.GetComponentInParent<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(damage, gameObject);
        }
    }
}