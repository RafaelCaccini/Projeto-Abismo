using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProjetilBlocker : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private int damage = 1;

    private Rigidbody2D rb;
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Launch(Vector2 direction, float speed, float gravity)
    {
        rb.gravityScale = gravity;
        rb.linearVelocity = direction.normalized * speed;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var player = collision.gameObject.GetComponentInParent<PlayerController>();

        if (player != null)
            player.TakeDamage(damage, gameObject);

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.TakeDamage(damage, gameObject);
            Destroy(gameObject);
        }
    }
}