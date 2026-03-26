using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aerial strike (pogo-like) melhorado:
/// - input contínuo e buffer configurável
/// - chain de ataques permitido
/// - bounce via AddForce(Impulse) com reset de velocidade Y
/// - hitbox controlada por Collider2D (ativar/desativar)
/// - verificação de "no ar" baseada em tag com tolerância para evitar flicker
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AerialStrikeController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.X;
    [SerializeField] [Range(-1f, 0f)] private float downInputThreshold = -0.5f;
    [SerializeField] private float inputBufferTime = 0.15f; // tempo de buffer para executar ataque

    [Header("Hitbox")]
    [SerializeField] private Collider2D hitboxCollider; // atribuir prefab/filho com isTrigger = true preferencialmente
    [SerializeField] private float hitboxDuration = 0.12f;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Damage / Bounce")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float bounceForce = 12f; // impulso aplicado (Impulse)
    [SerializeField] private bool usePerTargetLock = true; // evita múltiplos hits no mesmo alvo por ativação

    [Header("Cooldown / Flow")]
    [SerializeField] private float cooldown = 0.08f; // reduzido para fluidez

    [Header("Ground Check (usa tag)")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private float groundCheckOffset = 0.6f; // distância para checar abaixo do player
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private float groundGraceTime = 0.08f; // tolerância para variações de grounded

    // estado
    private Rigidbody2D rb;
    private float lastAttackInputTime = -999f;
    private float lastStrikeTime = -999f;
    private bool isHitboxActive = false;
    private float hitboxTimer = 0f;
    private HashSet<Collider2D> hitEnemiesThisActivation = new HashSet<Collider2D>();

    // ground tracking para evitar flicker
    private bool prevGrounded = false;
    private float lastLeftGroundTime = -999f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (hitboxCollider == null)
        {
            // cria hitbox filha padrão se não atribuída
            GameObject hb = new GameObject("AerialHitbox");
            hb.transform.SetParent(transform);
            hb.transform.localPosition = new Vector3(0f, -groundCheckOffset, 0f);
            var box = hb.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.9f, 0.4f);
            hitboxCollider = box;
            hitboxCollider.enabled = false;
        }
        else
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateGroundState();
        ProcessBufferedAttack();
        HandleHitboxTimer();
    }

    private void ReadInput()
    {
        if (Input.GetKeyDown(attackKey))
        {
            lastAttackInputTime = Time.time;
        }
    }

    private void ProcessBufferedAttack()
    {
        bool downHeld = Input.GetAxisRaw("Vertical") < downInputThreshold;
        bool bufferValid = Time.time <= lastAttackInputTime + inputBufferTime;
        bool cooldownReady = Time.time >= lastStrikeTime + cooldown;
        if (bufferValid && downHeld && cooldownReady && IsConsideredAirborne())
        {
            StartAerialStrike();
        }
    }

    private void StartAerialStrike()
    {
        lastStrikeTime = Time.time;
        ActivateHitbox();
        // feedback leve — substituir por animação/efeito se desejar
        Debug.Log("Aerial Strike: iniciada");
    }

    private void ActivateHitbox()
    {
        if (hitboxCollider == null) return;
        isHitboxActive = true;
        hitboxTimer = hitboxDuration;
        hitEnemiesThisActivation.Clear();
        hitboxCollider.enabled = true;
    }

    private void DeactivateHitbox()
    {
        if (hitboxCollider == null) return;
        isHitboxActive = false;
        hitboxCollider.enabled = false;
        hitEnemiesThisActivation.Clear();
    }

    private void HandleHitboxTimer()
    {
        if (!isHitboxActive) return;
        hitboxTimer -= Time.deltaTime;
        if (hitboxTimer <= 0f)
            DeactivateHitbox();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isHitboxActive) return;
        if (!other.CompareTag(enemyTag)) return;
        if (usePerTargetLock && hitEnemiesThisActivation.Contains(other)) return;

        // Aplica dano - utiliza SendMessage para compatibilidade com inimigos existentes
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        // Aplica bounce usando Impulse — resetando a velocidade vertical antes
        ApplyBounce();

        // marca alvo para evitar hits rápidos repetidos no mesmo alvo nesta ativação
        hitEnemiesThisActivation.Add(other);

        // feedback simples
        Debug.Log($"Aerial Strike: acertou {other.name}");
    }

    private void ApplyBounce()
    {
        // reset Y para garantir resposta consistente e aplicar impulso
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

        // permitir que o jogador encadeie imediatamente outro ataque caso pressione novamente
        // reduzimos lastStrikeTime para permitir reativação quase imediata (cadeia fluida)
        lastStrikeTime = Time.time - (cooldown * 0.5f);
    }

    /// <summary>
    /// Verifica se o jogador deve ser considerado 'no ar' tomando em conta tolerância
    /// para pequenas variações de grounded.
    /// </summary>
    private bool IsConsideredAirborne()
    {
        bool currentlyGrounded = IsGrounded();
        // se atualmente não está no chão, atualiza lastLeftGroundTime
        if (!currentlyGrounded)
            lastLeftGroundTime = Time.time;

        // Considera aéreo se não estiver no chão, ou se deixou o chão há pouco (tolerância)
        return !currentlyGrounded || (Time.time - lastLeftGroundTime) <= groundGraceTime;
    }

    /// <summary>
    /// Checa colisores abaixo do jogador comparando tags para decidir grounded.
    /// </summary>
    private bool IsGrounded()
    {
        Vector2 center = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, groundCheckRadius);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.gameObject == gameObject) continue;
            if (hit.CompareTag(groundTag))
                return true;
        }
        return false;
    }

    private void UpdateGroundState()
    {
        bool groundedNow = IsGrounded();
        if (groundedNow && !prevGrounded)
        {
            // acabou de encostar no chão
            prevGrounded = true;
        }
        else if (!groundedNow && prevGrounded)
        {
            // acabou de sair do chão
            prevGrounded = false;
            lastLeftGroundTime = Time.time;
        }
        else
        {
            prevGrounded = groundedNow;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hitboxCollider != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 pos = hitboxCollider.transform.position;
            if (hitboxCollider is BoxCollider2D box)
            {
                Vector3 size = new Vector3(box.size.x, box.size.y, 1f);
                Gizmos.DrawWireCube(pos + (Vector3)box.offset, size);
            }
        }

        Gizmos.color = Color.yellow;
        Vector3 checkPos = transform.position + Vector3.down * groundCheckOffset;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
}
