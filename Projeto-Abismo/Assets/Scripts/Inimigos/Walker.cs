using UnityEngine;

public class Walker : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 6f;

    [Header("Stats")]
    [SerializeField] private int life = 3;
    [SerializeField] private int damage = 1;

    [Header("Combat")]
    [SerializeField] private float damageCooldown = 1f;

    [Header("Light Detection")]
    [SerializeField] private Lampiao lampScript;
    [SerializeField] private float lightRadius = 5f;

    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private int patrolDirection = 1;
    private int fleeDirection = 0;
    private bool isFleeing = false;

    private float fleeTimer = 0f;
    private const float fleeDuration = 7f;

    private float lastDamageTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (lampScript == null)
            lampScript = FindFirstObjectByType<Lampiao>();
    }

    void Update()
    {
        HandleFleeTimer();

        if (!isFleeing && lampScript != null && lampScript.IsLightOn)
        {
            float dist = Vector2.Distance(transform.position, lampScript.transform.position);
            if (dist <= lightRadius)
                StartFleeing();
        }
    }

    void FixedUpdate()
    {
        Move();
        UpdateFlip();
    }

    private void HandleFleeTimer()
    {
        if (isFleeing)
        {
            fleeTimer -= Time.deltaTime;
            if (fleeTimer <= 0f)
                StopFleeing();
        }
    }

    private void Move()
    {
        int currentDir = isFleeing ? fleeDirection : patrolDirection;
        if (currentDir == 0) currentDir = patrolDirection;

        float speed = isFleeing ? runSpeed : walkSpeed;

        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(currentDir * 0.4f, 0f);
        Vector2 rayDirection = new Vector2(currentDir, 0f);

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, wallCheckDistance, wallLayer);
        Debug.DrawRay(rayOrigin, rayDirection * wallCheckDistance, Color.red);

        if (hit.collider != null)
        {
            if (isFleeing) fleeDirection *= -1;
            else patrolDirection *= -1;
            return;
        }

        rb.linearVelocity = new Vector2(currentDir * speed, rb.linearVelocity.y);
    }

    private void UpdateFlip()
    {
        if (rb.linearVelocity.x != 0f)
            sr.flipX = rb.linearVelocity.x < 0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (lampScript == null) return;

        if (other.gameObject == lampScript.LightArea)
        {
            StartFleeing();
        }
    }

    private void StartFleeing()
    {
        if (lampScript == null) return;

        fleeDirection = (lampScript.transform.position.x > transform.position.x) ? -1 : 1;
        isFleeing = true;
        fleeTimer = fleeDuration;
    }

    private void StopFleeing()
    {
        isFleeing = false;
        fleeDirection = 0;
    }

    // =========================
    // 💥 DANO (CORRIGIDO)
    // =========================

    private void OnCollisionStay2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Player"))
            return;

        TryDamage(other.gameObject);
    }

    private void TryDamage(GameObject target)
    {
        if (Time.time < lastDamageTime + damageCooldown)
            return;

        IDamageable dmg = target.GetComponentInParent<IDamageable>();

        if (dmg == null)
            return;

        dmg.TakeDamage(damage, gameObject);
        lastDamageTime = Time.time;

        Debug.Log($"Walker causou {damage} de dano em {target.name}");
    }

    // =========================
    // VIDA DO WALKER
    // =========================

    public void TakeDamage(int dmg)
    {
        life -= dmg;

        if (life <= 0)
            Destroy(gameObject);
    }
}