
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Animator animator;
    float VerticalVelocity = 0f;
    bool IsRunning = false;

    public PlayerHealth playerHealth;

    void Start()
    {
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsPlayerDead())
        {
            return;
        }

        float MovementSpeed = 5f;
        IsRunning = Input.GetKey(KeyCode.LeftShift);

        if (IsRunning)
        {
            MovementSpeed = 15f;
        }

        float HorizontalInput = Input.GetAxis("Horizontal");
        float VerticalInput = Input.GetAxis("Vertical");

        CharacterController controller = GetComponent<CharacterController>();

        if (controller.isGrounded && VerticalVelocity < 0)
        {
            VerticalVelocity = -2f;
        }
        else
        {
            VerticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            VerticalVelocity = 4f;
            animator.SetTrigger("Jump");
        }

        Vector3 Movement =
            transform.right * HorizontalInput * MovementSpeed
            + VerticalVelocity * Vector3.up
            + transform.forward * VerticalInput * MovementSpeed;

        float MovementAmount = Mathf.Clamp01(
            new Vector2(HorizontalInput, VerticalInput).magnitude
        );

        float AnimationSpeed = IsRunning
            ? MovementAmount
            : MovementAmount * 0.5f;

        animator.SetFloat("Speed", AnimationSpeed);
        animator.SetFloat("Horizontal", HorizontalInput);
        animator.SetFloat("Vertical", VerticalInput);

        controller.Move(Movement * Time.deltaTime);
        animator.SetBool("Grounded", controller.isGrounded);
    }
}
