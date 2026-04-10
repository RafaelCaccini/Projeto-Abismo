using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 30f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float jumpHoldTime = 0.2f;
    [SerializeField] private bool useHeightBasedJump = true; // alterna entre tempo (false) e altura (true)
    [SerializeField] private float jumpMaxHeight = 2.2f; // altura máxima alcançável segurando o botão
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private string wallTag = "Wall";

    [Header("Super Jump")]
    [SerializeField] private float superJumpForce = 30f;
    [SerializeField] private float superJumpHoldTime = 0.35f;
    [SerializeField] private float superJumpCooldown = 1.2f;
    [SerializeField] private float superJumpChargeTime = 3f; // tempo necessário para charge (3s)

    [Header("Attack")]
    [SerializeField] private GameObject attackBlockPrefab;
    [SerializeField] private float attackOffsetX = 1.6f;
    [SerializeField] private float attackOffsetY = 0.4f;
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private KeyCode attackKey = KeyCode.X;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;

    [Header("Life")]
    [SerializeField] private int maxLife = 5;
    private int currentLife;
    [SerializeField] private Lampiao lampiao;
    public Lampiao Lampiao => lampiao;

    public bool LuzAtiva { get; private set; }

    public void SetLuz(bool estado)
    {
        LuzAtiva = estado;
    }

    private Rigidbody2D rb;

    private float horizontalInput;
    private bool isJumping;
    private float jumpTimeCounter;
    private float jumpStartY; // usado para pulo baseado em altura

    // Super jump state
    private bool isSuperJumping;
    private float superJumpTimeCounter;
    private float lastSuperJumpTime;
    private float superJumpChargeTimer;

    // Estados de contato para controlar quando é permitido pular
    private bool isGrounded;
    private bool isTouchingWall;

    private bool facingRight = true;
    private float lastAttackTime;
    private PlayerAttack playerAttack;

    // Dash state
    private bool isDashing;
    private float dashTimeLeft;
    private float lastDashTime;
    private Vector2 dashDirection;
    private float originalGravityScale;
    private float storedVerticalVelocity; // guarda velocity.y antes do dash

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentLife = maxLife;
        originalGravityScale = rb.gravityScale;

        // assegura componente PlayerAttack e configura fallback
        playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack == null)
            playerAttack = gameObject.AddComponent<PlayerAttack>();

        // AUTO SET DO LAMPIÃO (evita erro humano)
        if (lampiao == null)
        {
            lampiao = GetComponentInChildren<Lampiao>();

            if (lampiao == null)
                Debug.LogError("[PlayerController] Lampião NÃO encontrado!");
            else
                Debug.Log("[PlayerController] Lampião auto-atribuído");
        }
    }

    void Update()
    {
        GetInput();
        HandleFlip();
        HandleSuperJump();
        HandleJump();
        HandleAttack();
        HandleDash();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            // Movimento 100% travado na vertical durante dash (não altera constraints físicas)
            rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, 0f);
            return;
        }

        HandleMovement();
    }

    void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
    }

    void HandleMovement()
    {
        float targetSpeed = horizontalInput * maxSpeed;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    void HandleJump()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (Input.GetButtonDown("Jump") && isGrounded && !isTouchingWall && !ctrlHeld)
        {
            isJumping = true;
            jumpTimeCounter = jumpHoldTime;
            jumpStartY = rb.position.y;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }

        if (useHeightBasedJump)
        {
            // Pulo baseado em altura alvo enquanto segura o botão
            if (Input.GetButton("Jump") && isJumping)
            {
                float currentHeight = rb.position.y - jumpStartY;
                if (currentHeight < jumpMaxHeight)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                }
                else
                {
                    isJumping = false;
                }
            }

            if (Input.GetButtonUp("Jump"))
            {
                isJumping = false;
            }
        }
        else
        {
            // Comportamento clássico baseado em tempo de hold
            if (Input.GetButton("Jump") && isJumping)
            {
                if (jumpTimeCounter > 0f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    jumpTimeCounter -= Time.deltaTime;
                }
                else
                {
                    isJumping = false;
                }
            }

            if (Input.GetButtonUp("Jump"))
            {
                isJumping = false;
            }
        }
    }

    void HandleSuperJump()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // Charging logic: segurando CTRL + SPACE acumula tempo até superJumpChargeTime
        if (ctrlHeld && Input.GetKey(KeyCode.Space))
        {
            superJumpChargeTimer += Time.deltaTime;

            // opcional: feedback de carregamento (log)
            if (superJumpChargeTimer >= 0.1f && superJumpChargeTimer < 0.1f + Time.deltaTime)
                Debug.Log("Super Jump: começando carregamento...");

            if (superJumpChargeTimer >= superJumpChargeTime)
            {
                // só executa se estiver no chão e cooldown OK
                if (isGrounded && !isTouchingWall && Time.time >= lastSuperJumpTime + superJumpCooldown)
                {
                    isSuperJumping = true;
                    superJumpTimeCounter = superJumpHoldTime;
                    lastSuperJumpTime = Time.time;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, superJumpForce);
                    isGrounded = false;
                    Debug.Log("Super Jump: executado após charge completo");
                }

                // reset do timer após execução para evitar múltiplos triggers
                superJumpChargeTimer = 0f;
            }
        }
        else
        {
            // soltou antes de completar: cancelar charge
            if (superJumpChargeTimer > 0f && superJumpChargeTimer < superJumpChargeTime)
            {
                Debug.Log("Super Jump: charge cancelado");
            }
            superJumpChargeTimer = 0f;
        }

        // comportamento de hold pós-execução (mantém força enquanto o jogador segura, como antes)
        if (isSuperJumping && Input.GetKey(KeyCode.Space))
        {
            if (superJumpTimeCounter > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, superJumpForce);
                superJumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isSuperJumping = false;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space) || !ctrlHeld)
        {
            isSuperJumping = false;
        }
    }

    void HandleFlip()
    {
        if (horizontalInput > 0 && !facingRight)
            Flip();
        else if (horizontalInput < 0 && facingRight)
            Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // Método público adicionado para suportar CameraTarget.IsFacingRight()
    public bool IsFacingRight()
    {
        return facingRight;
    }

    void HandleAttack()
    {
        if (Input.GetKeyDown(attackKey) && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    void PerformAttack()
    {
        // delega ao PlayerAttack (usa filho 'Dano' se existir, senão fallback prefab)
        playerAttack.PerformAttack(facingRight, new Vector2(attackOffsetX, attackOffsetY));
    }

    void HandleDash()
    {
        if (Input.GetKeyDown(dashKey) && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            // direção do dash: input horizontal se houver, senão direção que o personagem enfrenta
            float dir = Mathf.Abs(horizontalInput) > 0.1f ? horizontalInput : (facingRight ? 1f : -1f);
            dashDirection = new Vector2(Mathf.Sign(dir), 0f);

            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;

            // GUARDA velocidade vertical atual e zera componente vertical + gravidade
            storedVerticalVelocity = rb.linearVelocity.y;

            // Não alteramos constraints rigidbody (evita snaps/jitter). Em vez disso, desabilitamos gravidade temporariamente.
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, 0f);
        }

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
                EndDash();
        }
    }

    void EndDash()
    {
        isDashing = false;

        // restaura gravidade
        rb.gravityScale = originalGravityScale;

        // restaura a componente vertical guardada
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, storedVerticalVelocity);
    }

    // VIDA DO PLAYER
    public void TakeDamage(int damage, GameObject source)
    {
        currentLife -= damage;

        Debug.Log($"💥 Player tomou {damage} de {source.name} | Vida: {currentLife}");

        if (currentLife <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player morreu");
        Destroy(gameObject);
    }

    // Colisões para controlar isGrounded e isTouchingWall
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = true;
            isJumping = false;
        }

        if (collision.gameObject.CompareTag(wallTag))
        {
            isTouchingWall = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = false;
        }

        if (collision.gameObject.CompareTag(wallTag))
        {
            isTouchingWall = false;
        }
    }

    // Opcional: garante atualização de contato se o objeto permanecer em contato
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = true;
        }

        if (collision.gameObject.CompareTag(wallTag))
        {
            isTouchingWall = true;
        }
    }
}