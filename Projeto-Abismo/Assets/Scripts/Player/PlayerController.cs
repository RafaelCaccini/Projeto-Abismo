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
    [SerializeField] private float postDashFallDelay = 0.25f;

    [Header("Camera Target")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSmooth = 5f;
    [SerializeField] private float followSpeed = 10f;

    [Header("Life")]
    [SerializeField] private int maxLife = 5;
    private int currentLife;

    private Rigidbody2D rb;

    private float horizontalInput;
    private bool isJumping;
    private float jumpTimeCounter;

    private bool isSuperJumping;
    private float superJumpTimeCounter;
    private float lastSuperJumpTime;

    private bool isGrounded;
    private bool isTouchingWall;

    private bool facingRight = true;
    private float lastAttackTime;

    private bool isDashing;
    private float dashTimeLeft;
    private float lastDashTime;
    private Vector2 dashDirection;
    private float originalGravityScale;
    private float storedVerticalVelocity;
    private RigidbodyConstraints2D originalConstraints;

    private bool isAwaitingFall;
    private float fallDelayTimer;

    // CAMERA
    private float currentLookAhead;
    private bool lastFacingRight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentLife = maxLife;
        originalGravityScale = rb.gravityScale;
        originalConstraints = rb.constraints;

        lastFacingRight = facingRight; // 👈 AQUI
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

    void LateUpdate()
    {
        UpdateCameraTarget();
    }

    void FixedUpdate()
    {
        if (isDashing || isAwaitingFall)
        {
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

        if (ctrlHeld && Input.GetKeyDown(KeyCode.Space) && isGrounded && !isTouchingWall && Time.time >= lastSuperJumpTime + superJumpCooldown)
        {
            isSuperJumping = true;
            superJumpTimeCounter = superJumpHoldTime;
            lastSuperJumpTime = Time.time;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, superJumpForce);
            isGrounded = false;
        }

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

    // 🎥 CAMERA TARGET SYSTEM
    void UpdateCameraTarget()
    {
        if (cameraTarget == null) return;

        // Detecta mudança de direção
        if (facingRight != lastFacingRight)
        {
            currentLookAhead = 0f;
            lastFacingRight = facingRight;
        }

        float targetLookAhead = facingRight ? lookAheadDistance : -lookAheadDistance;

        currentLookAhead = Mathf.Lerp(
            currentLookAhead,
            targetLookAhead,
            lookAheadSmooth * Time.deltaTime
        );

        Vector3 targetPosition = transform.position + new Vector3(currentLookAhead, 0f, 0f);

        cameraTarget.position = Vector3.Lerp(
            cameraTarget.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
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
        float directionMultiplier = facingRight ? 1f : -1f;

        Vector3 spawnPosition = transform.position
            + new Vector3(attackOffsetX * directionMultiplier, attackOffsetY, 0f);

        GameObject block = Instantiate(attackBlockPrefab, spawnPosition, Quaternion.identity);
        block.transform.localScale = transform.localScale;
    }

    void HandleDash()
    {
        if (Input.GetKeyDown(dashKey) && Time.time >= lastDashTime + dashCooldown && !isDashing && !isAwaitingFall)
        {
            float dir = Mathf.Abs(horizontalInput) > 0.1f ? horizontalInput : (facingRight ? 1f : -1f);
            dashDirection = new Vector2(Mathf.Sign(dir), 0f);

            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;

            storedVerticalVelocity = 0f;
            rb.constraints = originalConstraints | RigidbodyConstraints2D.FreezePositionY;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, 0f);
        }

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
                isAwaitingFall = true;
                fallDelayTimer = postDashFallDelay;
            }
        }

        if (isAwaitingFall)
        {
            fallDelayTimer -= Time.deltaTime;
            if (fallDelayTimer <= 0f)
            {
                EndDash();
            }
        }
    }

    void EndDash()
    {
        isDashing = false;
        isAwaitingFall = false;

        rb.constraints = originalConstraints;
        rb.gravityScale = originalGravityScale;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, storedVerticalVelocity);
    }

    public void TakeDamage(int damage)
    {
        currentLife -= damage;

        if (currentLife <= 0)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
            isGrounded = true;

        if (collision.gameObject.CompareTag(wallTag))
            isTouchingWall = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
            isGrounded = false;

        if (collision.gameObject.CompareTag(wallTag))
            isTouchingWall = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
            isGrounded = true;

        if (collision.gameObject.CompareTag(wallTag))
            isTouchingWall = true;
    }
    public bool IsFacingRight()
    {
        return facingRight;
    }
}