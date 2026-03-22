using UnityEngine;

public class Walker : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 6f;

    [Header("Stats")]
    [SerializeField] private int life = 3;
    [SerializeField] private int damage = 1;

    [Header("Light Detection")]
    [SerializeField] private Lampiao lampScript;        // Referência ao Lampião
    [SerializeField] private float lightRadius = 5f;    // Para detecção opcional por distância

    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    // Estado
    private int patrolDirection = 1;
    private int fleeDirection = 0;
    private bool isFleeing = false;

    // Controle de tempo de fuga
    private float fleeTimer = 0f;
    private const float fleeDuration = 7f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (lampScript == null)
            Debug.LogWarning("Walker: Lampião não atribuído!", this);
    }

    void Update()
    {
        HandleFleeTimer();

        // Fuga por proximidade, caso queira ativar mesmo sem Trigger
        if (!isFleeing && lampScript != null && lampScript.lightArea != null && lampScript.lightArea.activeSelf)
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

        // Raycast para parede
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

    // Detecção por Trigger do LightArea
    void OnTriggerEnter2D(Collider2D other)
    {
        if (lampScript == null) return;

        if (other.gameObject == lampScript.gameObject ||
            other.gameObject == lampScript.lightArea)
        {
            StartFleeing();
        }
    }

    private void StartFleeing()
    {
        if (lampScript == null) return;

        // Define direção oposta ao Lampião
        fleeDirection = (lampScript.transform.position.x > transform.position.x) ? -1 : 1;
        isFleeing = true;
        fleeTimer = fleeDuration;
    }

    private void StopFleeing()
    {
        isFleeing = false;
        fleeDirection = 0;
    }

    public void TakeDamage(int dmg)
    {
        life -= dmg;
        if (life <= 0) Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightRadius);

        Gizmos.color = Color.red;
        int dir = isFleeing ? fleeDirection : patrolDirection;
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(dir * wallCheckDistance, 0f));
    }
}