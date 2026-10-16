
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Animator animator;

    private float VerticalVelocity = 0f;
    private bool IsRunning = false;

    public PlayerHealth playerHealth;

    void Update()
    {
        CharacterController controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            return;
        }

        bool IsDead = playerHealth != null && playerHealth.IsPlayerDead();

        float MovementSpeed = 5f;
        IsRunning = Input.GetKey(KeyCode.LeftShift);

        if (IsRunning)
        {
            MovementSpeed = 15f;
        }

        float HorizontalInput = Input.GetAxis("Horizontal");
        float VerticalInput = Input.GetAxis("Vertical");

        if (IsDead)
        {
            HorizontalInput = 0f;
            VerticalInput = 0f;
        }

        if (controller.isGrounded && VerticalVelocity < 0f)
        {
            VerticalVelocity = -2f;
        }
        else
        {
            VerticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        if (!IsDead && Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            VerticalVelocity = 4f;

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
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

        if (IsDead)
        {
            AnimationSpeed = 0f;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", AnimationSpeed);
            animator.SetFloat("Horizontal", HorizontalInput);
            animator.SetFloat("Vertical", VerticalInput);
            animator.SetBool("Grounded", controller.isGrounded);
        }

        controller.Move(Movement * Time.deltaTime);
    }
}
