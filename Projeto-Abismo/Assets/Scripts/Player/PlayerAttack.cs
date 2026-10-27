using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackDuration = 0.12f;
    [SerializeField] private float pogoBounceForce = 14f;

    [Header("Attack Points")]
    [SerializeField] private Transform attackPointRight;
    [SerializeField] private Transform attackPointLeft;
    [SerializeField] private Transform attackPointDown;

    [Header("Debug")]
    [SerializeField] private bool showHitbox = false;

    private Collider2D damageCollider;
    private SpriteRenderer sr;
    private HashSet<int> hitIDs = new HashSet<int>();
    private bool isAttacking;
    private bool isPogoAttack;

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

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
        sr = t.GetComponent<SpriteRenderer>();
        UpdateHitboxVisibility();

        var hitbox = t.GetComponent<DamageHitbox>();
        if (hitbox == null)
            hitbox = t.gameObject.AddComponent<DamageHitbox>();

        hitbox.Owner = this;
    }

    void Update()
    {
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

        float verticalInput = PlayerInputHandler.Instance != null
            ? PlayerInputHandler.Instance.Vertical()
            : Input.GetAxisRaw("Vertical");

        isPogoAttack = (verticalInput < -0.3f);

        Transform point;

        if (isPogoAttack && attackPointDown != null)
        {
            point = attackPointDown;
        }
        else
        {
            point = facingRight ? attackPointRight : attackPointLeft;
        }

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
        isPogoAttack = false;
    }

    public void RegisterHit(Collider2D other)
    {
        IDamageable dmg = other.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage, gameObject);

            Debug.Log($"[DEBUG ATAQUE] Acertou: {other.name} | É pogo? {isPogoAttack} | Está no chão? {playerController.IsGrounded}");

            if (isPogoAttack && !playerController.IsGrounded)
            {
                Debug.Log("[DEBUG POGO] Condições atendidas! Chamando o Pogo...");
                playerController.TriggerPogo(pogoBounceForce);
            }
        }
    }
}