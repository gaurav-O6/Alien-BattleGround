
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed = 4f;
    public float chaseSpeed = 10f;
    public float patrolRadius = 10f;
    public float waitAtDestination = 2f;

    [Header("Detection")]
    public float detectionRadius = 15f;

    [Header("Combat")]
    public float combatStoppingDistance = 3f;
    public float facePlayerTurnSpeed = 720f;

    [Header("Combat Jump")]
    public float jumpMinDistance = 4f;
    public float jumpMaxDistance = 10f;
    public float jumpCooldown = 5f;
    public float jumpDuration = 0.8f;
    public float jumpHeight = 2.5f;
    public float landingSearchRadius = 3f;

    [Header("Jump Collision")]
    public LayerMask jumpCollisionMask = Physics.DefaultRaycastLayers;
    public float collisionSkin = 0.05f;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    private Animator animator;
    private CapsuleCollider capsule;

    private bool isChasing;
    private bool isJumping;
    private float waitTimer;
    private float nextJumpTime;

    private Vector3 jumpStart;
    private Vector3 jumpEnd;
    private float jumpElapsed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        animator = GetComponentInChildren<Animator>();
        capsule = GetComponent<CapsuleCollider>();
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHealth = playerObject.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogError(
                "EnemyAI: No GameObject tagged Player was found.");
            enabled = false;
            return;
        }

        if (agent == null)
        {
            Debug.LogError("EnemyAI: NavMeshAgent is missing.");
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogError("EnemyAI: No child Animator was found.");
            enabled = false;
            return;
        }

        if (capsule == null)
        {
            Debug.LogError(
                "EnemyAI: CapsuleCollider is missing from the Alien root.");
            enabled = false;
            return;
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = combatStoppingDistance;
        agent.autoBraking = true;
        agent.updatePosition = true;
        agent.updateRotation = true;

        nextJumpTime = Time.time + jumpCooldown;

        ChoosePatrolDestination();
    }

    private void Update()
    {
        if (player == null || agent == null)
            return;

        if ((enemyHealth != null && enemyHealth.Health <= 0) ||
            (playerHealth != null && playerHealth.IsPlayerDead()))
        {
            StopJumpIfNeeded();
            agent.isStopped = true;
            return;
        }

        if (isJumping)
        {
            UpdateJump();
            return;
        }

        if (!agent.isOnNavMesh)
            return;

        float distanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            isChasing = true;
            waitTimer = 0f;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = combatStoppingDistance;
            agent.isStopped = false;

            if (distanceToPlayer >= jumpMinDistance &&
                distanceToPlayer <= jumpMaxDistance &&
                Time.time >= nextJumpTime)
            {
                if (TryStartJump())
                    return;
            }

            if (distanceToPlayer > combatStoppingDistance)
            {
                agent.SetDestination(player.position);
            }
            else
            {
                agent.ResetPath();
                agent.isStopped = true;
                FacePlayer();
            }

            return;
        }

        if (isChasing)
        {
            isChasing = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = 2f;
            agent.isStopped = false;
            ChoosePatrolDestination();
        }

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitAtDestination)
            {
                ChoosePatrolDestination();
                waitTimer = 0f;
            }
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            facePlayerTurnSpeed * Time.deltaTime);
    }

    private bool TryStartJump()
    {
        Vector3 destination = player.position;
        destination.y = transform.position.y;

        if (!NavMesh.SamplePosition(
            destination,
            out NavMeshHit hit,
            landingSearchRadius,
            agent.areaMask))
        {
            return false;
        }

        Vector3 direction = hit.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return false;

        jumpStart = transform.position;
        jumpEnd = hit.position;
        jumpElapsed = 0f;

        transform.rotation =
            Quaternion.LookRotation(direction.normalized);

        agent.isStopped = true;
        agent.ResetPath();
        agent.updatePosition = false;
        agent.updateRotation = false;

        isJumping = true;
        nextJumpTime = Time.time + jumpCooldown;

        animator.ResetTrigger("Jump");
        animator.SetTrigger("Jump");

        return true;
    }

    private void UpdateJump()
    {
        jumpElapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(
            jumpElapsed / Mathf.Max(jumpDuration, 0.1f));

        Vector3 targetPosition = Vector3.Lerp(
            jumpStart, jumpEnd, progress);

        targetPosition.y +=
            4f * jumpHeight * progress * (1f - progress);

        Vector3 currentPosition = transform.position;
        Vector3 movement = targetPosition - currentPosition;
        float movementDistance = movement.magnitude;

        if (movementDistance > 0.001f &&
            IsJumpMovementBlocked(movement, movementDistance))
        {
            // Cancel this jump rather than pass through a solid obstacle.
            FinishJump();
            return;
        }

        transform.position = targetPosition;

        if (progress >= 1f)
            FinishJump();
    }

    private bool IsJumpMovementBlocked(
        Vector3 movement,
        float movementDistance)
    {
        Vector3 scale = transform.lossyScale;

        float radius = capsule.radius *
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));

        float height = Mathf.Max(
            capsule.height * Mathf.Abs(scale.y),
            radius * 2f);

        Vector3 center =
            transform.TransformPoint(capsule.center);

        float halfSegment = height * 0.5f - radius;

        Vector3 point1 = center + Vector3.up * halfSegment;
        Vector3 point2 = center - Vector3.up * halfSegment;

        // The alien's own collider is ignored by filtering its hierarchy.
        RaycastHit[] hits = Physics.CapsuleCastAll(
            point1,
            point2,
            Mathf.Max(radius - collisionSkin, 0.01f),
            movement.normalized,
            movementDistance + collisionSkin,
            jumpCollisionMask,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            if (hit.collider == capsule ||
                hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                continue;
            }

            // Ignore other colliders belonging to the player hierarchy
            // so the jump doesn't stop just because it reaches the player.
            if (player != null &&
                (hitTransform == player ||
                 hitTransform.IsChildOf(player)))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void FinishJump()
    {
        if (agent == null)
            return;

        // Return to a valid NavMesh position near the current location.
        Vector3 recoveryPosition = transform.position;

        if (NavMesh.SamplePosition(
            recoveryPosition,
            out NavMeshHit hit,
            landingSearchRadius,
            agent.areaMask))
        {
            transform.position = hit.position;

            if (agent.isOnNavMesh)
                agent.Warp(hit.position);
            else
                agent.Warp(hit.position);
        }
        else
        {
            Debug.LogWarning(
                "EnemyAI: Could not find a valid NavMesh position " +
                "after the jump.");
        }

        agent.updatePosition = true;
        agent.updateRotation = true;
        isJumping = false;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;

            if (player != null)
                agent.SetDestination(player.position);
        }
    }

    private void StopJumpIfNeeded()
    {
        if (isJumping)
            FinishJump();
    }

    private void ChoosePatrolDestination()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPoint = transform.position + new Vector3(
                Random.Range(-patrolRadius, patrolRadius),
                0f,
                Random.Range(-patrolRadius, patrolRadius));

            if (NavMesh.SamplePosition(
                randomPoint,
                out NavMeshHit hit,
                patrolRadius,
                agent.areaMask))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        Debug.LogWarning(
            "EnemyAI: Could not find a patrol destination.");
    }
}
