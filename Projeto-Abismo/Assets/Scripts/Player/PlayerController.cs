using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour, IDamageable
{
    // =============================================
    // MOVIMENTO
    // =============================================

    [Header("Movimento")]
    private Animator anim;

    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 30f;

    // =============================================
    // PULO
    // =============================================

    [Header("Pulo")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float jumpHoldForce = 25f;
    [SerializeField] private float jumpHoldTime = 0.25f;
    [SerializeField] private bool useHeightBasedJump = true;
    [SerializeField] private float jumpMaxHeight = 2.5f;

    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private string wallTag = "Wall";
    [SerializeField] private LayerMask wallLayer = 0;

    // =============================================
    // POUSO POR ALTURA
    // =============================================

    [Header("Pouso por Altura")]
    [SerializeField] private bool usarPousoPorAltura = true;
    [SerializeField] private float alturaMinimaPousoAlto = 3f;
    [SerializeField] private bool debugPouso = false;

    private float fallStartY = 0f;
    private bool isFallingStarted = false;

    private Coroutine clearPousoAltoCoroutine = null;

    // =============================================
    // ATAQUE
    // =============================================

    [Header("Ataque")]
    [SerializeField] private float attackOffsetX = 1.6f;
    [SerializeField] private float attackOffsetY = 0.4f;
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private KeyCode attackKey = KeyCode.F;

    // =============================================
    // DASH
    // =============================================

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;

    // =============================================
    // ÁUDIO
    // =============================================

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip attackSound;

    // =============================================
    // VIDA
    // =============================================

    [Header("Vida")]
    [SerializeField] private int maxLife = 5;
    [SerializeField] private float invincibilityTime = 0.3f;

    private int currentLife;
    private bool isInvincible;

    public int CurrentLife => currentLife;
    public int MaxLife => maxLife;

    // =============================================
    // INVENCIBILIDADE POR HABILIDADE
    // =============================================

    [Header("Habilidade - Invencibilidade")]

    [SerializeField]
    private KeyCode teclaInvencibilidade = KeyCode.G;

    [SerializeField]
    private float duracaoInvencibilidade = 3f;

    [SerializeField]
    private bool debugInvencibilidade = true;

    private bool invencibilidadeAtiva;

    private Coroutine invencibilidadeCoroutine;

    public bool InvencibilidadeAtiva => invencibilidadeAtiva;

    // =============================================
    // MORTE
    // =============================================

    [Header("Animação de Morte")]
    [SerializeField] private float deathAnimationDuration = 1.5f;

    private bool isDead = false;

    // =============================================
    // LAMPIÃO
    // =============================================

    [Header("Lampião")]
    [SerializeField] private Lampiao lampiao;

    public Lampiao Lampiao => lampiao;

    private float lastMoveDirection = 1f;

    public bool LuzAtiva { get; private set; }

    public void SetLuz(bool estado)
    {
        LuzAtiva = estado;
    }

    // =============================================
    // HABILIDADES
    // =============================================

    [Header("Habilidades")]
    [SerializeField] private PlayerAbilities playerAbilities;

    private PlayerAbilities abilities;

    private bool abilitiesAvailable = false;

    // =============================================
    // REFERÊNCIAS
    // =============================================

    private Rigidbody2D rb;

    private PlayerAttack playerAttack;

    // =============================================
    // INPUT
    // =============================================

    private float horizontalInput;

    private bool facingRight = true;

    // =============================================
    // ESTADO DO PULO
    // =============================================

    private bool isJumping;

    private float jumpTimeCounter;

    private float jumpStartY;

    private bool isGrounded;

    private bool isTouchingWall;

    private bool isHoldingJumpInput;

    // =============================================
    // ESTADO DO DASH
    // =============================================

    private bool isDashing;

    private float dashTimeLeft;

    private float lastDashTime;

    private Vector2 dashDirection;

    private float originalGravityScale;

    private float storedVerticalVelocity;

    // =============================================
    // ESTADO DO ATAQUE
    // =============================================

    private float lastAttackTime;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        anim = GetComponentInChildren<Animator>();

        if (anim == null)
        {
            Debug.LogError(
                "[PlayerController] Animator NÃO encontrado!"
            );
        }

        // -----------------------------------------
        // ÁUDIO
        // -----------------------------------------

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource =
                    gameObject.AddComponent<AudioSource>();
            }
        }

        // -----------------------------------------
        // FÍSICA
        // -----------------------------------------

        if (rb != null)
        {
            originalGravityScale =
                rb.gravityScale;
        }

        // -----------------------------------------
        // VIDA
        // -----------------------------------------

        currentLife = maxLife;

        // -----------------------------------------
        // ATAQUE
        // -----------------------------------------

        playerAttack =
            GetComponent<PlayerAttack>();

        if (playerAttack == null)
        {
            playerAttack =
                gameObject.AddComponent<PlayerAttack>();
        }

        // -----------------------------------------
        // LAMPIÃO
        // -----------------------------------------

        if (lampiao == null)
        {
            lampiao =
                GetComponentInChildren<Lampiao>();

            if (lampiao == null)
            {
                Debug.LogError(
                    "[PlayerController] Lampião NÃO encontrado!"
                );
            }
        }

        // -----------------------------------------
        // PLAYER ABILITIES
        // -----------------------------------------

        abilities =
            playerAbilities != null
                ? playerAbilities
                : GetComponent<PlayerAbilities>();

        abilitiesAvailable =
            abilities != null;

        if (!abilitiesAvailable)
        {
            Debug.LogError(
                "[PlayerController] PlayerAbilities NÃO encontrado!"
            );
        }
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (isDead)
            return;

        GetInput();

        HandleFlip();

        HandleJumpInput();

        DetectFallStart();

        HandleAttack();

        HandleDash();

        HandleLampiao();

        HandleInvencibilidade();

        HandleAnimations();
    }

    // =============================================
    // FIXED UPDATE
    // =============================================

    private void FixedUpdate()
    {
        if (isDead)
            return;

        // -----------------------------------------
        // DASH
        // -----------------------------------------

        if (isDashing)
        {
            rb.linearVelocity =
                new Vector2(
                    dashDirection.x * dashSpeed,
                    0f
                );

            return;
        }

        // -----------------------------------------
        // PAREDE
        // -----------------------------------------

        if (
            isTouchingWall &&
            !isGrounded &&
            rb.linearVelocity.y > 0f
        )
        {
            rb.linearVelocity =
                new Vector2(
                    rb.linearVelocity.x,
                    0f
                );
        }

        HandleMovement();

        HandleChargedJumpPhysics();
    }

    // =============================================
    // INPUT
    // =============================================

    private void GetInput()
    {
        horizontalInput =
            PlayerInputHandler.Instance != null
                ? PlayerInputHandler.Instance.Horizontal()
                : Input.GetAxisRaw("Horizontal");

        if (horizontalInput > 0.1f)
        {
            lastMoveDirection = 1f;
        }
        else if (horizontalInput < -0.1f)
        {
            lastMoveDirection = -1f;
        }
    }

    // =============================================
    // MOVIMENTO
    // =============================================

    private void HandleMovement()
    {
        float targetSpeed =
            horizontalInput * maxSpeed;

        float accelRate =
            Mathf.Abs(targetSpeed) > 0.01f
                ? acceleration
                : deceleration;

        float newVelocityX =
            Mathf.MoveTowards(
                rb.linearVelocity.x,
                targetSpeed,
                accelRate *
                Time.fixedDeltaTime
            );

        rb.linearVelocity =
            new Vector2(
                newVelocityX,
                rb.linearVelocity.y
            );
    }

    // =============================================
    // PULO
    // =============================================

    private void HandleJumpInput()
    {
        if (isDead || isDashing)
            return;

        var input =
            PlayerInputHandler.Instance;

        bool jumpDown =
            input != null
                ? input.PularDown()
                : Input.GetKeyDown(KeyCode.Space);

        isHoldingJumpInput =
            input != null
                ? input.PularHeld()
                : Input.GetKey(KeyCode.Space);

        bool jumpUp =
            input != null
                ? input.PularUp()
                : Input.GetKeyUp(KeyCode.Space);

        bool hasNormalJump =
            abilitiesAvailable &&
            abilities.Has(SkillType.Jump);

        bool hasChargedJump =
            abilitiesAvailable &&
            abilities.Has(SkillType.ChargedJump);

        bool canJump =
            hasNormalJump ||
            hasChargedJump;

        // -----------------------------------------
        // INÍCIO DO PULO
        // -----------------------------------------

        if (
            jumpDown &&
            isGrounded &&
            !isTouchingWall &&
            canJump
        )
        {
            isGrounded = false;

            isJumping = true;

            jumpTimeCounter =
                jumpHoldTime;

            jumpStartY =
                rb.position.y;

            rb.linearVelocity =
                new Vector2(
                    rb.linearVelocity.x,
                    jumpForce
                );

            PlaySound(jumpSound);

            if (anim != null)
            {
                anim.SetBool(
                    "PuloPressionado",
                    hasChargedJump
                );
            }

            return;
        }

        // -----------------------------------------
        // SOLTOU PULO
        // -----------------------------------------

        if (
            jumpUp &&
            isJumping
        )
        {
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity =
                    new Vector2(
                        rb.linearVelocity.x,
                        rb.linearVelocity.y * 0.4f
                    );
            }

            StopChargedJump();
        }
    }

    // =============================================
    // FÍSICA DO PULO PRESSIONADO
    // =============================================

    private void HandleChargedJumpPhysics()
    {
        if (!isJumping)
            return;

        if (!isHoldingJumpInput)
            return;

        bool hasChargedJump =
            abilitiesAvailable &&
            abilities.Has(
                SkillType.ChargedJump
            );

        if (!hasChargedJump)
            return;

        if (jumpTimeCounter <= 0f)
        {
            StopChargedJump();
            return;
        }

        if (useHeightBasedJump)
        {
            float alturaAtual =
                rb.position.y -
                jumpStartY;

            if (
                alturaAtual >=
                jumpMaxHeight
            )
            {
                StopChargedJump();
                return;
            }
        }

        rb.AddForce(
            Vector2.up *
            jumpHoldForce,
            ForceMode2D.Force
        );

        jumpTimeCounter -=
            Time.fixedDeltaTime;
    }

    private void StopChargedJump()
    {
        isJumping = false;

        if (anim != null)
        {
            anim.SetBool(
                "PuloPressionado",
                false
            );
        }
    }

    // =============================================
    // INVENCIBILIDADE
    // =============================================

    private void HandleInvencibilidade()
    {
        if (isDead)
            return;

        bool habilidadeDesbloqueada =
            abilitiesAvailable &&
            abilities != null &&
            abilities.Has(
                SkillType.Invincibility
            );

        // -----------------------------------------
        // HABILIDADE NÃO DESBLOQUEADA
        // -----------------------------------------

        if (!habilidadeDesbloqueada)
            return;

        // -----------------------------------------
        // APERTAR G
        // -----------------------------------------

        if (
            Input.GetKeyDown(
                teclaInvencibilidade
            )
        )
        {
            AtivarInvencibilidade();
        }
    }

    private void AtivarInvencibilidade()
    {
        if (isDead)
            return;

        // Não ativa novamente enquanto
        // já estiver ativa.

        if (invencibilidadeAtiva)
        {
            if (debugInvencibilidade)
            {
                Debug.Log(
                    "[PLAYER] 🛡️ Invencibilidade já está ativa."
                );
            }

            return;
        }

        if (
            invencibilidadeCoroutine != null
        )
        {
            StopCoroutine(
                invencibilidadeCoroutine
            );
        }

        invencibilidadeCoroutine =
            StartCoroutine(
                InvencibilidadeTemporaria()
            );
    }

    private IEnumerator InvencibilidadeTemporaria()
    {
        invencibilidadeAtiva = true;

        if (debugInvencibilidade)
        {
            Debug.Log(
                "[PLAYER] 🛡️ INVENCIBILIDADE ATIVADA | Duração: "
                + duracaoInvencibilidade
                + " segundos"
            );
        }

        yield return new WaitForSeconds(
            duracaoInvencibilidade
        );

        invencibilidadeAtiva = false;

        invencibilidadeCoroutine = null;

        if (debugInvencibilidade)
        {
            Debug.Log(
                "[PLAYER] 🛡️ INVENCIBILIDADE TERMINOU."
            );
        }
    }

    // =============================================
    // QUEDA
    // =============================================

    private void DetectFallStart()
    {
        if (!usarPousoPorAltura)
            return;

        if (
            isGrounded ||
            isFallingStarted ||
            isDashing
        )
            return;

        if (
            rb != null &&
            rb.linearVelocity.y < -0.1f
        )
        {
            isFallingStarted = true;

            fallStartY =
                rb.position.y;

            if (debugPouso)
            {
                Debug.Log(
                    $"[Pouso] Iniciou queda em Y={fallStartY}"
                );
            }
        }
    }

    private IEnumerator ClearPousoAltoCoroutine(
        float delay
    )
    {
        yield return new WaitForSeconds(
            delay
        );

        if (anim != null)
        {
            anim.SetBool(
                "PousoAlto",
                false
            );
        }

        clearPousoAltoCoroutine = null;
    }

    // =============================================
    // FLIP
    // =============================================

    private void HandleFlip()
    {
        if (
            lastMoveDirection > 0 &&
            !facingRight
        )
        {
            Flip();
        }
        else if (
            lastMoveDirection < 0 &&
            facingRight
        )
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale =
            transform.localScale;

        scale.x *= -1;

        transform.localScale =
            scale;
    }

    public bool IsFacingRight()
    {
        return facingRight;
    }

    // =============================================
    // LAMPIÃO
    // =============================================

    private void HandleLampiao()
    {
        if (lampiao == null)
            return;

        var input =
            PlayerInputHandler.Instance;

        bool lampiaoInput =
            input != null
                ? input.LampiaoDown()
                : Input.GetKeyDown(
                    lampiao.ToggleLightKey
                );

        if (lampiaoInput)
        {
            lampiao.ToggleLuzExterno();
        }

        if (!lampiao.IsLightOn)
            return;

        bool alternarInput =
            input != null
                ? input.AlternarModoDown()
                : Input.GetKeyDown(
                    lampiao.AlternarModoKey
                );

        if (alternarInput)
        {
            lampiao.AlternarModo();
        }

        bool paralisarInput =
            input != null
                ? input.ParalisarDown()
                : Input.GetKeyDown(
                    lampiao.ParalisarKey
                );

        if (paralisarInput)
        {
            lampiao.AtivarParalisar();
        }
    }

    // =============================================
    // ATAQUE
    // =============================================

    private void HandleAttack()
    {
        if (isDead)
            return;

        var input =
            PlayerInputHandler.Instance;

        bool atacou =
            input != null
                ? input.AtacarDown()
                : Input.GetKeyDown(
                    attackKey
                ) ||
                Input.GetMouseButtonDown(0);

        if (
            atacou &&
            Time.time >=
            lastAttackTime +
            attackCooldown
        )
        {
            PerformAttack();

            lastAttackTime =
                Time.time;

            PlaySound(attackSound);

            if (anim != null)
            {
                anim.ResetTrigger(
                    "Attack"
                );

                anim.SetTrigger(
                    "Attack"
                );
            }
        }
    }

    private void PerformAttack()
    {
        bool attackRight =
            lastMoveDirection > 0;

        playerAttack.PerformAttack(
            attackRight,
            new Vector2(
                attackOffsetX,
                attackOffsetY
            )
        );
    }

    // =============================================
    // DASH
    // =============================================

    private void HandleDash()
    {
        if (isDead)
            return;

        var input =
            PlayerInputHandler.Instance;

        bool dashInput =
            input != null
                ? input.DashDown()
                : Input.GetKeyDown(
                    dashKey
                );

        if (
            dashInput &&
            Time.time >=
            lastDashTime +
            dashCooldown &&
            !isDashing
        )
        {
            if (
                !abilitiesAvailable ||
                !abilities.Has(
                    SkillType.Dash
                )
            )
            {
                return;
            }

            float dir =
                Mathf.Abs(horizontalInput) >
                0.1f
                    ? horizontalInput
                    : lastMoveDirection;

            dashDirection =
                new Vector2(
                    dir,
                    0f
                );

            isDashing = true;

            dashTimeLeft =
                dashDuration;

            lastDashTime =
                Time.time;

            storedVerticalVelocity =
                rb.linearVelocity.y;

            rb.gravityScale = 0f;

            rb.linearVelocity =
                new Vector2(
                    dashDirection.x *
                    dashSpeed,
                    0f
                );
        }

        if (isDashing)
        {
            dashTimeLeft -=
                Time.deltaTime;

            if (
                dashTimeLeft <= 0f
            )
            {
                EndDash();
            }
        }
    }

    private void EndDash()
    {
        isDashing = false;

        rb.gravityScale =
            originalGravityScale;

        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,
                storedVerticalVelocity
            );
    }

    // =============================================
    // ÁUDIO
    // =============================================

    private void PlaySound(
        AudioClip clip
    )
    {
        if (
            clip != null &&
            audioSource != null
        )
        {
            audioSource.PlayOneShot(
                clip
            );
        }
    }

    // =============================================
    // ANIMAÇÕES
    // =============================================

    private void HandleAnimations()
    {
        if (
            isDead ||
            anim == null
        )
        {
            return;
        }

        float velX =
            Mathf.Abs(
                rb.linearVelocity.x
            );

        float velY =
            rb.linearVelocity.y;

        anim.SetBool(
            "IsRun",
            velX > 0.1f &&
            isGrounded &&
            !isDashing
        );

        anim.SetBool(
            "IsJump",
            velY > 0.1f &&
            !isGrounded
        );

        anim.SetBool(
            "IsFalling",
            velY < -0.1f &&
            !isGrounded
        );

        anim.SetBool(
            "IsGrounded",
            isGrounded
        );
    }

    // =============================================
    // VIDA / DANO
    // =============================================

    public void TakeDamage(
        int damage,
        GameObject source
    )
    {
        // -----------------------------------------
        // PLAYER MORTO
        // -----------------------------------------

        if (isDead)
            return;

        // -----------------------------------------
        // INVENCIBILIDADE DA HABILIDADE
        // -----------------------------------------

        if (invencibilidadeAtiva)
        {
            if (debugInvencibilidade)
            {
                string fonte =
                    source != null
                        ? source.name
                        : "NULL";

                Debug.Log(
                    "[PLAYER] 🛡️ DANO BLOQUEADO | Fonte: "
                    + fonte
                    + " | Dano: "
                    + damage
                );
            }

            return;
        }

        // -----------------------------------------
        // INVENCIBILIDADE NORMAL APÓS DANO
        // -----------------------------------------

        if (isInvincible)
        {
            Debug.LogWarning(
                "[PLAYER] Dano ignorado: invencibilidade temporária."
            );

            return;
        }

        // -----------------------------------------
        // DEBUG
        // -----------------------------------------

        string sourceName =
            source != null
                ? source.name
                : "NULL";

        string sourceTag =
            source != null
                ? source.tag
                : "NULL";

        string sourceLayer =
            source != null
                ? LayerMask.LayerToName(
                    source.layer
                )
                : "NULL";

        Vector3 sourcePos =
            source != null
                ? source.transform.position
                : Vector3.zero;

        string stack =
            System.Environment.StackTrace;

        Debug.Log(
            $"[DANO] {sourceName} | {sourceTag} | {sourceLayer} | {sourcePos} | {damage}\n{stack}"
        );

        // -----------------------------------------
        // APLICA DANO
        // -----------------------------------------

        currentLife -= damage;

        Debug.Log(
            "[PLAYER] Vida atual: "
            + currentLife
        );

        // -----------------------------------------
        // MORTE
        // -----------------------------------------

        if (currentLife <= 0)
        {
            currentLife = 0;

            Die();

            return;
        }

        // -----------------------------------------
        // ANIMAÇÃO DE DANO
        // -----------------------------------------

        if (anim != null)
        {
            anim.SetTrigger(
                "Dano"
            );
        }

        // -----------------------------------------
        // INVENCIBILIDADE NORMAL
        // -----------------------------------------

        StartCoroutine(
            InvincibilityCoroutine()
        );
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        yield return new WaitForSeconds(
            invincibilityTime
        );

        isInvincible = false;
    }

    // =============================================
    // MORTE
    // =============================================

    private void Die()
    {
        StartCoroutine(
            DeathCoroutine()
        );
    }

    private IEnumerator DeathCoroutine()
    {
        if (isDead)
            yield break;

        isDead = true;

        // -----------------------------------------
        // DESATIVA INVENCIBILIDADE
        // -----------------------------------------

        invencibilidadeAtiva = false;

        if (
            invencibilidadeCoroutine != null
        )
        {
            StopCoroutine(
                invencibilidadeCoroutine
            );

            invencibilidadeCoroutine = null;
        }

        // -----------------------------------------
        // PARA MOVIMENTO
        // -----------------------------------------

        rb.linearVelocity =
            Vector2.zero;

        // -----------------------------------------
        // FINALIZA DASH
        // -----------------------------------------

        if (isDashing)
        {
            isDashing = false;

            rb.gravityScale =
                originalGravityScale;
        }

        // -----------------------------------------
        // ANIMAÇÕES
        // -----------------------------------------

        if (anim != null)
        {
            anim.SetBool(
                "IsRun",
                false
            );

            anim.SetBool(
                "IsJump",
                false
            );

            anim.SetBool(
                "IsFalling",
                false
            );

            anim.SetBool(
                "IsGrounded",
                false
            );

            anim.SetBool(
                "PuloPressionado",
                false
            );

            anim.SetBool(
                "PousoAlto",
                false
            );

            anim.ResetTrigger(
                "Dano"
            );

            anim.SetTrigger(
                "Morrendo"
            );
        }

        // -----------------------------------------
        // ESPERA ANIMAÇÃO DE MORTE
        // -----------------------------------------

        yield return new WaitForSeconds(
            deathAnimationDuration
        );

        // -----------------------------------------
        // DESATIVA FÍSICA
        // -----------------------------------------

        rb.simulated = false;

        Collider2D col =
            GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = false;
        }

        // -----------------------------------------
        // TELA DE MORTE
        // -----------------------------------------

        if (
            DeathScreen.instance != null
        )
        {
            DeathScreen.instance
                .MostrarTelaMorte();
        }
        else
        {
            Debug.LogError(
                "DeathScreen NULL"
            );
        }
    }

    // =============================================
    // COLISÕES
    // =============================================

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        // -----------------------------------------
        // CHÃO
        // -----------------------------------------

        if (
            collision.gameObject.CompareTag(
                groundTag
            )
        )
        {
            if (
                rb.linearVelocity.y >
                0.1f
            )
            {
                return;
            }

            isGrounded = true;

            StopChargedJump();

            // -------------------------------------
            // POUSO POR ALTURA
            // -------------------------------------

            if (
                usarPousoPorAltura &&
                isFallingStarted
            )
            {
                float landingY =
                    rb != null
                        ? rb.position.y
                        : transform.position.y;

                float fallDistance =
                    fallStartY -
                    landingY;

                bool pousoAlto =
                    fallDistance >=
                    alturaMinimaPousoAlto;

                if (anim != null)
                {
                    anim.SetBool(
                        "PousoAlto",
                        pousoAlto
                    );

                    if (
                        clearPousoAltoCoroutine != null
                    )
                    {
                        StopCoroutine(
                            clearPousoAltoCoroutine
                        );
                    }

                    clearPousoAltoCoroutine =
                        StartCoroutine(
                            ClearPousoAltoCoroutine(
                                0.25f
                            )
                        );
                }

                if (debugPouso)
                {
                    Debug.Log(
                        $"[Pouso] Distância: {fallDistance} | Tipo: {(pousoAlto ? "Alto" : "Normal")}"
                    );
                }

                isFallingStarted = false;

                fallStartY = 0f;
            }
        }

        // -----------------------------------------
        // PAREDE
        // -----------------------------------------

        if (
            collision.gameObject.CompareTag(
                wallTag
            )
        )
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
            if (
                rb.linearVelocity.y >
                0.1f
            )
            {
                return;
            }

            foreach (
                ContactPoint2D contact
                in collision.contacts
            )
            {
                if (
                    contact.normal.y >
                    0.7f
                )
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

    // =============================================
    // PROPRIEDADE DO DASH
    // =============================================

    public bool IsDashing => isDashing;
}