using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackDuration = 0.12f;

    [Header("Attack Points")]
    [SerializeField] private Transform attackPointRight;
    [SerializeField] private Transform attackPointLeft;

    [Header("Debug")]
    [SerializeField] private bool showHitbox = false;

    private Collider2D damageCollider;
    private SpriteRenderer sr;
    private HashSet<int> hitIDs = new HashSet<int>();
    private bool isAttacking;

    void Awake()
    {
        Transform t = transform.Find("Dano");

        if (t == null)
        {
            Debug.LogError("SEM HITBOX 'Dano' NO PLAYER.");
            return;
        }

        damageCollider = t.GetComponent<Collider2D>();
        if (damageCollider == null)
        {
            Debug.LogError("Dano não tem Collider2D!");
            return;
        }

        damageCollider.enabled = false;

        // pega renderer (visual da hitbox)
        sr = t.GetComponent<SpriteRenderer>();

        // garante estado inicial
        UpdateHitboxVisibility();

        var hitbox = t.GetComponent<DamageHitbox>();
        if (hitbox == null)
            hitbox = t.gameObject.AddComponent<DamageHitbox>();

        hitbox.Owner = this;
    }

    void Update()
    {
        // isso permite mudar no Inspector em tempo real
        UpdateHitboxVisibility();
    }

    void UpdateHitboxVisibility()
    {
        if (sr != null)
            sr.enabled = showHitbox;
    }

    public void PerformAttack(bool facingRight, Vector2 offset)
    {
        if (isAttacking) return;

        Transform point = facingRight ? attackPointRight : attackPointLeft;

        if (point != null)
            damageCollider.transform.position = point.position;
        else
            damageCollider.transform.position = transform.position;

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

        IDamageable dmg = other.GetComponent<IDamageable>();

        if (dmg == null)
            dmg = other.GetComponentInParent<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(damage, gameObject);
        }
    }
}