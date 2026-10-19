
using UnityEngine;
using UnityEngine.AI;

public class AlienAnimationController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }


    private void Update()
    {
        if (agent == null || animator == null)
            return;

        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

        float horizontal = Mathf.Clamp(localVelocity.x / 5f, -1f, 1f);
        float vertical = Mathf.Clamp(localVelocity.z / 5f, -1f, 1f);

        animator.SetFloat("Horizontal", horizontal);
        animator.SetFloat("Vertical", vertical);
        animator.SetFloat("Speed", agent.velocity.magnitude > 2.5f ? 1f : 0f);
    }

}
