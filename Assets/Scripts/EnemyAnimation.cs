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

    void Start()
    {
       
    }
    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

        animator.SetFloat("Horizontal", localVelocity.x);
        animator.SetFloat("Vertical", localVelocity.z);
    }
}
