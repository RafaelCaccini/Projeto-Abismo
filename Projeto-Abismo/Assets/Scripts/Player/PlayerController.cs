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

        private Rigidbody2D rb;

        private float horizontalInput;
        private bool isJumping;
        private float jumpTimeCounter;

        // Super jump state
        private bool isSuperJumping;
        private float superJumpTimeCounter;
        private float lastSuperJumpTime;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentLife = maxLife;
        originalGravityScale = rb.gravityScale;
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
            // Aplicar dash mantendo a velocidade vertical atual (para não "quebrar" o pulo)
            rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, rb.linearVelocity.y);
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
        // Evita que o pulo normal dispare quando o jogador está tentando o super jump (CTRL + Space)
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // Só permite iniciar o pulo se estiver no chão e não estiver encostando numa parede
        if (Input.GetButtonDown("Jump") && isGrounded && !isTouchingWall && !ctrlHeld)
        {
            isJumping = true;
            jumpTimeCounter = jumpHoldTime;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false; // evita re-pular até colidir de novo com o chão
        }

        // Enquanto o botão estiver pressionado e dentro do tempo permitido, mantém/estende o pulo
        if (Input.GetButton("Jump") && isJumping)
        {
            if (jumpTimeCounter > 0f)
            {
                // Mantém a velocidade vertical para sustentar o pulo por um curto período
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        // Ao soltar a tecla, encerra a fase de "hold" do pulo
        if (Input.GetButtonUp("Jump"))
        {
            isJumping = false;
        }
    }

    void HandleSuperJump()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // Iniciar Super Jump: CTRL + Space (apenas quando estiver no chão e cooldown)
        if (ctrlHeld && Input.GetKeyDown(KeyCode.Space) && isGrounded && !isTouchingWall && Time.time >= lastSuperJumpTime + superJumpCooldown)
        {
            isSuperJumping = true;
            superJumpTimeCounter = superJumpHoldTime;
            lastSuperJumpTime = Time.time;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, superJumpForce);
            isGrounded = false;
        }

        // Enquanto CTRL e Space estiverem pressionados, manter a força por superJumpHoldTime
        if (ctrlHeld && Input.GetKey(KeyCode.Space) && isSuperJumping)
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

        // Ao soltar Space ou CTRL, encerra a fase de hold do super jump
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

            // reduzir/gravar gravidade para evitar queda brusca durante o dash
            rb.gravityScale = 0f;
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
        rb.gravityScale = originalGravityScale;
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