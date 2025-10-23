using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace NewMovement
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Player : MonoBehaviour
    {
        // compute animator hash at game startup instead of every frame
        private static readonly int AnimatorSpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int RunAnimSpeedMultiplier = Animator.StringToHash("AnimSpeedMultiplier");

        [Header("Dependencies")]
        public Rigidbody2D rb;
        [FormerlySerializedAs("collider")] public BoxCollider2D boxCollider;
        public Animator anim;
        public SpriteRenderer spriteRenderer;

        [Header("Movement variables")]
        public float rotationSpeed = 360;
        public float acceleration = 20f;
        public float deceleration = 30f;
        public float minSpeedToBrake = 2f;
        public float friction = 10f;
        public float slope = 5f;
        public float topSpeed = 10f;
        public float maxSpeed = 15f;
        public float minSpeedToSlide = 2f;
        public float minAngleToSlide = 30f;
        public float minAngleToFall = 60f;
        public float airAcceleration = 10f;
        public float gravity = 25f;
        public float maxJumpHeight = 4f;
        public float minJumpHeight = 1f;
        public float jumpImpulse = 12f;
        public float jumpTime = .2f;

        [Header("Misc settings")]
        public bool active = true;
        public bool ignoreGravity;
        public bool disableCollision;
        public bool rotateToGround = true;
        public bool allowCeilingReaquisition;
        public float minCeilingAngle = 45f;

        [Header("Animation speed settings")]
        public float minRunAnimationMultiplier = 1f;
        public float maxRunAnimationMultiplier = 2f;

        [Header("Layer masks")]
        public LayerMask groundLayer;
        public LayerMask ceilingLayer;
        public LayerMask wallLayer;
        public LayerMask enemyLayer; // Added enemy layer

        [Header("Collider settings")]
        public Bounds bounds;

        [Serializable]
        public class Bounds
        {
            public float safeMargin = 0.01f;
            public float snapDistance = 0.1f;
            public float height = 0.5f;
            public Vector2 size = new Vector2(0.5f, 1f);
            public Vector2 extents => size / 2f;
        }

        public float direction { get; set; }
        [NonSerialized]
        public Vector2 velocity;
        public Vector2 previousVelocity { get; private set; }
        private Vector2 _ceilingNormal;
        private Vector2 _wallNormal;
        private Vector2 previousPosition;
        public bool grounded { get; set; }

        public PlayerStateMachine state;
        public PlayerInput input;

        // Jump variables
        private bool isJumping = false;
        private float jumpTimeCounter = 0f;
        private bool jumpPressed = false;
        private bool jumpHeld = false;
        private bool jumpReleased = false;
        private bool lastJumpState = false;

        // these are just for convenience
        public Vector2 position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public float xPos
        {
            get => position.x;
            set => position = new Vector2(value, yPos);
        }

        public float yPos
        {
            get => position.y;
            set => position = new Vector2(xPos, value);
        }

        public Vector2 up
        {
            get => transform.up;
            set
            {
                Vector3 targetForward = Vector3.forward;
                Vector3 targetUp = value.normalized;
                targetRotation = Quaternion.LookRotation(targetForward, targetUp);
            }
        }

        private Quaternion targetRotation;

        public Vector2 right => transform.right;
        public Vector2 ceilingNormal => _ceilingNormal;
        public Vector2 wallNormal => _wallNormal;

        public bool canJump => jumpTimer == 0.0f && grounded;
        public float jumpTimer { get; set; }

        // validate is called in editor
        private void OnValidate()
        {
            rb ??= GetComponent<Rigidbody2D>();
            boxCollider ??= GetComponent<BoxCollider2D>();
            anim ??= GetComponent<Animator>();
            spriteRenderer ??= GetComponent<SpriteRenderer>();
        }

        // reset is called in editor when component is added
        private void Reset() => OnValidate();

        private void Awake()
        {
            state = new PlayerStateMachine(this);

            // Get references to state components and add them to the state machine
            NormalState normalState = GetComponent<NormalState>();
            normalState.Initialize(state, this);
            state.AddState(typeof(NormalState), normalState);

            JumpState jumpState = GetComponent<JumpState>();
            jumpState.Initialize(state, this);
            state.AddState(typeof(JumpState), jumpState);

            FallState fallState = GetComponent<FallState>();
            fallState.Initialize(state, this);
            state.AddState(typeof(FallState), fallState);

            BrakeState brakeState = GetComponent<BrakeState>();
            brakeState.Initialize(state, this);
            state.AddState(typeof(BrakeState), brakeState);

            state.ChangeState<NormalState>();

            // deactivate unity physics because we process collisions ourselves
            rb.isKinematic = true;
            boxCollider.isTrigger = true;
            boxCollider.size = bounds.size;
            boxCollider.offset = new Vector2(0.0f, bounds.height);

            // Initialize input if not assigned
            if (input == null)
                input = new PlayerInput();
        }

        private void Update()
        {
            // Update inputs
            input.InputUpdate();
            UpdateJumpInput();

            if (!active)
                return;
            var deltaTime = Time.deltaTime; // you can use Time.smoothDeltaTime instead
            PhysicsUpdate(deltaTime);
            UpdateAnimation();
            transform.rotation = Quaternion.RotateTowards(
              transform.rotation,
              targetRotation,
              rotationSpeed * Time.deltaTime
            );
        }

        // Handle jump input separately
        private void UpdateJumpInput()
        {
            bool currentJump = Input.GetButton("Jump");
            jumpPressed = currentJump && !lastJumpState;
            jumpHeld = currentJump;
            jumpReleased = !currentJump && lastJumpState;
            lastJumpState = currentJump;
        }

        private void UpdateAnimation()
        {
            anim.SetFloat(AnimatorSpeedHash, Mathf.Abs(velocity.x));
            anim.SetBool(IsJumpingHash, !grounded);
            anim.SetFloat(RunAnimSpeedMultiplier, Remap(Mathf.Abs(velocity.x), 0f, topSpeed, minRunAnimationMultiplier, maxRunAnimationMultiplier));
        }

        private static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        private void PhysicsUpdate(float deltaTime)
        {
            state.UpdateState();

            // Handle jump input
            HandleJump(deltaTime);

            velocity = Vector2.ClampMagnitude(velocity, maxSpeed); // Limit max velocity

            ProcessVelocity(deltaTime);

            if (disableCollision)
                return;
            _ceilingNormal.x = 0.0f; // Reset collision normals
            _ceilingNormal.y = 0.0f;
            _wallNormal.x = 0.0f;
            _wallNormal.y = 0.0f;
            ProcessHorizontalCollision(); // Check for wall collisions
            ProcessVerticalCollision(); // Check for ground/ceiling collisions
        }

        // ADDED: Jump handling
        private void HandleJump(float deltaTime)
        {
            // Update jump timer
            if (jumpTimer > 0)
                jumpTimer -= deltaTime;

            // Start jump
            if (jumpPressed && canJump)
            {
                velocity.y = jumpImpulse;
                isJumping = true;
                jumpTimeCounter = jumpTime;
                ExitGround();
            }

            // Variable height jump
            if (jumpHeld && isJumping)
            {
                if (jumpTimeCounter > 0)
                {
                    velocity.y = jumpImpulse;
                    jumpTimeCounter -= deltaTime;
                }
                else
                {
                    isJumping = false;
                }
            }

            // Cut jump short if button released
            if (jumpReleased && isJumping)
            {
                isJumping = false;
                jumpTimeCounter = 0;
                if (velocity.y > 0)
                    velocity.y *= 0.5f; // Cut jump velocity in half
            }
        }

        private void ProcessVelocity(float deltaTime)
        {
            previousVelocity = velocity;
            previousPosition = position;
            if (grounded)
            {
                // Apply velocity along the ground normal
                position += velocity * deltaTime;
            }
            else
                position += velocity * deltaTime;
        }

        private void ProcessHorizontalCollision()
        {
            if (velocity.x == 0.0)
                return;
            Vector2 vector2 = right * Mathf.Sign(velocity.x); // Get direction based on velocity
            Vector2 origin = position + up * bounds.height; // Calculate raycast origin at top of bounds
            float num1 = bounds.extents.x + Mathf.Max(bounds.safeMargin, 0.01f); // Calculate collision margin
            float num2 = Mathf.Abs(position.x - previousPosition.x); // Get movement delta
            Vector2 direction = vector2;
            RaycastHit2D raycastHit2D = default;
            ref RaycastHit2D local = ref raycastHit2D;
            float distance = num1 + num2; // Total raycast distance

            // FIXED: Added enemy layer to collision check
            LayerMask collisionMask = wallLayer | enemyLayer;

            if (!RaycastHelper.Raycast(origin, direction, out local, distance, collisionMask)) // Cast ray to detect walls
                return;
            if (!raycastHit2D.collider.enabled || Vector2.Dot(raycastHit2D.normal, velocity) > 0.0) // Skip if moving away from wall
                return;

            // Handle enemy collision differently - bounce back
            if (((1 << raycastHit2D.collider.gameObject.layer) & enemyLayer) != 0)
            {
                velocity.x = -velocity.x * 0.5f; // Bounce back from enemy
                position = raycastHit2D.point - up * bounds.height + raycastHit2D.normal * (num1 + 0.1f); // Add extra space
                return;
            }

            if (grounded)
            {
                if (Vector2.Dot(raycastHit2D.normal, up) < 0.7f) // Check if wall is steep enough while grounded
                {
                    velocity.x = 0.0f; // Stop horizontal movement
                    position = raycastHit2D.point - up * bounds.height + raycastHit2D.normal * num1; // Adjust position
                }
            }
            else
            {
                velocity.x = 0.0f; // Stop horizontal movement when in air
                position = raycastHit2D.point - up * bounds.height + raycastHit2D.normal * num1; // Adjust position
            }
            _wallNormal.x = raycastHit2D.normal.x; // Store wall normal for physics calculations
            _wallNormal.y = raycastHit2D.normal.y;
        }

        private void ProcessVerticalCollision()
        {
            if (grounded)
            {
                Vector2 point;
                Vector2 normal;
                Collider2D other;
                bool snap;
                // Check if player is still in contact with ground
                if (GetGroundContact(-up, groundLayer, out point, out normal, out other, out snap) && velocity.y <= 0.0)
                {
                    if (!other.enabled || velocity.y > 0.0)
                        return;
                    velocity.y = 0.0f;
                    if (!snap)
                        return;
                    up = rotateToGround ? normal : Vector2.up; // Align player with ground slope
                    position = point - up * bounds.height + up * bounds.extents.y; // Snap to ground
                }
                else
                    ExitGround(); // No ground contact - transition to air
            }
            else
            {
                // Check for ceiling collisions when moving upward
                Vector2 direction = up * Mathf.Sign(velocity.y);
                Vector2 point1;
                Vector2 normal1;
                Collider2D other1;
                bool snap;
                if (velocity.y > 0.0 && GetGroundContact(direction, ceilingLayer, out point1, out normal1, out other1, out snap))
                {
                    if (other1.enabled && velocity.y > 0.0)
                    {
                        if (allowCeilingReaquisition && Vector2.Angle(normal1, Vector2.up) < (double)minCeilingAngle)
                        {
                            EnterGround(normal1);
                        }
                        else
                        {
                            velocity.y = 0.0f; // Stop upward movement when hitting ceiling
                            yPos = point1.y - bounds.extents.y - bounds.height; // Adjust position to avoid ceiling penetration
                        }
                        // Store ceiling normal vector for physics calculations
                        _ceilingNormal.x = normal1.x;
                        _ceilingNormal.y = normal1.y;
                    }
                }

                // Check for ground contact when moving downward
                Vector2 point2;
                Vector2 normal2;
                Collider2D other2;
                // Skip if moving upward or no ground detected or surface normal points down
                if (velocity.y >= 0.0 || !GetGroundContact(direction, groundLayer, out point2, out normal2, out other2, out snap) || normal2.y <= 0.0)
                    return;
                if (!other2.enabled || velocity.y >= 0.0)
                    return;
                EnterGround(normal2); // Transition to grounded state
                yPos = point2.y + bounds.extents.y - bounds.height; // Snap to ground surface
            }
        }

        // Checks for ground contact by casting rays from both sides of the player
        private bool GetGroundContact(
          Vector2 direction,
          LayerMask layer,
          out Vector2 point,
          out Vector2 normal,
          out Collider2D other,
          out bool snap)
        {
            // Calculate ray origins at left and right edges of player
            Vector2 origin1 = position - right * bounds.extents.x + up * bounds.height;
            Vector2 origin2 = position + right * bounds.extents.x + up * bounds.height;

            // Calculate ray distance based on movement and grounded state
            float num = !grounded ? Mathf.Abs(position.y - previousPosition.y) : 0.0f;
            float distance1 = bounds.extents.y + (grounded ? bounds.snapDistance : 0.0f) + num;

            // Cast rays from both sides
            RaycastHit2D hit;
            bool flag1 = RaycastHelper.Raycast(origin1, direction, out hit, distance1, layer);
            Vector2 direction1 = direction;
            RaycastHit2D b = default;
            ref RaycastHit2D local = ref b;
            double distance2 = distance1;
            LayerMask layer1 = layer;
            bool flag2 = RaycastHelper.Raycast(origin2, direction1, out local, (float)distance2, layer1);

            // Initialize output parameters
            point = normal = Vector2.zero;
            snap = flag1 & flag2;
            other = null;

            if (flag1 & flag2)
            {
                // Both rays hit - get closest hit point
                RaycastHit2D closestHit = RaycastHelper.GetClosestHit(position, hit, b);
                other = closestHit.collider;

                // Check if surface normals are similar enough to snap
                if (Vector2.Dot(hit.normal, b.normal) >= 0.8f)
                {
                    point = (hit.point + b.point) / 2f;
                    normal = (hit.normal + b.normal) / 2f;
                }
                else
                {
                    point = closestHit.point;
                    normal = closestHit.normal;
                    snap = false;
                }
            }
            else if (flag1 | flag2)
            {
                // Only one ray hit - use that hit info
                RaycastHit2D raycastHit2D = hit.collider != null ? hit : b;
                point = raycastHit2D.point;
                normal = raycastHit2D.normal;
                other = raycastHit2D.collider;
            }
            return flag1 | flag2;
        }

        public void EnterGround(Vector2 normal)
        {
            if (grounded)
                return;
            up = rotateToGround ? normal : Vector2.up; // Align to ground normal if enabled
            velocity.x = velocity.x * transform.up.y + velocity.y * -transform.up.x; // Convert velocity to ground-relative 
            FlipCharacter(velocity.x); // Face movement direction
            velocity.y = 0.0f; // Zero out vertical velocity
            grounded = true;
            isJumping = false; // Reset jump state
            jumpTimeCounter = 0f;
        }

        public void ExitGround()
        {
            if (!grounded)
                return;
            Vector2 velocity = this.velocity;
            this.velocity.x = velocity.x * up.y + velocity.y * up.x;
            this.velocity.y = velocity.y * up.y - velocity.x * up.x;
            up = Vector2.up;
            grounded = false;
        }

        public float GetAngle() => Mathf.Acos(up.y) * 57.29578f;

        public void FlipCharacter(float dir)
        {
            if (dir == 0.0)
                return;
            spriteRenderer.flipX = dir < 0.0;
            direction = spriteRenderer.flipX ? -1f : 1f;
        }

        public void ApplyAcceleration(float deltaTime)
        {
            float num = grounded ? acceleration : airAcceleration; // Use different acceleration on ground vs air
            if (input.horizontal > 0.0 && velocity.x <= topSpeed)
            {
                velocity.x += num * deltaTime; // Accelerate right
                velocity.x = Mathf.Min(velocity.x, topSpeed); // Clamp to max speed
            }
            else
            {
                if (input.horizontal >= 0.0 || velocity.x < -topSpeed)
                    return;
                velocity.x -= num * deltaTime; // Accelerate left
                velocity.x = Mathf.Max(velocity.x, -topSpeed); // Clamp to max speed
            }
        }

        // Applies deceleration when changing direction to simulate momentum and improve control feel
        public void ApplyDeceleration(float deltaTime)
        {
            // Skip if: not grounded, no input, or moving in same direction as input
            if (!grounded || input.horizontal == 0.0 || Math.Abs(Mathf.Sign(velocity.x) - Mathf.Sign(input.horizontal)) < .001f)
                return;
            if (velocity.x > 0.0) // Moving right
            {
                velocity.x -= deceleration * deltaTime; // Apply deceleration
                if (velocity.x > 0.0)
                    return;
                velocity.x = -0.1f; // Small boost in opposite direction when stopping
            }
            else // Moving left
            {
                if (velocity.x >= 0.0)
                    return;
                velocity.x += deceleration * deltaTime; // Apply deceleration
                if (velocity.x < 0.0)
                    return;
                velocity.x = 0.1f; // Small boost in opposite direction when stopping
            }
        }

        public void ApplyFriction(float deltaTime)
        {
            if (!grounded || input.horizontal != 0.0)
                return;
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, (friction) * deltaTime);
        }

        public void ApplyGravity(float deltaTime)
        {
            if (grounded || ignoreGravity)
                return;
            velocity.y -= gravity * deltaTime;
        }

        public void ApplySlopeFactor(float deltaTime)
        {
            if (!grounded) // Skip if not on ground
                return;
            // Apply slope factor based on the ground's angle
            velocity.x -= slope * up.x * deltaTime;
            velocity.y -= slope * up.y * deltaTime;
        }

        public void ApplyFall()
        {
            if (!grounded || Mathf.Abs(velocity.x) > minSpeedToSlide || GetAngle() < minAngleToSlide)
                return;
            if (GetAngle() >= minAngleToFall)
                ExitGround();
        }

        public void LockJump(float playerJumpTime) => jumpTimer = playerJumpTime;
    }
}