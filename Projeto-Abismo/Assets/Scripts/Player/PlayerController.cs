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

    /*
    [Header("Stomp")]
    [SerializeField] private int stompDamage = 1;
    [SerializeField] private float stompCooldown = 0.2f;
    [SerializeField] private float stompIntentWindow = 0.5f; // tempo que o "segurar S" é válido
    private float lastStompTime;
    private bool stompIntent;
    private float stompIntentTimestamp;
    private bool stompAvailable;
    */

    // Pequena proteção para evitar que o 'stomp' por colisão física conflite com a hitbox.
    // Por padrão desligado (controle via Inspector).
    /*
    [Header("Stomp")]
    [SerializeField] private bool collisionStompEnabled = false;
    */

    private Rigidbody2D rb;

    private float horizontalInput;
    private bool isJumping;
    private float jumpTimeCounter;

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

        /*
        // STOMP desativado: bloco comentado para evitar interferência com pogo/aerial strike
        // Se houver um stomp "registrado" e o jogador apertar P, aplica impulso para cima
        if (stompAvailable && Input.GetKeyDown(KeyCode.P))
        {
            stompAvailable = false;

            // Tenta delegar o bounce ao AerialStrikeController (se existir) para evitar lógica duplicada/conflicting
            var aerial = GetComponent<AerialStrikeController>();
            if (aerial != null)
            {
                aerial.ApplyBounce();
            }
            else
            {
                // fallback: aplica bounce localmente
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                Debug.Log("Stomp: bounce aplicado via PlayerController (fallback)");
            }

            isGrounded = false;
            isJumping = false;
            Debug.Log("Stomp: bounce aplicado via tecla P");
        }

        // expira a intenção de stomp passado o tempo
        if (stompIntent && Time.time > stompIntentTimestamp + stompIntentWindow)
            stompIntent = false;
        */
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

        // Detecta quando jogador pressiona/segura S para indicar intenção de stomp (agora suporta hold)
        // STOMP desativado: comentário para evitar setar stompIntent
        /*
        if (Input.GetKey(KeyCode.S))
        {
            stompIntent = true;
            stompIntentTimestamp = Time.time;
        }
        */
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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }

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
        if (attackBlockPrefab == null)
        {
            Debug.LogError("attackBlockPrefab está NULL ou quebrado!");
            return;
        }

        float directionMultiplier = facingRight ? 1f : -1f;

        Vector3 spawnPosition = transform.position
            + new Vector3(attackOffsetX * directionMultiplier, attackOffsetY, 0f);

        GameObject block = Instantiate(attackBlockPrefab, spawnPosition, Quaternion.identity);
        block.transform.localScale = transform.localScale;
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
    public void TakeDamage(int damage)
    {
        currentLife -= damage;

        Debug.Log("Vida: " + currentLife);

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

        /*
        // STOMP POR COLISÃO (executa apenas se collisionStompEnabled == true)
        if (collisionStompEnabled && collision.gameObject.CompareTag("Enemy"))
        {
            if (Time.time < lastStompTime + stompCooldown)
                return;

            float relativeY = transform.position.y - collision.transform.position.y;
            bool movingDown = rb.linearVelocity.y <= 0f;
            const float aboveThreshold = 0.18f;

            if (movingDown && relativeY > aboveThreshold && (stompIntent || Input.GetKey(KeyCode.S)))
            {
                collision.gameObject.SendMessage("TakeDamage", stompDamage, SendMessageOptions.DontRequireReceiver);

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

                rb.position = rb.position + Vector2.up * 0.05f;

                lastStompTime = Time.time;
                isGrounded = false;
                isJumping = false;

                Debug.Log($"Stomp registrado em {collision.gameObject.name}. Aperte P para bounce.");
            }
        }
        */
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

        /*
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // permite o jogador apertar S enquanto estiver sobre o inimigo para registrar o stomp
            if (Time.time >= lastStompTime + stompCooldown)
            {
                float relativeY = transform.position.y - collision.transform.position.y;
                bool movingDown = rb.linearVelocity.y <= 0f;
                const float aboveThreshold = 0.18f;

                if (movingDown && relativeY > aboveThreshold && Input.GetKey(KeyCode.S))
                {
                    collision.gameObject.SendMessage("TakeDamage", stompDamage, SendMessageOptions.DontRequireReceiver);
                    stompAvailable = true;
                    stompIntent = false;
                    lastStompTime = Time.time;
                    rb.position = rb.position + Vector2.up * 0.05f;
                    isGrounded = false;
                    isJumping = false;
                    Debug.Log($"Stomp registrado em {collision.gameObject.name} (stay). Aperte P para bounce.");
                }
            }
        }
        */
    }
}