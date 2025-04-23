using UnityEngine;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController2 : MonoBehaviour
{
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Animator animator;

    [Header("Basic Movement")]
    public float walkSpeed = 6.0f;
    public float acceleration = 10.0f;
    public float deceleration = 20.0f;
    public float airControl = 0.5f;
    public float rotationSpeed = 15.0f;

    [Header("Jump Parameters")]
    public float jumpForce = 7.0f;
    public int maxJumps = 1;
    private int remainingJumps;

    [Header("Slope Parameters")]
    public float slopeLimit = 45f;
    public float slopeInfluence = 0.5f;
    public float downhillMultiplier = 1.3f;
    public float uphillMultiplier = 0.7f;
    public float slopeTransitionSmoothing = 10f;
    public float highSpeedThreshold = 10.0f;
    public float highSpeedSnapMultiplier = 2.0f;

    [Header("Ground Detection")]
    public LayerMask groundLayers;
    public float groundCheckDistance = 0.2f;
    public float coyoteTime = 0.15f;
    public float slopeRayLength = 1.5f;
    public float snapDistance = 0.5f;

    [Header("Power-up System")]
    public float powerUpDuration = 5.0f;
    public float speedPowerUpMultiplier = 1.5f;
    public float jumpPowerUpMultiplier = 1.5f;

    [Header("Smoothing Parameters")]
    public float movementSmoothing = 0.05f;
    public float velocityLerpSpeed = 20f;
    public float inertiaFactor = 0.95f;
    public float frictionlessDecelerationMultiplier = 0.2f;

    [Header("Crouch Parameters")]
    public float crouchHeightMultiplier = 0.5f;
    public float crouchSpeedMultiplier = 1.2f;
    public float crouchSpeedBuildUp = 2.0f;
    public float crouchSnapStrength = 15f;
    public float momentumPreservationFactor = 0.8f;
    
    [Header("Game Juice")]
    public ShakeData landingShakeData;
    public ShakeData flyingShakeData;
    // public GameObject speedLineParticle;

    [Header("Footstep Sounds")]
    public float footstepInterval = 0.5f; // Time between footstep sounds
    private float footstepTimer = 0f;
    

    private Vector3 moveDirection = Vector3.zero;
    private float currentSpeed = 0f;
    public bool isGrounded = false;
    private float lastGroundedTime = 0f;
    private Vector3 groundNormal = Vector3.up;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private float slopeAngle = 0f;
    private Vector3 smoothedMoveDirection = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 lastFrameVelocity = Vector3.zero;

    private bool hasSpeedPowerUp = false;
    private bool hasJumpPowerUp = false;
    private Coroutine speedPowerUpCoroutine;
    private Coroutine jumpPowerUpCoroutine;

    private int animSpeedHash;
    private int animGroundedHash;
    private int animJumpHash;
    private int animSlopeAngleHash;

    private float horizontalInput;
    private float verticalInput;
    private bool jumpPressed;
    private bool isCrouching = false;
    private float crouchCurrentSpeed = 0f;
    private bool wasGrounded; // Tracks the previous grounded state

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (animator != null)
        {
            animSpeedHash = Animator.StringToHash("Speed");
            animGroundedHash = Animator.StringToHash("Grounded");
            animJumpHash = Animator.StringToHash("Jump");
            animSlopeAngleHash = Animator.StringToHash("SlopeAngle");
        }

        remainingJumps = maxJumps;
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ActivateSkill();
        }

        if (isCrouching == false)
        {
            isCrouching = !isCrouching;
        }
        else AdjustColliderForCrouch(isCrouching);

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        DetectSlope();

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 targetDirection = forward * verticalInput + right * horizontalInput;

        if (isCrouching)
        {
            HandleCrouchMovement(targetDirection);
        }
        else
        {
            if (targetDirection.magnitude > 0.1f)
            {
                targetDirection.Normalize();
                float smoothingFactor = isGrounded ? (IsOnSlope() ? 8f : 12f) : 12f * airControl;
                smoothedMoveDirection = Vector3.Lerp(smoothedMoveDirection, targetDirection, Time.fixedDeltaTime * smoothingFactor);

                float targetSpeed = walkSpeed;
                if (hasSpeedPowerUp)
                {
                    targetSpeed *= speedPowerUpMultiplier;
                }

                float accelRate = isGrounded ? acceleration : acceleration * airControl;

                if (IsOnSlope())
                {
                    float slopeFactor = 1f - (slopeAngle / slopeLimit) * slopeInfluence;
                    float directionDot = Vector3.Dot(smoothedMoveDirection, Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized);
                    if (directionDot > 0.1f)
                    {
                        slopeFactor *= downhillMultiplier;
                    }
                    else if (directionDot < -0.1f)
                    {
                        slopeFactor *= Mathf.Lerp(uphillMultiplier, 1.0f, rb.linearVelocity.magnitude / walkSpeed);
                    }
                    targetSpeed *= slopeFactor;
                }

                currentSpeed = Mathf.Lerp(currentSpeed, Mathf.Max(targetSpeed, currentSpeed * momentumPreservationFactor), Time.fixedDeltaTime * accelRate);

                Quaternion targetRotation = Quaternion.LookRotation(smoothedMoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

                moveDirection = smoothedMoveDirection;
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * deceleration * frictionlessDecelerationMultiplier);
                smoothedMoveDirection = Vector3.Lerp(smoothedMoveDirection, Vector3.zero, Time.fixedDeltaTime * deceleration * frictionlessDecelerationMultiplier);
            }
        }

        if (jumpPressed && !isCrouching)
        {
            bool canCoyoteJump = Time.time - lastGroundedTime < coyoteTime;
            if (isGrounded || canCoyoteJump)
            {
                Jump();
            }
            else if (remainingJumps > 0)
            {
                Jump();
            }
            jumpPressed = false;
        }

        HandleMovement();
    }

    void CheckGrounded()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        RaycastHit hit;

        float sphereRadius = capsuleCollider.radius * 0.5f;
        float effectiveSnapDistance = snapDistance;

        if (IsOnSlope() && rb.linearVelocity.magnitude > highSpeedThreshold)
        {
            float downhillDot = Vector3.Dot(rb.linearVelocity.normalized, Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized);
            if (downhillDot > 0.1f)
            {
                effectiveSnapDistance *= highSpeedSnapMultiplier;
            }
        }

        if (Physics.SphereCast(rayStart, sphereRadius, Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayers))
        {
            if (!wasGrounded && rb.linearVelocity.y <= 0) // Check for landing
            {
                CameraShakerHandler.Shake(landingShakeData);
                AudioManager.Instance.PlaySFX("LandingSFX");
            }

            isGrounded = true;
            groundNormal = hit.normal;
            lastGroundedTime = Time.time;

            if (remainingJumps < maxJumps)
            {
                remainingJumps = maxJumps;
            }

            float distanceToGround = hit.distance - 0.1f;
            if (distanceToGround > 0 && distanceToGround < effectiveSnapDistance)
            {
                float snapStrength = isCrouching ? crouchSnapStrength : 10f;
                if (rb.linearVelocity.magnitude > highSpeedThreshold && IsOnSlope())
                {
                    snapStrength *= highSpeedSnapMultiplier;
                }
                Vector3 targetPosition = transform.position - Vector3.up * distanceToGround;
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * snapStrength);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            }
        }
        else
        {
            isGrounded = false;
            CameraShakerHandler.Shake(flyingShakeData);

            // Play flying sound once when becoming airborne
            // if (wasGrounded)
            // {
            //     AudioManager.Instance.PlaySFX("FlyingSFX");
            // }
        }

        if (!isGrounded)
        {
            if (Physics.SphereCast(transform.position + Vector3.up * 0.5f, sphereRadius, Vector3.down, out hit, groundCheckDistance + effectiveSnapDistance, groundLayers))
            {
                float distanceToGround = hit.distance - 0.5f;
                if (distanceToGround < effectiveSnapDistance)
                {
                    float snapStrength = isCrouching ? crouchSnapStrength : 10f;
                    if (rb.linearVelocity.magnitude > highSpeedThreshold && Vector3.Angle(hit.normal, Vector3.up) <= slopeLimit)
                    {
                        snapStrength *= highSpeedSnapMultiplier;
                    }
                    Vector3 targetPosition = new Vector3(transform.position.x, hit.point.y + capsuleCollider.height / 2, transform.position.z);
                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * snapStrength);

                    if (!wasGrounded && rb.linearVelocity.y <= 0)
                    {
                        CameraShakerHandler.Shake(landingShakeData);
                        AudioManager.Instance.PlaySFX("LandingSFX");
                    }

                    isGrounded = true;
                    groundNormal = hit.normal;
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                }
            }
        }

        // Footstep sound logic
        if (isGrounded && rb.linearVelocity.magnitude > 0.1f) // Check if grounded and moving
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                AudioManager.Instance.PlayRandomFootstep();
                footstepTimer = footstepInterval; // Reset timer
            }
        }
        else
        {
            footstepTimer = 0; // Reset timer when not grounded or not moving
        }

        wasGrounded = isGrounded; // Update previous grounded state
    }

    void DetectSlope()
    {
        if (!isGrounded)
        {
            slopeAngle = 0f;
            return;
        }

        slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDirection, out hit, slopeRayLength, groundLayers))
        {
            float forwardSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            slopeAngle = Mathf.Lerp(slopeAngle, forwardSlopeAngle, Time.deltaTime * slopeTransitionSmoothing);
        }
    }

    bool IsOnSlope()
    {
        if (!isGrounded) return false;
        return slopeAngle > 0 && slopeAngle <= slopeLimit;
    }

    void HandleCrouchMovement(Vector3 targetDirection)
    {
        if (targetDirection.magnitude > 0.1f)
        {
            targetDirection.Normalize();
            smoothedMoveDirection = Vector3.Lerp(smoothedMoveDirection, targetDirection, Time.fixedDeltaTime * 12f);

            float targetSpeed = walkSpeed * crouchSpeedMultiplier;
            if (hasSpeedPowerUp)
            {
                targetSpeed *= speedPowerUpMultiplier;
            }

            if (IsOnSlope())
            {
                float downhillDot = Vector3.Dot(smoothedMoveDirection, Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized);
                if (downhillDot > 0.1f)
                {
                    targetSpeed *= downhillMultiplier;
                }
            }

            crouchCurrentSpeed = Mathf.MoveTowards(crouchCurrentSpeed, targetSpeed, Time.fixedDeltaTime * crouchSpeedBuildUp);
            currentSpeed = crouchCurrentSpeed;

            Quaternion targetRotation = Quaternion.LookRotation(smoothedMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            moveDirection = smoothedMoveDirection;
        }
        else
        {
            crouchCurrentSpeed = Mathf.Lerp(crouchCurrentSpeed, 0, Time.fixedDeltaTime * deceleration);
            currentSpeed = crouchCurrentSpeed;
            smoothedMoveDirection = Vector3.Lerp(smoothedMoveDirection, Vector3.zero, Time.fixedDeltaTime * deceleration);
        }
    }

    void HandleMovement()
    {
        Vector3 targetVelocity = moveDirection * currentSpeed;

        if (IsOnSlope())
        {
            Vector3 slopeMovement = Vector3.ProjectOnPlane(targetVelocity, groundNormal);
            targetVelocity = slopeMovement;

            float downhillDot = Vector3.Dot(targetVelocity.normalized, Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized);
            float speedFactor = 1.0f;

            if (downhillDot > 0.1f)
            {
                speedFactor = 1.0f + (downhillDot * (downhillMultiplier - 1.0f));
                if (rb.linearVelocity.magnitude > highSpeedThreshold)
                {
                    Vector3 downwardForce = -groundNormal * (rb.linearVelocity.magnitude * 0.5f * highSpeedSnapMultiplier);
                    rb.AddForce(downwardForce, ForceMode.Acceleration);
                }
            }
            else if (downhillDot < -0.1f)
            {
                speedFactor = Mathf.Lerp(1.0f, uphillMultiplier, slopeAngle / slopeLimit);
                float momentum = rb.linearVelocity.magnitude / walkSpeed;
                speedFactor = Mathf.Max(speedFactor, momentum * 0.8f);
            }

            targetVelocity *= speedFactor;
        }

        if (moveDirection.magnitude < 0.1f && isGrounded)
        {
            Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            if (currentHorizontalVelocity.magnitude > 0.1f)
            {
                targetVelocity = currentHorizontalVelocity * inertiaFactor;
            }
        }

        targetVelocity.y = rb.linearVelocity.y;

        if (isGrounded)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                Vector3 targetHorizontalVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z);

                Vector3 newHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetHorizontalVelocity, Time.fixedDeltaTime * velocityLerpSpeed);

                rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);
            }
            else
            {
                Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

                if (currentHorizontalVelocity.magnitude > 0.1f)
                {
                    Vector3 deceleratedVelocity = currentHorizontalVelocity * (1.0f - (Time.fixedDeltaTime * frictionlessDecelerationMultiplier));
                    rb.linearVelocity = new Vector3(deceleratedVelocity.x, rb.linearVelocity.y, deceleratedVelocity.z);
                }
            }
        }
        else
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 airVelocity = new Vector3(
                    Mathf.Lerp(rb.linearVelocity.x, targetVelocity.x, Time.fixedDeltaTime * airControl * velocityLerpSpeed),
                    rb.linearVelocity.y,
                    Mathf.Lerp(rb.linearVelocity.z, targetVelocity.z, Time.fixedDeltaTime * airControl * velocityLerpSpeed)
                );

                rb.linearVelocity = airVelocity;
            }
        }

        CancelFriction();
    }

    void CancelFriction()
    {
        if (!isGrounded || moveDirection.magnitude > 0.1f)
            return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 lastHorizontalVelocity = new Vector3(lastFrameVelocity.x, 0, lastFrameVelocity.z);

        if (horizontalVelocity.magnitude < lastHorizontalVelocity.magnitude * inertiaFactor)
        {
            Vector3 restoredVelocity = lastHorizontalVelocity * inertiaFactor;
            rb.linearVelocity = new Vector3(restoredVelocity.x, rb.linearVelocity.y, restoredVelocity.z);
        }
    }

    void AdjustColliderForCrouch(bool crouch)
    {
        if (crouch)
        {
            capsuleCollider.height = originalColliderHeight * crouchHeightMultiplier;
            capsuleCollider.center = originalColliderCenter + Vector3.down * (originalColliderHeight * (1f - crouchHeightMultiplier) / 2f);
        }
        else
        {
            capsuleCollider.height = originalColliderHeight;
            capsuleCollider.center = originalColliderCenter;
            currentSpeed = crouchCurrentSpeed * momentumPreservationFactor;
            crouchCurrentSpeed = 0f;
        }
    }

    void Jump()
    {
        float jumpMultiplier = hasJumpPowerUp ? jumpPowerUpMultiplier : 1.0f;
        float finalJumpForce = jumpForce * jumpMultiplier;

        if (IsOnSlope())
        {
            Vector3 slopeJumpDirection = Vector3.Lerp(Vector3.up, groundNormal, 0.5f).normalized;

            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            rb.linearVelocity = horizontalVelocity + (slopeJumpDirection * finalJumpForce);
        }
        else
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            rb.linearVelocity = horizontalVelocity + (Vector3.up * finalJumpForce);
        }

        remainingJumps--;

        if (animator != null)
        {
            animator.SetTrigger(animJumpHash);
        }
    }

    void UpdateAnimator()
    {
        if (animator != null)
        {
            float normalizedSpeed = rb.linearVelocity.magnitude / walkSpeed;
            animator.SetFloat(animSpeedHash, normalizedSpeed);
            animator.SetBool(animGroundedHash, isGrounded);
        }
    }

    void ActivateSkill()
    {
        if (!hasSpeedPowerUp)
        {
            ActivateSpeedPowerUp();
        }
        else if (!hasJumpPowerUp)
        {
            ActivateJumpPowerUp();
        }
    }

    void ActivateSpeedPowerUp()
    {
        if (speedPowerUpCoroutine != null)
        {
            StopCoroutine(speedPowerUpCoroutine);
        }

        speedPowerUpCoroutine = StartCoroutine(SpeedPowerUpRoutine());
    }

    IEnumerator SpeedPowerUpRoutine()
    {
        hasSpeedPowerUp = true;
        yield return new WaitForSeconds(powerUpDuration);
        hasSpeedPowerUp = false;
    }

    void ActivateJumpPowerUp()
    {
        if (jumpPowerUpCoroutine != null)
        {
            StopCoroutine(jumpPowerUpCoroutine);
        }

        jumpPowerUpCoroutine = StartCoroutine(JumpPowerUpRoutine());
    }

    IEnumerator JumpPowerUpRoutine()
    {
        hasJumpPowerUp = true;
        remainingJumps = maxJumps;
        yield return new WaitForSeconds(powerUpDuration);
        hasJumpPowerUp = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SpeedPowerUp"))
        {
            ActivateSpeedPowerUp();
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("JumpPowerUp"))
        {
            ActivateJumpPowerUp();
            Destroy(other.gameObject);
        }
    }

    public float GetCurrentSpeed()
    {
        return rb.linearVelocity.magnitude;
    }

    public void ApplyExternalForce(Vector3 force, ForceMode forceMode = ForceMode.Impulse)
    {
        rb.AddForce(force, forceMode);
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, groundNormal * 1.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, moveDirection * slopeRayLength);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * (groundCheckDistance + 0.1f), capsuleCollider.radius * 0.5f);
        }
    }
}