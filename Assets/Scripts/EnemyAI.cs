using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyJump enemyJump;

    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;
    public float patrolRadius = 10f;
    public float DetectionRadius = 15f;

    [Header("Jump Down")]
    public float jumpDownPlayerDistance = 6f;
    public float minimumJumpDownHeight = 1.2f;

    private Transform player;
    private PlayerHealth playerHealth;
    private bool isChasing;

    public float WaitAtDestination = 2f;
    private float waitTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyJump = GetComponent<EnemyJump>();

        agent.stoppingDistance = 2f;
        agent.autoBraking = true;
        agent.speed = patrolSpeed;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHealth =
                playerObject.GetComponent<PlayerHealth>();
        }

        ChoosePatrolDestination();
    }

    void Update()
    {
        if (player == null)
            return;

        // Let an active jump finish without NavMesh commands
        // fighting the custom jump movement.
        if (enemyJump != null && enemyJump.IsJumping())
            return;

        if (playerHealth != null && playerHealth.IsPlayerDead())
        {
            isChasing = false;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            return;
        }

        float distanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= DetectionRadius)
        {
            isChasing = true;
            agent.speed = chaseSpeed;
            waitTimer = 0f;

            Vector3 directionToPlayer =
                player.position - transform.position;

            directionToPlayer.y = 0f;

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(directionToPlayer);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    5f * Time.deltaTime);
            }

            if (distanceToPlayer > agent.stoppingDistance)
            {
                // Check for a jump down before following the
                // normal NavMesh route toward the player.
                if (enemyJump != null &&
                    enemyJump.ShouldJumpDownTowards(
                        player,
                        jumpDownPlayerDistance,
                        minimumJumpDownHeight))
                {
                    enemyJump.StartJump(directionToPlayer);
                    return;
                }

                agent.SetDestination(player.position);
            }
            else
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            return;
        }

        if (isChasing)
        {
            isChasing = false;
            agent.speed = patrolSpeed;
            ChoosePatrolDestination();
        }

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
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
            Vector3 randomPoint = new Vector3(
                Random.Range(-55f, 55f),
                0f,
                Random.Range(-55f, 55f));

            if (NavMesh.SamplePosition(
                randomPoint,
                out NavMeshHit hit,
                5f,
                NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return true;
            }
        }

        return false;
    }
}