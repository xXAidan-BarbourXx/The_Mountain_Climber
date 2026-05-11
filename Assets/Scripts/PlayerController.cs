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

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Death Settings")]
    [SerializeField] private float deathPauseDuration = 1f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchDuration = 1f;
    [SerializeField] private float crouchYOffset = 0.5f;

    private Rigidbody rb;
    private InputAction moveAction;
    private bool isGrounded;
    private int currentLane = 0;
    private float targetX = 0f;
    private bool isDead = false;
    private bool isCrouching = false;
    private float originalY;

    private CapsuleCollider capsuleCollider;
    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;
    private Vector3 originalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = new PlayerInputActions();

        originalScale = transform.localScale;

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
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
        rb.AddForce(new Vector3(0f, jumpSpeed * 0.4f, jumpSpeed), ForceMode.Impulse);
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
        if (isDead || isCrouching) return;
        StartCoroutine(CrouchSequence());
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

        transform.localScale = originalScale;

        rb.MovePosition(new Vector3(rb.position.x, originalY, rb.position.z));

        if (animator != null)
            animator.SetBool("Crouch", false);

        isCrouching = false;
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        float newX = Mathf.MoveTowards(rb.position.x, targetX, laneSlideSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(
            (newX - rb.position.x) / Time.fixedDeltaTime,
            isCrouching ? 0f : rb.linearVelocity.y,
            forwardSpeed
        );

        if (isCrouching)
            rb.MovePosition(new Vector3(rb.position.x, originalY - crouchYOffset, rb.position.z + forwardSpeed * Time.fixedDeltaTime));
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") && !isDead)
        {
            isDead = true;
            playerInput.Disable();
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Animator animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("Death");

        yield return new WaitForSeconds(deathPauseDuration);

        GameManager.Instance.TriggerGameOver();
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}