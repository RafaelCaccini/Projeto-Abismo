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
    [SerializeField] private LayerMask wallLayer = 0;

    [Header("Pouso por Altura")]
    [SerializeField] private bool usarPousoPorAltura = true;
    [SerializeField] private float alturaMinimaPousoAlto = 3f;
    [SerializeField] private bool debugPouso = false;

    // queda tracking
    private float fallStartY = 0f;
    private bool isFallingStarted = false;
    private Coroutine clearPousoAltoCoroutine = null;

    [Header("Attack")]
    [SerializeField] private float attackOffsetX = 1.6f;
    [SerializeField] private float attackOffsetY = 0.4f;
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private KeyCode attackKey = KeyCode.F;

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

    [Header("Death Animation")]
    [SerializeField] private float deathAnimationDuration = 1.5f; // tempo de espera após disparar a animação de morte no Animator
    private bool isDead = false; // trava todas as ações e animações após a morte

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

    private IEnumerator ClearPousoAltoCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (anim != null)
            anim.SetBool("PousoAlto", false);
        clearPousoAltoCoroutine = null;
    }

    private Rigidbody2D rb;

    private float horizontalInput;
    private bool isJumping;
    private float jumpTimeCounter;
    private float jumpStartY; // usado para pulo baseado em altura

    // (Super Jump state removed)

    // Estados de contato para controlar quando é permitido pular
    private bool isGrounded;
    private bool isTouchingWall;

    private bool facingRight = true;
    private float lastAttackTime;
    private PlayerAttack playerAttack;

    [Header("Abilities")]
    [SerializeField] private PlayerAbilities playerAbilities; // optional assign in inspector
    private PlayerAbilities abilities;
    private bool abilitiesAvailable = false;

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

        // Abilities: prefer inspector assignment, fallback to GetComponent
        if (playerAbilities != null)
        {
            abilities = playerAbilities;
        }
        else
        {
            abilities = GetComponent<PlayerAbilities>();
        }

        if (abilities == null)
        {
            Debug.LogError("[PlayerController] PlayerAbilities componente NÃO encontrado no Player. Adicione PlayerAbilities ao GameObject para gerenciar habilidades.");
            abilitiesAvailable = false;
        }
        else
        {
            abilitiesAvailable = true;
        }
    }

    void Update()
    {
        GetInput();

        HandleFlip();

        HandleJump();

        if (
            abilitiesAvailable &&
            abilities != null &&
            abilities.Has(SkillType.ChargedJump)
        )
        {
            HandleChargedJump();
        }

        DetectFallStart();

        HandleAttack();

        HandleDash();

        HandleLampiao();

        HandleAnimations();
    }

    // Detecta quando o jogador começa a cair (velocidade vertical negativa) e marca o Y inicial
    private void DetectFallStart()
    {
        if (!usarPousoPorAltura)
            return;

        // Não iniciar detecção de queda durante dash ou quando já no chão
        if (isGrounded || isFallingStarted || isDashing)
            return;

        // Considere que a queda começou quando a velocidade vertical ficar negativa
        if (rb != null && rb.linearVelocity.y < -0.1f)
        {
            isFallingStarted = true;
            fallStartY = rb.position.y;
            if (debugPouso)
                Debug.Log($"[Pouso] Iniciou queda em Y={fallStartY}");
        }
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

        if (isDashing)
        {
            // Movimento 100% travado na vertical durante dash (não altera constraints físicas)
            rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, 0f);
            return;
        }

        // Previne escalada de parede: se estiver colidindo com uma parede, estiver no ar
        // e tentar subir, cancela o movimento vertical para não escalar a parede.
        // (wallLayer é usado como checagem complementar na detecção de colisão)
        if (isTouchingWall && !isGrounded && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        HandleMovement();
    }

    void GetInput()
    {
        horizontalInput = PlayerInputHandler.Instance != null
            ? PlayerInputHandler.Instance.Horizontal()
            : Input.GetAxisRaw("Horizontal");

        if (horizontalInput > 0.1f) lastMoveDirection = 1f;
        else if (horizontalInput < -0.1f) lastMoveDirection = -1f;
    }

    void HandleMovement()
    {
        if (isDead)
            return;

        float targetSpeed = horizontalInput * maxSpeed;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    void HandleJump()
    {
        if (isDead)
            return;

        var input = PlayerInputHandler.Instance;
        bool pulouDown = input != null ? input.PularDown() : Input.GetButtonDown("Jump");

        if (pulouDown && isGrounded && !isTouchingWall)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpStartY = rb.position.y;
            jumpTimeCounter = jumpHoldTime;
            isGrounded = false;
            isJumping = abilitiesAvailable && abilities != null && abilities.Has(SkillType.ChargedJump);

            if (anim != null && !isJumping)
                anim.SetBool("PuloPressionado", false);
        }
    }

    void HandleChargedJump()
    {
        if (isDead)
            return;

        // =========================================
        // VERIFICA SE A HABILIDADE EXISTE
        // =========================================

        bool habilidadeDesbloqueada =
            abilitiesAvailable &&
            abilities != null &&
            abilities.Has(SkillType.ChargedJump);

        // Se não possui a habilidade,
        // não pode fazer pulo pressionado
        if (!habilidadeDesbloqueada)
        {
            isJumping = false;

            if (anim != null)
                anim.SetBool("PuloPressionado", false);

            return;
        }

        // =========================================
        // NÃO ESTÁ EXECUTANDO PULO PRESSIONADO
        // =========================================

        if (!isJumping)
        {
            if (anim != null)
                anim.SetBool("PuloPressionado", false);

            return;
        }

        bool puloPressionadoAtivo = false;

        // =========================================
        // SOLTOU O BOTÃO
        // =========================================

        if (Input.GetButtonUp("Jump"))
        {
            isJumping = false;

            if (anim != null)
                anim.SetBool("PuloPressionado", false);

            return;
        }

        // =========================================
        // SEGURANDO O BOTÃO
        // =========================================

        if (Input.GetButton("Jump"))
        {
            // -----------------------------------------
            // SISTEMA BASEADO EM ALTURA
            // -----------------------------------------

            if (useHeightBasedJump)
            {
                float alturaAtual =
                    rb.position.y - jumpStartY;

                if (
                    alturaAtual < jumpMaxHeight &&
                    jumpTimeCounter > 0f
                )
                {
                    rb.linearVelocity = new Vector2(
                        rb.linearVelocity.x,
                        jumpForce
                    );

                    jumpTimeCounter -= Time.deltaTime;

                    puloPressionadoAtivo = true;
                }
                else
                {
                    isJumping = false;
                }
            }

            // -----------------------------------------
            // SISTEMA BASEADO EM TEMPO
            // -----------------------------------------

            else
            {
                if (jumpTimeCounter > 0f)
                {
                    rb.linearVelocity = new Vector2(
                        rb.linearVelocity.x,
                        jumpForce
                    );

                    jumpTimeCounter -= Time.deltaTime;

                    puloPressionadoAtivo = true;
                }
                else
                {
                    isJumping = false;
                }
            }
        }

        // =========================================
        // ANIMAÇÃO
        // =========================================

        if (anim != null)
        {
            anim.SetBool(
                "PuloPressionado",
                puloPressionadoAtivo
            );
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
        if (isDead)
            return;

        var input = PlayerInputHandler.Instance;
        // Quando PlayerInputHandler existe, delega para AtacarDown() (que já inclui mouse).
        // Fallback: tecla X + clique esquerdo do mouse (fonte única de mouse está no handler).
        bool atacou = input != null
            ? input.AtacarDown()
            : Input.GetKeyDown(attackKey) || Input.GetMouseButtonDown(0);

        if (atacou && Time.time >= lastAttackTime + attackCooldown)
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
    void HandleLampiao()
    {
        var input = PlayerInputHandler.Instance;
        bool lampiaoInput = input != null ? input.LampiaoDown() : Input.GetKeyDown(KeyCode.L);

        if (lampiaoInput && lampiao != null)
            lampiao.ToggleLuzExterno();
    }

    void HandleDash()
    {
        if (isDead)
            return;

        var input = PlayerInputHandler.Instance;
        bool dashInput = input != null ? input.DashDown() : Input.GetKeyDown(dashKey);

        if (dashInput && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            if (!abilitiesAvailable || !abilities.Has(SkillType.Dash)) return;

            float dir = Mathf.Abs(horizontalInput) > 0.1f ? horizontalInput : lastMoveDirection;
            dashDirection = new Vector2(dir, 0f);
            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;

            storedVerticalVelocity = rb.linearVelocity.y;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, 0f);
        }

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f) EndDash();
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
        // Depois que a Player morre, ignora novos danos completamente
        if (isDead)
            return;

        // Debug: always log source of damage for investigation
        string sourceName = source != null ? source.name : "NULL";
        string sourceTag = source != null ? source.tag : "NULL";
        string sourceLayer = "NULL";
        Vector3 sourcePos = Vector3.zero;
        if (source != null)
        {
            sourceLayer = LayerMask.LayerToName(source.layer);
            sourcePos = source.transform.position;
        }

        string stack = System.Environment.StackTrace;

        if (isInvincible)
        {
            Debug.LogWarning($"[PLAYER DAMAGE IGNORED] Fonte: {sourceName} | Tag: {sourceTag} | Layer: {sourceLayer} | Pos: {sourcePos} | Dano: {damage} -- Player is invincible.\nStack:\n{stack}");
            return;
        }

        Debug.Log("PLAYER TOMOU DANO");
        Debug.Log($"[PLAYER DAMAGE] Fonte: {sourceName} | Tag: {sourceTag} | Layer: {sourceLayer} | Pos: {sourcePos} | Dano: {damage}\nStack:\n{stack}");

        currentLife -= damage;

        Debug.Log("VIDA ATUAL: " + currentLife);

        if (currentLife <= 0)
        {
            currentLife = 0;

            Debug.Log("CHAMANDO DIE");

            Die();

            return;
        }

        // Player sobreviveu — dispara animação de dano antes da invencibilidade
        if (anim != null)
            anim.SetTrigger("Dano");

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
        if (isDead)
            return;

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

        StartCoroutine(DeathCoroutine());
    }

    /// <summary>
    /// Processo de morte: toca a animação Morrendo, espera seu tempo,
    /// então desativa física, colisão e mostra a tela de morte.
    /// Protegido contra múltiplas chamadas simultâneas.
    /// </summary>
    private IEnumerator DeathCoroutine()
    {
        // Garante que a morte não seja iniciada duas vezes
        if (isDead)
            yield break;

        isDead = true;

        // Trava movimento — zero velocidade
        rb.linearVelocity = Vector2.zero;

        // Se morreu durante o dash, restaura gravidade sem chamar EndDash
        // (EndDash restauraria storedVerticalVelocity, permitindo movimento pós-morte)
        if (isDashing)
        {
            isDashing = false;
            rb.gravityScale = originalGravityScale;
        }

        // Desliga parâmetros de animação conflitantes
        if (anim != null)
        {
            anim.SetBool("IsRun", false);
            anim.SetBool("IsJump", false);
            anim.SetBool("IsFalling", false);
            anim.SetBool("IsGrounded", false);
            anim.SetBool("PuloPressionado", false);
            anim.SetBool("PousoAlto", false);
        }

        // Dispara animação de morte (prioridade máxima via Any State)
        if (anim != null)
            anim.SetTrigger("Morrendo");

        // Espera a duração configurada no Inspector para a animação tocar
        yield return new WaitForSeconds(deathAnimationDuration);

        // Desativa física
        rb.simulated = false;

        // Desativa colisão
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Mostra tela de morte
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
            if (anim != null)
                anim.SetBool("PuloPressionado", false);

            // Se estávamos em uma queda, calcule a altura e decida tipo de pouso
            if (usarPousoPorAltura && isFallingStarted)
            {
                float landingY = rb != null ? rb.position.y : transform.position.y;
                float fallDistance = fallStartY - landingY;
                bool pousoAlto = fallDistance >= alturaMinimaPousoAlto;

                if (anim != null)
                {
                    anim.SetBool("PousoAlto", pousoAlto);
                    // Limpa o bool após breve tempo para não interferir no próximo pouso
                    if (clearPousoAltoCoroutine != null)
                        StopCoroutine(clearPousoAltoCoroutine);
                    clearPousoAltoCoroutine = StartCoroutine(ClearPousoAltoCoroutine(0.25f));
                }

                if (debugPouso)
                    Debug.Log($"[Pouso] Altura da queda: {fallDistance} | Tipo: {(pousoAlto ? "Alto" : "Normal")}");

                // reset
                isFallingStarted = false;
                fallStartY = 0f;
            }
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