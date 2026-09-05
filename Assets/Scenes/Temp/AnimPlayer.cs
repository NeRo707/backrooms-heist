using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AnimPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -19.62f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 playerVelocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Check ground state directly from CharacterController
        isGrounded = controller.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        // Reset downward force when touching ground to prevent accumulation
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // 2. Horizontal Movement
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            // Rotation
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Horizontal displacement
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);
        }

        // 3. Jump Impulse (v = sqrt(h * -2 * g))
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Apply Gravity & Vertical Movement
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // 5. Update Animation Parameter
        animator.SetFloat("xInput", moveDirection.magnitude);
    }
}