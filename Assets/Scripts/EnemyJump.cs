using UnityEngine;
using UnityEngine.AI;

public class EnemyJump : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpHeight = 1.5f;
    public float jumpCooldown = 3f;
    public float forwardJumpSpeed = 4f;

    [Header("Barrier Detection")]
    public float obstacleCheckDistance = 1.2f;
    public float obstacleCheckHeight = 0.4f;
    public float maxBarrierHeight = 1.2f;
    public LayerMask obstacleLayers = ~0;

    [Header("Landing")]
    public float landingSearchDistance = 2f;
    public float groundCheckDistance = 0.25f;
    public float maxJumpDuration = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private CapsuleCollider capsuleCollider;

    private bool isJumping;
    private float verticalVelocity;
    private float nextJumpTime;
    private float jumpStartTime;

    private Vector3 horizontalJumpDirection;
    private Vector3 lastNavMeshPosition;

    private bool originalUpdatePosition;
    private bool originalUpdateRotation;
    private bool originalStopped;

    private readonly RaycastHit[] rayHits = new RaycastHit[16];

    private int jumpTriggerHash;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        jumpTriggerHash = Animator.StringToHash("Jump");
    }

    public bool IsJumping()
    {
        return isJumping;
    }

    public bool CanJump()
    {
        return !isJumping &&
               agent != null &&
               agent.enabled &&
               agent.isOnNavMesh &&
               Time.time >= nextJumpTime;
    }

    public bool IsLowBarrierAhead()
    {
        if (!CanJump())
            return false;

        Vector3 origin =
            transform.position + Vector3.up * obstacleCheckHeight;

        if (!Physics.Raycast(
            origin,
            transform.forward,
            out RaycastHit hit,
            obstacleCheckDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (hit.transform.root == transform.root)
            return false;

        Vector3 aboveBarrier =
            hit.point + Vector3.up * maxBarrierHeight;

        if (Physics.Raycast(
            aboveBarrier,
            transform.forward,
            obstacleCheckDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3 landingProbe =
            transform.position +
            transform.forward *
            (obstacleCheckDistance + landingSearchDistance);

        if (!NavMesh.SamplePosition(
            landingProbe,
            out NavMeshHit landingHit,
            landingSearchDistance,
            agent.areaMask))
        {
            return false;
        }

        return Vector3.Dot(
            landingHit.position - hit.point,
            transform.forward) > agent.radius;
    }

    // Detects when a nearby player is on lower NavMesh ground.
    public bool ShouldJumpDownTowards(
        Transform target,
        float maxPlayerDistance,
        float minHeightDifference)
    {
        if (!CanJump() || target == null)
            return false;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude > maxPlayerDistance)
            return false;

        if (toTarget.sqrMagnitude < 0.01f)
            return false;

        if (!NavMesh.SamplePosition(
            target.position,
            out NavMeshHit targetNavHit,
            2f,
            agent.areaMask))
        {
            return false;
        }

        // Only jump down when the player's walkable ground is
        // sufficiently lower than the alien's current NavMesh position.
        if (agent.nextPosition.y - targetNavHit.position.y
            < minHeightDifference)
        {
            return false;
        }

        Vector3 direction = toTarget.normalized;

        // Check that there is lower NavMesh ground ahead,
        // toward the player, before committing to the jump.
        float probeDistance = Mathf.Min(
            3f,
            toTarget.magnitude * 0.7f);

        Vector3 landingProbe =
            transform.position + direction * probeDistance;

        if (!NavMesh.SamplePosition(
            landingProbe,
            out NavMeshHit landingHit,
            landingSearchDistance,
            agent.areaMask))
        {
            return false;
        }

        return agent.nextPosition.y - landingHit.position.y
               >= minHeightDifference;
    }

    public void StartJump(Vector3 direction)
    {
        if (!CanJump())
            return;

        horizontalJumpDirection = direction;
        horizontalJumpDirection.y = 0f;

        if (horizontalJumpDirection.sqrMagnitude < 0.001f)
            return;

        horizontalJumpDirection.Normalize();

        lastNavMeshPosition = agent.nextPosition;

        originalUpdatePosition = agent.updatePosition;
        originalUpdateRotation = agent.updateRotation;
        originalStopped = agent.isStopped;

        isJumping = true;
        jumpStartTime = Time.time;
        nextJumpTime = Time.time + jumpCooldown;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.updatePosition = false;
        agent.updateRotation = false;

        verticalVelocity = Mathf.Sqrt(
            2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);

        if (animator != null)
            animator.SetTrigger(jumpTriggerHash);
    }

    void Update()
    {
        if (!isJumping)
            return;

        if (Time.time - jumpStartTime >= maxJumpDuration)
        {
            RecoverFromJump();
            return;
        }

        float deltaTime = Time.deltaTime;

        verticalVelocity += Physics.gravity.y * deltaTime;

        Vector3 movement =
            horizontalJumpDirection * forwardJumpSpeed +
            Vector3.up * verticalVelocity;

        transform.position += movement * deltaTime;

        if (verticalVelocity <= 0f)
            TryLand();
    }

    private void TryLand()
    {
        float halfHeight = capsuleCollider != null
            ? capsuleCollider.height * 0.5f
            : 1f;

        Vector3 origin =
            transform.position + Vector3.up * 0.1f;

        float rayDistance = halfHeight + groundCheckDistance;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            rayHits,
            rayDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = rayHits[i];

            if (hit.transform.root == transform.root)
                continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > 50f)
                continue;

            if (!NavMesh.SamplePosition(
                hit.point,
                out NavMeshHit navHit,
                landingSearchDistance,
                agent.areaMask))
            {
                continue;
            }

            if (Mathf.Abs(navHit.position.y - hit.point.y) > 0.5f)
                continue;

            FinishJump(navHit.position);
            return;
        }
    }

    private void FinishJump(Vector3 landingPosition)
    {
        if (agent == null || !agent.enabled ||
            !agent.Warp(landingPosition))
        {
            RecoverFromJump();
            return;
        }

        transform.position = landingPosition;

        RestoreAgent();

        verticalVelocity = 0f;
        isJumping = false;
    }

    private void RecoverFromJump()
    {
        if (agent != null && agent.enabled)
        {
            if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit safePosition,
                landingSearchDistance,
                agent.areaMask))
            {
                agent.Warp(safePosition.position);
                transform.position = safePosition.position;
            }
            else if (NavMesh.SamplePosition(
                lastNavMeshPosition,
                out safePosition,
                landingSearchDistance,
                agent.areaMask))
            {
                agent.Warp(safePosition.position);
                transform.position = safePosition.position;
            }

            RestoreAgent();
        }

        verticalVelocity = 0f;
        isJumping = false;
    }

    private void RestoreAgent()
    {
        agent.updatePosition = originalUpdatePosition;
        agent.updateRotation = originalUpdateRotation;
        agent.isStopped = originalStopped;
    }
}