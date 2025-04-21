using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Animator animator;

    [Header("Basic Movement")]
    public float walkSpeed = 6.0f;
    public float runSpeed = 12.0f;
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
    public float slopeTransitionSmoothing = 5f;

    [Header("Slide Parameters")]
    public float slideSpeed = 15.0f;
    public float slideCooldown = 1.0f;
    public float slideDecayRate = 0.8f;
    public float minSpeedForSlide = 4.0f;
    public float slideHeight = 0.5f;
    public float normalHeight = 2.0f;
    public float slopeSpeedMultiplier = 1.5f;
    public float maxSlopeAngle = 45f;
    public float slideTransitionSpeed = 10f;

    [Header("Ground Detection")]
    public LayerMask groundLayers;
    public float groundCheckDistance = 0.2f;
    public float coyoteTime = 0.15f;
    public float slopeRayLength = 1.5f;

    [Header("Power-up System")]
    public float powerUpDuration = 5.0f;
    public float speedPowerUpMultiplier = 1.5f;
    public float jumpPowerUpMultiplier = 1.5f;

    private Vector3 moveDirection = Vector3.zero;
    private float currentSpeed = 0f;
    private Vector3 slideDirection = Vector3.zero;
    private bool isSliding = false;
    private float nextSlideTime = 0f;
    private float slideTimeRemaining = 0f;
    private bool isGrounded = false;
    private float lastGroundedTime = 0f;
    private Vector3 groundNormal = Vector3.up;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private float targetColliderHeight;
    private Vector3 targetColliderCenter;
    private float slopeAngle = 0f;

    private bool hasSpeedPowerUp = false;
    private bool hasJumpPowerUp = false;
    private Coroutine speedPowerUpCoroutine;
    private Coroutine jumpPowerUpCoroutine;

    private int animSpeedHash;
    private int animGroundedHash;
    private int animJumpHash;
    private int animSlideHash;
    private int animSlopeAngleHash;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;
        targetColliderHeight = originalColliderHeight;
        targetColliderCenter = originalColliderCenter;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (animator != null)
        {
            animSpeedHash = Animator.StringToHash("Speed");
            animGroundedHash = Animator.StringToHash("Grounded");
            animJumpHash = Animator.StringToHash("Jump");
            animSlideHash = Animator.StringToHash("Sliding");
            animSlopeAngleHash = Animator.StringToHash("SlopeAngle");
        }

        remainingJumps = maxJumps;
    }

    void Update()
    {
        CheckGrounded();
        DetectSlope();
        HandleInput();
        UpdateColliderShape();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void CheckGrounded()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;

        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayers))
        {
            isGrounded = true;
            groundNormal = hit.normal;
            lastGroundedTime = Time.time;

            if (remainingJumps < maxJumps)
            {
                remainingJumps = maxJumps;
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    void DetectSlope()
    {
        if (!isGrounded)
        {
            slopeAngle = 0f;
            return;
        }

        slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

        // Check for slopes ahead
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDirection, out hit, slopeRayLength, groundLayers))
        {
            float forwardSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            // Blend current slope with upcoming slope for smoother transitions
            slopeAngle = Mathf.Lerp(slopeAngle, forwardSlopeAngle, Time.deltaTime * slopeTransitionSmoothing);
        }
    }

    bool IsOnSlope()
    {
        if (!isGrounded) return false;
        return slopeAngle > 0 && slopeAngle <= slopeLimit;
    }

    void HandleInput()
    {
        // Use Camera.main instead of custom camera
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 targetDirection = forward * Input.GetAxis("Vertical") + right * Input.GetAxis("Horizontal");

        if (targetDirection.magnitude > 0.1f && !isSliding)
        {
            targetDirection.Normalize();

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

            if (hasSpeedPowerUp)
            {
                targetSpeed *= speedPowerUpMultiplier;
            }

            float accelRate = isGrounded ? acceleration : acceleration * airControl;

            // Adjust speed for slopes
            if (IsOnSlope())
            {
                float slopeFactor = 1f - (slopeAngle / slopeLimit) * slopeInfluence;

                // Check if moving uphill or downhill
                float directionDot = Vector3.Dot(targetDirection, Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized);

                if (directionDot > 0.1f) // Moving downhill
                {
                    slopeFactor *= downhillMultiplier;
                }
                else if (directionDot < -0.1f) // Moving uphill
                {
                    slopeFactor *= uphillMultiplier;
                }

                targetSpeed *= slopeFactor;
            }

            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * accelRate);

            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            moveDirection = targetDirection;
        }
        else if (!isSliding)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime * deceleration);
        }

        if (Input.GetKeyDown(KeyCode.Space))
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
        }

        if (Input.GetKeyDown(KeyCode.C) && Time.time > nextSlideTime && currentSpeed > minSpeedForSlide && isGrounded)
        {
            StartSlide();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ActivateSkill();
        }
    }

    void HandleMovement()
    {
        if (isSliding)
        {
            slideDirection = Vector3.Lerp(slideDirection, slideDirection * slideDecayRate, Time.deltaTime);
            Vector3 slideVelocity = slideDirection;

            if (IsOnSlope())
            {
                Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                float slopeInfluence = slopeAngle / maxSlopeAngle;

                slideVelocity += slopeDirection * slopeSpeedMultiplier * slopeInfluence;
            }

            Vector3 verticalVel = new Vector3(0, rb.linearVelocity.y, 0);
            rb.linearVelocity = slideVelocity + verticalVel;

            slideTimeRemaining -= Time.deltaTime;

            if (slideTimeRemaining <= 0 || slideDirection.magnitude < 1.0f)
            {
                EndSlide();
            }
        }
        else
        {
            Vector3 targetVelocity = moveDirection * currentSpeed;

            if (IsOnSlope())
            {
                Vector3 slopeMovement = Vector3.ProjectOnPlane(targetVelocity, groundNormal);
                Vector3 slopeCorrectionVelocity = slopeMovement - targetVelocity;
                targetVelocity = slopeMovement;

                // Gradually adjust to the slope-projected velocity for smoother transitions
                Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                targetVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * slopeTransitionSmoothing);

                float downhillDot = Vector3.Dot(targetVelocity.normalized, Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized);
                if (downhillDot > 0.1f)
                {
                    float speedFactor = 1.0f + (downhillDot * (slopeSpeedMultiplier - 1.0f));
                    targetVelocity *= speedFactor;

                    if (slopeAngle > 20f && currentSpeed > minSpeedForSlide && !isSliding)
                    {
                        StartSlide();
                        return;
                    }
                }
            }

            targetVelocity.y = rb.linearVelocity.y;

            Vector3 velocityChange = targetVelocity - rb.linearVelocity;
            velocityChange.y = 0;

            if (isGrounded)
            {
                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            else
            {
                rb.AddForce(velocityChange * airControl, ForceMode.VelocityChange);
            }

            if (!isGrounded)
            {
                rb.AddForce(Physics.gravity, ForceMode.Acceleration);
            }
        }
    }

    void Jump()
    {
        float jumpMultiplier = hasJumpPowerUp ? jumpPowerUpMultiplier : 1.0f;
        float finalJumpForce = jumpForce * jumpMultiplier;

        if (IsOnSlope())
        {
            Vector3 slopeJumpDirection = Vector3.Lerp(Vector3.up, groundNormal, 0.5f).normalized;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(slopeJumpDirection * finalJumpForce, ForceMode.Impulse);
        }
        else
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0;
            rb.linearVelocity = velocity;
            rb.AddForce(Vector3.up * finalJumpForce, ForceMode.Impulse);
        }

        remainingJumps--;

        if (animator != null)
        {
            animator.SetTrigger(animJumpHash);
        }
    }

    void StartSlide()
    {
        isSliding = true;
        slideDirection = transform.forward * slideSpeed;
        slideTimeRemaining = 1.0f;

        targetColliderHeight = slideHeight;
        targetColliderCenter = new Vector3(0, slideHeight / 2, 0);

        Vector3 currentVel = rb.linearVelocity;
        currentVel.y = 0;
        rb.linearVelocity = slideDirection + new Vector3(0, rb.linearVelocity.y, 0);

        if (animator != null)
        {
            animator.SetBool(animSlideHash, true);
        }
    }

    void EndSlide()
    {
        isSliding = false;
        nextSlideTime = Time.time + slideCooldown;

        targetColliderHeight = originalColliderHeight;
        targetColliderCenter = originalColliderCenter;

        if (animator != null)
        {
            animator.SetBool(animSlideHash, false);
        }
    }

    void UpdateColliderShape()
    {
        if (capsuleCollider.height != targetColliderHeight || capsuleCollider.center != targetColliderCenter)
        {
            capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetColliderHeight, Time.deltaTime * slideTransitionSpeed);
            capsuleCollider.center = Vector3.Lerp(capsuleCollider.center, targetColliderCenter, Time.deltaTime * slideTransitionSpeed);
        }
    }

    void UpdateAnimator()
    {
        if (animator != null)
        {
            float normalizedSpeed = rb.linearVelocity.magnitude / walkSpeed;

            animator.SetFloat(animSpeedHash, normalizedSpeed);
            animator.SetBool(animGroundedHash, isGrounded);
            animator.SetBool(animSlideHash, isSliding);

            if (animSlopeAngleHash != 0)
            {
                animator.SetFloat(animSlopeAngleHash, slopeAngle);
            }
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
        Debug.Log("Speed PowerUp Activated!");

        hasSpeedPowerUp = true;
        yield return new WaitForSeconds(powerUpDuration);
        hasSpeedPowerUp = false;

        Debug.Log("Speed PowerUp Ended");
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
        Debug.Log("Jump PowerUp Activated!");

        hasJumpPowerUp = true;
        remainingJumps = maxJumps;
        yield return new WaitForSeconds(powerUpDuration);
        hasJumpPowerUp = false;

        Debug.Log("Jump PowerUp Ended");
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

    void OnCollisionEnter(Collision collision)
    {
        if (isSliding && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 reflectDir = Vector3.Reflect(slideDirection.normalized, contact.normal);

            slideDirection = reflectDir * slideDirection.magnitude * 0.8f;
        }
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, groundNormal * 1.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, moveDirection * slopeRayLength);
        }
    }
}