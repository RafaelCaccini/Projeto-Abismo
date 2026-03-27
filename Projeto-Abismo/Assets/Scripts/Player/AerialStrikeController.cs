using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aerial strike (pogo-like) melhorado:
/// - input contínuo e buffer configurável
/// - chain de ataques permitido
/// - bounce via AddForce(Impulse) com reset de velocidade Y
/// - hitbox controlada por Collider2D (ativar/desativar) ou por prefab visual
/// - verificação de "no ar" baseada em tag com tolerância para evitar flicker
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AerialStrikeController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.P; // agora P aplica o hit
    [SerializeField] [Range(-1f, 0f)] private float downInputThreshold = -0.5f;
    [SerializeField] private float inputBufferTime = 0.15f; // tempo de buffer para executar ataque

    // Novas opções para usar mouse como alternativa ao P
    [Header("Input - Mouse Options")]
    [Tooltip("Permite usar o botão esquerdo do mouse (clique) como alternativa para ativar o pogo. Geralmente usado junto com S.")]
    [SerializeField] private bool allowMouseLeftPogo = true;
    [Tooltip("Permite usar o botão direito do mouse (clique) como alternativa para ativar o pogo.")]
    [SerializeField] private bool allowMouseRightPogo = false;
    [Tooltip("Se marcado, exige que a tecla S esteja segurada para que o clique do mouse dispare o pogo.")]
    [SerializeField] private bool requireSHoldForMousePogo = true;
    [Tooltip("Se marcado, exige que a tecla S esteja segurada para que a tecla configurada dispare o pogo.")]
    [SerializeField] private bool requireSHoldForKeyPogo = true;

    [Header("Hitbox")]
    [SerializeField] private Collider2D hitboxCollider; // usado se não quiser prefab visual
    [SerializeField] private GameObject hitboxPrefab; // prefab visual (opcional) — você vai atribuir
    [SerializeField] private float hitboxDuration = 0.12f;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Hitbox Position")]
    [Tooltip("Distância em unidades que a hitbox ficará abaixo do pivot do jogador. Use o slider ou insira um número.")]
    [SerializeField] [Range(0f, 2f)] private float hitboxYOffset = 0.6f;
    [Tooltip("Tamanho da área de hit da hitbox (largura, altura) - usado para OverlapBox")]
    [SerializeField] private Vector2 hitboxSize = new Vector2(0.9f, 0.4f);

    [Header("Visual")]
    [Tooltip("Se marcado, a hitbox (prefab ou placeholder) ficará invisível. Desmarque para ver o sprite.")]
    [SerializeField] private bool hideHitboxVisual = false;
    [Tooltip("Sprite opcional usado como placeholder para a hitbox caso não exista prefab (visível apenas se 'hideHitboxVisual' estiver desmarcado).")]
    [SerializeField] private Sprite hitboxDebugSprite;

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
    private float lastStrikeTime = -999f;
    private bool isHitboxActive = false;
    private float hitboxTimer = 0f;
    private HashSet<Collider2D> hitEnemiesThisActivation = new HashSet<Collider2D>();

    // se usar prefab visual
    private GameObject hitboxInstance;

    // ground tracking para evitar flicker
    private bool prevGrounded = false;
    private float lastLeftGroundTime = -999f;
    private float lastAttackInputTime = -999f; // Adicione esta linha junto aos outros campos privados

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (hitboxCollider == null && hitboxPrefab == null)
        {
            // cria hitbox filha padrão se não atribuída
            GameObject hb = new GameObject("AerialHitbox");
            hb.transform.SetParent(transform);
            hb.transform.localPosition = new Vector3(0f, -hitboxYOffset, 0f); // usa hitboxYOffset configurável
            var box = hb.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = hitboxSize;
            hitboxCollider = box;
            hitboxCollider.enabled = false;

            // adiciona SpriteRenderer de placeholder se houver sprite de debug
            if (hitboxDebugSprite != null)
            {
                var sr = hb.AddComponent<SpriteRenderer>();
                sr.sprite = hitboxDebugSprite;
                sr.sortingOrder = 100;
                sr.color = new Color(1f, 1f, 1f, 0.85f);
                sr.enabled = !hideHitboxVisual;
            }
        }
        else if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
            // tenta readaptar o tamanho se for BoxCollider2D
            if (hitboxCollider is BoxCollider2D boxCol)
                boxCol.size = hitboxSize;
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateGroundState();
        HandleHitboxTimer();
    }

    private void ReadInput()
    {
        // Tecla configurada (ex.: P). Se requerido, exige S segurado.
        bool keyPressed = Input.GetKeyDown(attackKey) && (!requireSHoldForKeyPogo || Input.GetKey(KeyCode.S));

        // Clique do mouse como alternativa (0 = esquerdo, 1 = direito).
        // A combinação S + ClickEsquerdo é controlada por allowMouseLeftPogo + requireSHoldForMousePogo.
        bool mouseLeft = allowMouseLeftPogo && Input.GetMouseButtonDown(0) && (!requireSHoldForMousePogo || Input.GetKey(KeyCode.S));
        bool mouseRight = allowMouseRightPogo && Input.GetMouseButtonDown(1) && (!requireSHoldForMousePogo || Input.GetKey(KeyCode.S));

        if ((keyPressed || mouseLeft || mouseRight) && Time.time >= lastStrikeTime + cooldown)
        {
            StartAerialStrike();
        }

        // Mantém suporte ao buffer antigo caso queira usar o attackKey com buffer/down input
        if (Input.GetKeyDown(attackKey))
        {
            lastAttackInputTime = Time.time;
        }
    }

    private void StartAerialStrike()
    {
        lastStrikeTime = Time.time;
        ActivateHitbox();

        // Se houver prefab visual, instantiate para mostrar a hitbox
        if (hitboxPrefab != null)
        {
            if (hitboxInstance != null) Destroy(hitboxInstance);
            hitboxInstance = Instantiate(hitboxPrefab, transform);
            // coloca o prefab na posição desejada (agora usa hitboxYOffset)
            hitboxInstance.transform.localPosition = new Vector3(0f, -hitboxYOffset, 0f);
            // garante que qualquer collider do prefab seja trigger
            var col = hitboxInstance.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            // aplica visibilidade conforme checkbox
            UpdateHitboxVisualVisibility(hitboxInstance);
        }

        // usa área configurada para detectar alvos (não depende do collider instanciado)
        DetectAndApplyHits();

        // feedback leve — substituir por animação/efeito se desejar
        Debug.Log("Aerial Strike: iniciada via P");
    }

    private void UpdateHitboxVisualVisibility(GameObject instance)
    {
        if (instance == null) return;
        var sr = instance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = !hideHitboxVisual;
        }
        else if (!hideHitboxVisual && hitboxDebugSprite != null)
        {
            // se usuário quer ver e a instância não tem SR, adiciona um placeholder
            sr = instance.AddComponent<SpriteRenderer>();
            sr.sprite = hitboxDebugSprite;
            sr.sortingOrder = 100;
            sr.color = new Color(1f, 1f, 1f, 0.85f);
            sr.enabled = true;
        }
    }

    private void ActivateHitbox()
    {
        isHitboxActive = true;
        hitboxTimer = hitboxDuration;
        hitEnemiesThisActivation.Clear();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
            // se houver sprite no collider, aplica visibilidade
            var sr = hitboxCollider.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = !hideHitboxVisual;
        }
    }

    private void DeactivateHitbox()
    {
        isHitboxActive = false;
        hitEnemiesThisActivation.Clear();

        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        if (hitboxInstance != null)
        {
            Destroy(hitboxInstance);

            // Opcional: em vez de destruir, deixar invisível (comentado conforme pedido)
            // hitboxInstance.SetActive(false);
            // var sr = hitboxInstance.GetComponent<SpriteRenderer>();
            // if (sr) sr.enabled = false;
        }
    }

    private void HandleHitboxTimer()
    {
        if (!isHitboxActive) return;
        hitboxTimer -= Time.deltaTime;
        if (hitboxTimer <= 0f)
            DeactivateHitbox();
    }

    private void DetectAndApplyHits()
    {
        // define centro e tamanho explicitamente para garantir que a detecção siga hitboxYOffset / hitboxSize
        Vector2 center = (Vector2)transform.position + Vector2.down * hitboxYOffset;
        Vector2 size = hitboxSize;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (var h in hits)
        {
            if (h == null) continue;
            if (h.gameObject == gameObject) continue; // ignora o próprio jogador
            if (!h.CompareTag(enemyTag)) continue;
            if (usePerTargetLock && hitEnemiesThisActivation.Contains(h)) continue;

            // Aplica dano e bounce
            h.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            ApplyBounce();

            hitEnemiesThisActivation.Add(h);
            Debug.Log($"Aerial Strike: acertou {h.name} via área (tag={h.tag})");
        }
    }

    private void ApplyBounce()
    {
        // reset Y para garantir resposta consistente e aplicar impulso
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

        // permitir encadeamento fluido
        lastStrikeTime = Time.time - (cooldown * 0.5f);
    }

    /// <summary>
    /// Verifica se o jogador deve ser considerado 'no ar' tomando em conta tolerância
    /// para pequenas variações de grounded.
    /// </summary>
    private bool IsConsideredAirborne()
    {
        bool currentlyGrounded = IsGrounded();
        if (!currentlyGrounded)
            lastLeftGroundTime = Time.time;

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
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position + Vector3.down * hitboxYOffset;
        Vector3 size = new Vector3(hitboxSize.x, hitboxSize.y, 1f);
        Gizmos.DrawWireCube(pos, size);

        Gizmos.color = Color.yellow;
        Vector3 checkPos = transform.position + Vector3.down * groundCheckOffset;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }

    // Atualiza visibilidade no Editor quando trocar a checkbox
    private void OnValidate()
    {
        if (hitboxInstance != null)
            UpdateHitboxVisualVisibility(hitboxInstance);

        if (hitboxCollider != null)
        {
            var sr = hitboxCollider.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = !hideHitboxVisual;
        }
    }
}
