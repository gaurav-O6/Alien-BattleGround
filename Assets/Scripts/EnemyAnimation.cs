
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimation : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (agent == null || animator == null)
        {
            return;
        }

        // Use the alien's actual movement speed.
        float movementSpeed = agent.velocity.magnitude;
        animator.SetFloat("Speed", movementSpeed);

        // Convert movement into the alien's local directions.
        Vector3 localVelocity =
            transform.InverseTransformDirection(agent.velocity);

        animator.SetFloat("Horizontal", localVelocity.x);
        animator.SetFloat("Vertical", localVelocity.z);
    }
}
