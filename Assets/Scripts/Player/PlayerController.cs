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

    [Header("O2 Input Delay")]
    [SerializeField] private float maxInputDelay = 0.5f;    // delay at 1% O2
    [SerializeField] private PlayerHealth playerHealth;     // assign in Inspector

    [Header("Power-Up State")]
    private bool isInvulnerable = false;
    private bool isLaunched = false;
    private float launchLockedY = 0f;
    private bool launchYReady = false;
    private const float launchTargetY = 5f;
    private const float launchLiftSpeed = 8f;
    private Coroutine higherJumpCoroutine;
    private Coroutine invulnerabilityCoroutine;
    private Coroutine launchCoroutine;
    private float remainingJumpTime;
    private float remainingInvulnerabilityTime;
    private bool obstacleCollisionIgnored = false;

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

    // -------------------------------------------------------------------
    // O2 Input Queue
    // Inputs are stamped with a fireAt time and stored in a fixed circular
    // buffer. DrainInputQueue() is called from Update() every frame.
    // No coroutines, no allocations at any O2 level after startup.
    // -------------------------------------------------------------------

    private struct PendingInput
    {
        public float fireAt;      // Time.time when this action should execute
        public byte type;        // 0=jump  1=move  2=crouch
        public Vector2 moveValue;   // only meaningful when type==1
    }

    private const int QUEUE_CAPACITY = 16;
    private PendingInput[] inputQueue = new PendingInput[QUEUE_CAPACITY];
    private int queueHead = 0;
    private int queueCount = 0;

    // Delay in seconds at the current O2 level.
    // 100% O2 -> 0s   |   1% O2 -> ~0.495s
    private float InputDelay =>
        playerHealth == null ? 0f : (1f - playerHealth.HPPercent) * maxInputDelay;

    private void EnqueueInput(byte type, Vector2 moveValue = default)
    {
        if (queueCount >= QUEUE_CAPACITY) return;   // full — drop newest input

        int tail = (queueHead + queueCount) % QUEUE_CAPACITY;
        inputQueue[tail] = new PendingInput
        {
            fireAt = Time.time + InputDelay,     // snapshot delay at press time
            type = type,
            moveValue = moveValue
        };
        queueCount++;
    }

    private void DrainInputQueue()
    {
        while (queueCount > 0)
        {
            ref PendingInput next = ref inputQueue[queueHead];
            if (Time.time < next.fireAt) break;     // queue is time-ordered so we can stop here

            switch (next.type)
            {
                case 0: ExecuteJump(); break;
                case 1: ExecuteMove(next.moveValue); break;
                case 2: ExecuteCrouch(); break;
            }

            queueHead = (queueHead + 1) % QUEUE_CAPACITY;
            queueCount--;
        }
    }

    // -------------------------------------------------------------------

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

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
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

    // Input callbacks — only enqueue, never execute directly
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (isDead) return;
        EnqueueInput(0);
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (isDead) return;
        // Snapshot input value immediately — context is invalid after this frame
        EnqueueInput(1, ctx.ReadValue<Vector2>());
    }

    private void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (isDead) return;
        EnqueueInput(2);
    }

    // Execute methods — called by DrainInputQueue when the fireAt time arrives
    private void ExecuteJump()
    {
        if (isLaunched) return;
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

    private void ExecuteMove(Vector2 input)
    {
        if (isDead) return;
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

    private void ExecuteCrouch()
    {
        if (isDead) return;
        if (isLaunched) return;

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

    private void SetObstacleCollisionIgnored(bool ignore)
    {
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer == -1)
        {
            Debug.LogWarning("PlayerController: 'Obstacle' layer not found.");
            return;
        }

        Physics.IgnoreLayerCollision(gameObject.layer, obstacleLayer, ignore);
        obstacleCollisionIgnored = ignore;
    }

    public void ApplyHigherJump(float duration, float multiplier)
    {
        if (higherJumpCoroutine != null)
        {
            StopCoroutine(higherJumpCoroutine);
            remainingJumpTime += duration;
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
            remainingInvulnerabilityTime += duration;
        }
        else
        {
            remainingInvulnerabilityTime = duration;
        }

        invulnerabilityCoroutine = StartCoroutine(InvulnerabilitySequence(remainingInvulnerabilityTime));
    }

    public void ApplyLaunch(float duration = 5f, float upwardForce = 5f,
                            float speedMultiplier = 3f, float collisionGracePeriod = 1f)
    {
        if (launchCoroutine != null)
            StopCoroutine(launchCoroutine);

        launchCoroutine = StartCoroutine(LaunchSequence(duration, upwardForce, speedMultiplier, collisionGracePeriod));
    }

    private IEnumerator LaunchSequence(float duration, float upwardForce,
                                       float speedMultiplier, float collisionGracePeriod)
    {
        isLaunched = true;
        launchYReady = false;

        if (isCrouching)
            CancelCrouch();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        forwardSpeed = baseForwardSpeed * speedMultiplier;

        SetObstacleCollisionIgnored(true);
        Debug.Log("Launch STARTED");

        float liftTimeout = 5f;
        float liftElapsed = 0f;
        while (rb.position.y < launchTargetY - 0.05f && liftElapsed < liftTimeout)
        {
            liftElapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        launchLockedY = launchTargetY;
        launchYReady = true;

        if (collisionGracePeriod > 0f)
            yield return new WaitForSeconds(collisionGracePeriod);

        SetObstacleCollisionIgnored(false);

        float remaining = duration - collisionGracePeriod;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        isLaunched = false;
        launchYReady = false;
        launchLockedY = 0f;
        launchCoroutine = null;
        forwardSpeed = baseForwardSpeed;
        Debug.Log("Launch ENDED");
    }

    private IEnumerator HigherJumpSequence(float duration, float multiplier)
    {
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
        Debug.Log("Invulnerability ENDED");
    }

    private void Update()
    {
        // Drain the O2 input queue every frame
        if (!isDead)
            DrainInputQueue();

        // Death fall
        if (!isDead) return;
        if (deathFallTimer < deathPauseDuration)
        {
            deathFallTimer += Time.deltaTime;
            return;
        }
        rb.AddForce(new Vector3(0f, 0f, -deathFallForce), ForceMode.Acceleration);
    }

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
            if (!isLaunched)
                forwardSpeed = baseForwardSpeed;
            wasAirborne = false;
        }

        if (!wasAirborne && !isLaunched)
            forwardSpeed = baseForwardSpeed;

        float newX = Mathf.MoveTowards(rb.position.x, targetX, scaledLaneSlideSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(
            (newX - rb.position.x) / Time.fixedDeltaTime,
            isCrouching ? 0f : (isLaunched ? 0f : rb.linearVelocity.y),
            forwardSpeed
        );

        if (isLaunched)
        {
            float targetY = launchYReady
                ? launchLockedY
                : Mathf.MoveTowards(rb.position.y, launchTargetY, launchLiftSpeed * Time.fixedDeltaTime);
            rb.MovePosition(new Vector3(newX, targetY, rb.position.z + forwardSpeed * Time.fixedDeltaTime));
        }
        else if (isCrouching)
            rb.MovePosition(new Vector3(newX, originalY - crouchYOffset, rb.position.z + forwardSpeed * Time.fixedDeltaTime));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") && !isDead)
        {
            if (isInvulnerable)
            {
                GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
                foreach (GameObject obstacle in obstacles)
                    Destroy(obstacle);

                if (invulnerabilityCoroutine != null)
                    StopCoroutine(invulnerabilityCoroutine);

                isInvulnerable = false;
                invulnerabilityCoroutine = null;
                Debug.Log("Invulnerability ENDED - obstacle hit");
                return;
            }

            isDead = true;
            playerInput.Disable();
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        rb.linearVelocity = Vector3.zero;
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

    public void ForceKill()
    {
        if (isDead) return;
        isDead = true;
        playerInput.Disable();
        StartCoroutine(DeathSequence());
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}