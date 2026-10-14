using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour

{
    private NavMeshAgent agent;
    public float patrolRadius = 10f;
    public float DetectionRadius = 15f;
    private Transform player;
    private bool isChasing;
    public float WaitAtDestination = 2f;
    private float waitTimer;



    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 3f;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        ChoosePatrolDestination();
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= DetectionRadius)
        {
            isChasing = true;
            waitTimer = 0f;
            agent.SetDestination(player.position);
            Vector3 DirectionToPlayer = (player.position - transform.position);
            DirectionToPlayer.y = 0f;

            if (DirectionToPlayer != Vector3.zero)
            {
                Quaternion TargetRotation = Quaternion.LookRotation(DirectionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation, 5f * Time.deltaTime);
            }
            return;
        }
        if (isChasing)
        {
            isChasing = false;
            ChoosePatrolDestination();
        }
     
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= WaitAtDestination)
            {
                ChoosePatrolDestination();
                waitTimer = 0f;
            }
        }
    }
    private bool ChoosePatrolDestination()
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = new Vector3( Random.Range(-55f, 55f), 0f, Random.Range(-55f, 55f));

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return true;
            }
        }
        return false;
    }
}
