using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerInputActions playerInput;
    [SerializeField] private float speed;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float forwardSpeed;
    [SerializeField] private float laneSlideSpeed = 10f;

    [Header("Jump Speed Boost")]
    [SerializeField] private float jumpForwardSpeedMalt = 1.25f;
    private float baseForwardSpeed;
    private float baseJumpSpeed;
    private float baseJumpMalt;

    [Header("Speed Acceleration")]
    [SerializeField] private float accelerationRate = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Death Settings")]
    [SerializeField] private float deathPauseDuration = 1f;
    [SerializeField] private float deathFallForce = 15f;
    private float deathFallTimer = 0f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchDuration = 1f;
    [SerializeField] private float crouchYOffset = 0.5f;
    [SerializeField] private float airCrouchDropForce = 20f;

    [Header("Power-Up State")]
    private bool isInvulnerable = false;
    private Coroutine higherJumpCoroutine;
    private Coroutine invulnerabilityCoroutine;
    private float remainingJumpTime;
    private float remainingInvulnerabilityTime;

    private Rigidbody rb;
    private InputAction moveAction;
    private bool isGrounded;
    private bool wasAirborne = false;
    private int currentLane = 0;
    private float targetX = 0f;
    private bool isDead = false;
    private bool isCrouching = false;
    private float originalY;

    private CapsuleCollider capsuleCollider;
    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;
    private Vector3 originalScale;

    private Coroutine crouchCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = new PlayerInputActions();

        originalScale = transform.localScale;
        baseForwardSpeed = forwardSpeed;
        baseJumpSpeed = jumpSpeed;
        baseJumpMalt = jumpForwardSpeedMalt;

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            originalCapsuleHeight = capsuleCollider.height;
            originalCapsuleCenter = capsuleCollider.center;
        }
        else
        {
            Debug.LogWarning("No CapsuleCollider found on player!");
        }
    }

    private void OnEnable()
    {
        if (playerInput == null) return;
        playerInput.Player.Jump.performed += OnJump;
        playerInput.Player.Move.performed += OnMove;
        playerInput.Player.Crouch.performed += OnCrouch;
        moveAction = playerInput.Player.Move;
        playerInput.Enable();
    }

    private void OnDisable()
    {
        if (playerInput == null) return;
        playerInput.Player.Jump.performed -= OnJump;
        playerInput.Player.Move.performed -= OnMove;
        playerInput.Player.Crouch.performed -= OnCrouch;
        playerInput.Disable();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!isGrounded || isDead) return;

        if (isCrouching)
        {
            CancelCrouch();
            return;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
        rb.AddForce(new Vector3(0f, jumpSpeed * 0.4f, jumpSpeed), ForceMode.Impulse);
        forwardSpeed = baseForwardSpeed * jumpForwardSpeedMalt;
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (isDead) return;
        Vector2 input = ctx.ReadValue<Vector2>();
        if (input.x > 0.5f && currentLane < 1)
        {
            currentLane++;
            targetX = currentLane * 3f;
        }
        else if (input.x < -0.5f && currentLane > -1)
        {
            currentLane--;
            targetX = currentLane * 3f;
        }
    }

    private void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (isDead) return;

        if (!isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -airCrouchDropForce, rb.linearVelocity.z);
            return;
        }

        if (isCrouching)
        {
            CancelCrouch();
            return;
        }

        crouchCoroutine = StartCoroutine(CrouchSequence());
    }

    private void CancelCrouch()
    {
        if (crouchCoroutine != null)
            StopCoroutine(crouchCoroutine);

        transform.localScale = originalScale;
        rb.MovePosition(new Vector3(rb.position.x, originalY, rb.position.z));

        Animator animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("Crouch", false);

        isCrouching = false;
        crouchCoroutine = null;
    }

    private IEnumerator CrouchSequence()
    {
        isCrouching = true;

        Animator animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("Crouch", true);

        transform.localScale = new Vector3(
            originalScale.x,
            originalScale.y,
            originalScale.z * 0.5f
        );

        originalY = rb.position.y;
        rb.MovePosition(new Vector3(rb.position.x, originalY - crouchYOffset, rb.position.z));

        yield return new WaitForSeconds(crouchDuration);

        CancelCrouch();
    }

    // --- Power-Up Methods ---

    public void ApplyHigherJump(float duration, float multiplier)
    {
        if (higherJumpCoroutine != null)
        {
            StopCoroutine(higherJumpCoroutine);
            remainingJumpTime += duration; // extend duration, don't stack multiplier
        }
        else
        {
            remainingJumpTime = duration;
        }

        higherJumpCoroutine = StartCoroutine(HigherJumpSequence(remainingJumpTime, multiplier));
    }

    public void ApplyInvulnerability(float duration)
    {
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
            remainingInvulnerabilityTime += duration; // extend duration
        }
        else
        {
            remainingInvulnerabilityTime = duration;
        }

        invulnerabilityCoroutine = StartCoroutine(InvulnerabilitySequence(remainingInvulnerabilityTime));
    }

    private IEnumerator HigherJumpSequence(float duration, float multiplier)
    {
        // Always apply from base values so multiplier never stacks
        jumpSpeed = baseJumpSpeed * multiplier;
        jumpForwardSpeedMalt = baseJumpMalt * multiplier;

        Debug.Log("HigherJump STARTED");

        yield return new WaitForSeconds(duration);

        jumpSpeed = baseJumpSpeed;
        jumpForwardSpeedMalt = baseJumpMalt;
        higherJumpCoroutine = null;

        Debug.Log("HigherJump ENDED");
    }

    private IEnumerator InvulnerabilitySequence(float duration)
    {
        isInvulnerable = true;
        Debug.Log("Invulnerability STARTED");

        yield return new WaitForSeconds(duration);

        isInvulnerable = false;
        invulnerabilityCoroutine = null;

        Debug.Log("Invulnerability ENDED - duration expired");
    }

    // --- Core Loop ---

    private void FixedUpdate()
    {
        if (isDead) return;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        baseForwardSpeed += accelerationRate * Time.fixedDeltaTime;

        float scaledLaneSlideSpeed = laneSlideSpeed * (baseForwardSpeed / forwardSpeed);

        if (!isGrounded)
            wasAirborne = true;
        else if (wasAirborne && isGrounded)
        {
            forwardSpeed = baseForwardSpeed;
            wasAirborne = false;
        }

        if (!wasAirborne)
            forwardSpeed = baseForwardSpeed;

        float newX = Mathf.MoveTowards(rb.position.x, targetX, scaledLaneSlideSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(
            (newX - rb.position.x) / Time.fixedDeltaTime,
            isCrouching ? 0f : rb.linearVelocity.y,
            forwardSpeed
        );

        if (isCrouching)
            rb.MovePosition(new Vector3(newX, originalY - crouchYOffset, rb.position.z + forwardSpeed * Time.fixedDeltaTime));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") && !isDead)
        {
            if (isInvulnerable)
            {
                // Destroy all obstacles then end invulnerability immediately
                GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
                foreach (GameObject obstacle in obstacles)
                    Destroy(obstacle);

                if (invulnerabilityCoroutine != null)
                    StopCoroutine(invulnerabilityCoroutine);

                isInvulnerable = false;
                invulnerabilityCoroutine = null;

                Debug.Log("Invulnerability ENDED - obstacle hit, all obstacles cleared");
                return;
            }

            isDead = true;
            playerInput.Disable();
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        rb.linearVelocity = new Vector3(0f, 0f, 0f);
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;

        CameraController cam = Camera.main.GetComponent<CameraController>();
        if (cam != null)
            cam.TriggerDeathSequence();

        Animator animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("Death");

        yield return new WaitForSeconds(deathPauseDuration);

        GameManager.Instance.TriggerGameOver();
    }

    private void Update()
    {
        if (!isDead) return;
        if (deathFallTimer < deathPauseDuration)
        {
            deathFallTimer += Time.deltaTime;
            return;
        }
        rb.AddForce(new Vector3(0f, 0f, -deathFallForce), ForceMode.Acceleration);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}