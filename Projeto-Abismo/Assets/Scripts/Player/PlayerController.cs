using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour, IDamageable
{

    [Header("Movement")]
    private Animator anim;
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

    [SerializeField] private float invincibilityTime = 0.3f;
    private bool isInvincible;

    [SerializeField] private Lampiao lampiao;
    public Lampiao Lampiao => lampiao;
    public int CurrentLife => currentLife;
    public int MaxLife => maxLife;
    private float lastMoveDirection = 1f;
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
        anim = GetComponentInChildren<Animator>();

        if (anim == null)
            Debug.LogError("[PlayerController] Animator NÃO encontrado no Player!");

        originalGravityScale = rb.gravityScale;

        currentLife = maxLife;

        playerAttack = GetComponent<PlayerAttack>();

        if (playerAttack == null)
            playerAttack = gameObject.AddComponent<PlayerAttack>();

        if (lampiao == null)
        {
            lampiao = GetComponentInChildren<Lampiao>();

            if (lampiao == null)
                Debug.LogError("[PlayerController] Lampião NÃO encontrado!");
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
        HandleAnimations();
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

        // guarda última direção válida
        if (horizontalInput > 0)
            lastMoveDirection = 1f;
        else if (horizontalInput < 0)
            lastMoveDirection = -1f;
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
        if (lastMoveDirection > 0 && !facingRight)
            Flip();
        else if (lastMoveDirection < 0 && facingRight)
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

            if (anim != null)
            {
                anim.ResetTrigger("Attack");
                anim.SetTrigger("Attack");
            }
        }
    }

    void PerformAttack()
    {
        // delega ao PlayerAttack (usa filho 'Dano' se existir, senão fallback prefab)
        bool attackRight = lastMoveDirection > 0;
        playerAttack.PerformAttack(attackRight, new Vector2(attackOffsetX, attackOffsetY));
    }

    void HandleDash()
    {
        if (Input.GetKeyDown(dashKey) && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            // direção do dash: input horizontal se houver, senão direção que o personagem enfrenta
            float dir = horizontalInput != 0 ? horizontalInput : lastMoveDirection;
            dashDirection = new Vector2(dir, 0f);

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
        if (isInvincible)
            return;

        Debug.Log("PLAYER TOMOU DANO");

        currentLife -= damage;

        Debug.Log("VIDA ATUAL: " + currentLife);

        if (currentLife <= 0)
        {
            currentLife = 0;

            Debug.Log("CHAMANDO DIE");

            Die();

            return;
        }

        StartCoroutine(InvincibilityCoroutine());
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }


    void HandleAnimations()
    {
        if (anim == null)
            return;

        float velX =
            Mathf.Abs(rb.linearVelocity.x);

        float velY =
            rb.linearVelocity.y;

        // RUN

        bool isRunning =
            velX > 0.1f &&
            isGrounded &&
            !isDashing;

        anim.SetBool(
            "IsRun",
            isRunning
        );

        // JUMP

        bool jumping =
    velY > 0.1f &&
    !isGrounded;

        anim.SetBool(
            "IsJump",
            jumping
        );

        // FALL

        bool falling =
  
            velY < -0.1f &&
    !isGrounded;

        anim.SetBool(
            "IsFalling",
            falling
        );

        // GROUNDED

        anim.SetBool(
            "IsGrounded",
            isGrounded
        );
    }

    void Die()
    {
        Debug.Log("Player morreu");

        // trava movimento
        rb.linearVelocity = Vector2.zero;

        // desativa física
        rb.simulated = false;

        // desativa colisão
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.enabled = false;

        // desliga animações
        if (anim != null)
        {
            anim.SetBool("IsRun", false);
            anim.SetBool("IsJump", false);
            anim.SetBool("IsFalling", false);
        }

        // mostra tela
        if (DeathScreen.instance != null)
        {
            DeathScreen.instance.MostrarTelaMorte();
        }
        else
        {
            Debug.LogError("DeathScreen NULL");
        }
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

    private void OnCollisionExit2D(
    Collision2D collision
)
    {
        if (
            collision.gameObject.CompareTag(
                groundTag
            )
        )
        {
            isGrounded = false;
        }

        if (
            collision.gameObject.CompareTag(
                wallTag
            )
        )
        {
            isTouchingWall = false;
        }
    }

    // Opcional: garante atualização de contato se o objeto permanecer em contato
    private void OnCollisionStay2D(
       Collision2D collision
   )
    {
        if (
            collision.gameObject.CompareTag(
                groundTag
            )
        )
        {
            foreach (
                ContactPoint2D contact
                in collision.contacts
            )
            {
                // chão REAL vindo de baixo

                if (contact.normal.y > 0.7f)
                {
                    isGrounded = true;
                    return;
                }
            }
        }

        if (
            collision.gameObject.CompareTag(
                wallTag
            )
        )
        {
            isTouchingWall = true;
        }
    }

}