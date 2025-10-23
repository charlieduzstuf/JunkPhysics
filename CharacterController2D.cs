using UnityEngine;

public class CharacterController2D : MonoBehaviour
{
    // Movement Settings
    [SerializeField] private float maxSpeed = 10f;          // Maximum running speed
    [SerializeField] private float acceleration = 25f;      // How fast speed increases
    [SerializeField] private float airControl = 15f;        // Air movement control
    [SerializeField] private float friction = 20f;          // Slowdown when no input
    [SerializeField] private float gravity = 20f;           // Strong gravity for quick falls
    [SerializeField] private float slopeSpeedBonus = 5f;    // Speed boost or reduction on slopes
    [SerializeField] private float slideThreshold = 45f;    // Angle where sliding begins
    [SerializeField] private float slideSpeed = 8f;         // Speed when sliding down slopes

    // Jump Settings
    [SerializeField] private float jumpForce = 15f;         // Jump height

    // Ground Check
    [SerializeField] private LayerMask groundLayer;         // Layers that count as ground
    [SerializeField] private float groundCheckRadius = 0.2f;// Radius for ground detection
    [SerializeField] private Transform groundCheck;         // Position to check ground

    // Visuals
    [SerializeField] private Transform graphicsHolder;      // Sprite holder (kept upright)
    [SerializeField] private Animator animator;             // Optional animator

    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;
    private bool facingRight = true;
    private Vector2 slopeNormal = Vector2.up;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;  // Disable default gravity for manual control
        rb.freezeRotation = true;  // Prevent physics-based rotation

        if (!groundCheck) Debug.LogError("GroundCheck Transform is missing!");
        if (!graphicsHolder) Debug.LogError("GraphicsHolder Transform is missing!");
        if (!animator) animator = graphicsHolder?.GetComponent<Animator>();
    }

    void Update()
    {
        // Get input
        moveInput = Input.GetAxisRaw("Horizontal");

        // Handle jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isGrounded = false;
        }

        // Update animator
        if (animator)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalSpeed", rb.velocity.y);
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();
        Move();
        ApplyGravity();
    }

    void CheckGrounded()
    {
        // Use circle cast for reliable ground detection
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isGrounded = hit != null;

        if (isGrounded)
        {
            // Get slope normal from the collider
            RaycastHit2D rayHit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.2f, groundLayer);
            slopeNormal = rayHit.collider ? rayHit.normal : Vector2.up;
        }
        else
        {
            slopeNormal = Vector2.up;
        }
    }

    void Move()
    {
        Vector2 velocity = rb.velocity;
        float slopeAngle = Vector2.Angle(Vector2.up, slopeNormal);
        Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x).normalized;

        if (isGrounded)
        {
            // Calculate slope effect on speed
            float slopeFactor = 0f;
            if (slopeAngle > 0)
            {
                // Adjust slope direction based on facing
                if (facingRight && slopeDirection.x < 0) slopeDirection = -slopeDirection;
                else if (!facingRight && slopeDirection.x > 0) slopeDirection = -slopeDirection;

                // Speed up downhill, slow uphill
                slopeFactor = slopeSpeedBonus * (facingRight ? slopeDirection.x : -slopeDirection.x);
            }

            // Ground movement
            float targetSpeed = (moveInput * maxSpeed) + slopeFactor;
            float currentSpeed = velocity.x;

            if (Mathf.Abs(moveInput) > 0.1f)
            {
                // Accelerate towards target speed
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
            }
            else if (slopeAngle > slideThreshold)
            {
                // Slide down steep slopes when idle
                currentSpeed = slopeDirection.x * slideSpeed * (slopeAngle > 90 ? -1 : 1);
            }
            else
            {
                // Apply friction to slow down, but don't set to 0 immediately
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, friction * Time.fixedDeltaTime);
            }

            velocity.x = currentSpeed;
        }
        else
        {
            // Air movement
            float targetSpeed = moveInput * maxSpeed;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, airControl * Time.fixedDeltaTime);
        }

        rb.velocity = velocity;

        // Flip character based on movement direction
        if ((moveInput > 0 && !facingRight) || (moveInput < 0 && facingRight))
            Flip();
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            // Apply gravity, ensuring vertical velocity doesn't exceed a reasonable limit
            // This prevents the player from getting stuck when moving upwards against gravity
            rb.velocity += Vector2.down * gravity * Time.fixedDeltaTime;
            if (rb.velocity.y < -20f) // Cap falling speed
            {
                rb.velocity = new Vector2(rb.velocity.x, -20f);
            }
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = graphicsHolder.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1 : -1);
        graphicsHolder.localScale = scale;
    }

    // Visualize ground check in editor
    void OnDrawGizmos()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}