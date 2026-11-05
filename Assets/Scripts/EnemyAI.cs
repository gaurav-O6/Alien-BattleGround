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

    [Header("Gunshot Awareness")]

    public float hearingRadius = 60f;

    public float investigationDuration = 5f;

    [Header("Combat")]

    public float combatStoppingDistance = 3f;

    [Tooltip("Minimum clear distance between the Player and enemy capsule surfaces.")]

    public float desiredSurfaceGap = 1f;

    public float facePlayerTurnSpeed = 720f;

    [Header("Player Push Reaction")]

    [Tooltip("Extra distance beyond the safety circle at which a Player push can be detected.")]

    public float pushDetectionExtraDistance = 0.5f;

    [Tooltip("Enemy movement speed while retreating from a Player push.")]

    public float pushSpeed = 3.5f;

    [Tooltip("How far the enemy tries to move away from the Player while being pushed.")]

    public float pushRetreatDistance = 2f;

    [Tooltip("How long the enemy continues retreating after the Player stops pushing.")]

    public float pushReleaseDelay = 0.25f;

    [Tooltip("Minimum Player horizontal movement speed required to register a push.")]

    public float pushVelocityThreshold = 0.2f;

    [Tooltip("Animator damping time for locomotion parameters.")]

    public float animatorDampTime = 0.1f;

    [Header("Combat Jump")]

    public float jumpMinDistance = 4f;

    public float jumpMaxDistance = 10f;

    public float jumpCooldown = 5f;

    public float jumpDuration = 0.8f;

    public float jumpHeight = 2.5f;

    [Tooltip("NavMesh sample radius for finding a landing point near the target platform.")]

    public float landingSearchRadius = 5f;

    public float elevatedJumpThreshold = 1f;

    [Header("Jump Collision")]

    public LayerMask jumpCollisionMask = Physics.DefaultRaycastLayers;

    public float collisionSkin = 0.05f;

    [Header("Vision")]

    public LayerMask visionMask;

    public float visionHeight = 1.2f;

    [Header("Stuck Recovery")]

    public float stuckTime = 2f;

    public float minimumMovementToResetStuck = 0.15f;

    public float recoveryCooldown = 2f;

    [Header("Safety Circle")]

    [Tooltip("Extra horizontal buffer used to keep enemy centers outside the Player capsule.")]


    public float safetyBuffer = 0.1f;

    [Header("Camera Corridor Protection")]
    [Tooltip("Half-width of the protected space between the camera and Player.")]
    public float cameraCorridorHalfWidth = 0.8f;

    [Tooltip("Maximum distance from the Player to protect toward the camera.")]
    public float cameraCorridorDepth = 5.5f;

    private Transform mainCameraTransform;
    private NavMeshAgent agent;

    private Transform player;

    private PlayerHealth playerHealth;

    private CharacterController playerCharacterController;

    private EnemyHealth enemyHealth;

    private Animator animator;

    private CapsuleCollider capsule;

    private bool isChasing;

    private bool isJumping;

    private bool isInvestigating;

    private bool isBeingPushed;

    private float waitTimer;

    private float nextJumpTime;

    private float investigationTimer;

    private float jumpElapsed;

    private float stuckTimer;

    private float nextRecoveryTime;

    private float pushReleaseTimer;

    private Vector3 lastStuckCheckPosition;

    private Vector3 lastPlayerPosition;
    private Vector3 lastAnimatorPosition;

    private Vector3 investigationPosition;

    private Vector3 jumpStart;

    private Vector3 jumpEnd;

    private Vector3 playerHorizontalVelocity;

    private Vector3 lastPushDestination;

    private void OnEnable()

    {

        GunshotAlert.OnGunshot += HearGunshot;

    }

    private void OnDisable()

    {

        GunshotAlert.OnGunshot -= HearGunshot;

    }

    private void Awake()

    {

        agent = GetComponent<NavMeshAgent>();

        enemyHealth = GetComponent<EnemyHealth>();

        animator = GetComponentInChildren<Animator>();

        capsule = GetComponent<CapsuleCollider>();

    }

    private void Start()

    {

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            mainCameraTransform = mainCamera.transform;
        }
        else
        {
            Debug.LogWarning("EnemyAI: No MainCamera found.");
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)

        {

            Debug.LogError("EnemyAI: No GameObject tagged Player was found.");

            enabled = false;

            return;

        }

        player = playerObject.transform;

        playerHealth = playerObject.GetComponent<PlayerHealth>();

        playerCharacterController = playerObject.GetComponent<CharacterController>();

        if (agent == null || animator == null || capsule == null)

        {

            Debug.LogError("EnemyAI: Missing NavMeshAgent, Animator, or CapsuleCollider.", this);

            enabled = false;

            return;

        }

        // Disable only enemy-vs-Player body contact. Enemy/world collisions remain enabled.

        if (playerCharacterController != null)

        {

            Physics.IgnoreCollision(capsule, playerCharacterController, false);

        }

        else

        {

            Debug.LogWarning(

                "EnemyAI: Player has no CharacterController; cannot ignore body collision.",

                this);

        }

        agent.speed = patrolSpeed;

        agent.stoppingDistance = 0.2f;

        agent.autoBraking = true;

        agent.updatePosition = true;

        agent.updateRotation = true;

        nextJumpTime = Time.time + jumpCooldown;

        nextRecoveryTime = Time.time + recoveryCooldown;

        lastStuckCheckPosition = transform.position;

        lastPlayerPosition = player.position;

        ChoosePatrolDestination();

    }

    private void Update()

    {

        if (player == null || agent == null || !agent.isOnNavMesh)

            return;

        UpdatePlayerMovementSample();

        if ((enemyHealth != null && enemyHealth.Health <= 0) ||

            (playerHealth != null && playerHealth.IsPlayerDead()))

        {

            StopJumpIfNeeded();

            agent.isStopped = true;

            UpdateLocomotionAnimator();

            return;

        }

        if (isJumping)

        {

            UpdateJump();

            EnforceSafetyCircle();

            UpdateLocomotionAnimator();

            return;

        }

        // Detect the Player actively moving into the enemy.

        if (DetectPlayerPush())

        {

            isBeingPushed = true;

            pushReleaseTimer = pushReleaseDelay;

        }

        else if (isBeingPushed)

        {

            pushReleaseTimer -= Time.deltaTime;

            if (pushReleaseTimer <= 0f)

                isBeingPushed = false;

        }

        if (isBeingPushed)

        {

            HandlePlayerPush();

            EnforceSafetyCircle();

            UpdateLocomotionAnimator();

            return;

        }

        // Push reaction has ended. Restore the correct movement speed.

        if (isChasing)

            agent.speed = chaseSpeed;

        else

            agent.speed = patrolSpeed;

        MonitorStuck();

        if (isJumping)

        {

            UpdateJump();

            EnforceSafetyCircle();

            UpdateLocomotionAnimator();

            return;

        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        float horizontalDistanceToPlayer =

            GetHorizontalDistance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)

        {

            isInvestigating = false;

            HandlePlayerDetection(distanceToPlayer, horizontalDistanceToPlayer);

            EnforceSafetyCircle();

            UpdateLocomotionAnimator();

            return;

        }

        if (isInvestigating)

        {

            HandleInvestigation();

            EnforceSafetyCircle();

            UpdateLocomotionAnimator();

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

        HandlePatrol();

        EnforceCameraCorridor();

        UpdateLocomotionAnimator();

    }

    private void UpdatePlayerMovementSample()

    {

        if (player == null)

            return;

        Vector3 velocity;

        if (playerCharacterController != null)

        {

            velocity = playerCharacterController.velocity;

        }

        else

        {

            velocity = (player.position - lastPlayerPosition) /

                       Mathf.Max(Time.deltaTime, 0.0001f);

        }

        velocity.y = 0f;

        playerHorizontalVelocity = velocity;

        lastPlayerPosition = player.position;

    }

    private bool DetectPlayerPush()

    {

        if (player == null)

            return false;

        float distance = GetHorizontalDistance(transform.position, player.position);

        float detectionDistance = GetSafeCenterDistance() + pushDetectionExtraDistance;

        if (distance > detectionDistance)

            return false;

        if (playerHorizontalVelocity.magnitude < pushVelocityThreshold)

            return false;

        Vector3 directionFromPlayerToEnemy = transform.position - player.position;

        directionFromPlayerToEnemy.y = 0f;

        if (directionFromPlayerToEnemy.sqrMagnitude < 0.001f)

            return false;

        directionFromPlayerToEnemy.Normalize();

        // Positive dot product means the Player is moving toward the enemy.

        float movingIntoEnemy = Vector3.Dot(

            playerHorizontalVelocity.normalized,

            directionFromPlayerToEnemy);

        return movingIntoEnemy > 0.2f;

    }

    private void HandlePlayerPush()

    {

        if (player == null || !agent.isOnNavMesh)

            return;

        agent.isStopped = false;

        agent.speed = pushSpeed;

        agent.stoppingDistance = 0.1f;

        Vector3 awayFromPlayer = transform.position - player.position;

        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)

        {

            awayFromPlayer = -transform.forward;

            awayFromPlayer.y = 0f;

        }

        awayFromPlayer.Normalize();

        // Keep the enemy outside the safety circle while moving away from the Player.

        Vector3 desiredDestination =

            player.position + awayFromPlayer *

            (GetSafeCenterDistance() + Mathf.Max(0.1f, pushRetreatDistance));

        desiredDestination.y = transform.position.y;

        // Avoid recalculating the NavMesh destination unnecessarily every frame.

        if (Vector3.Distance(lastPushDestination, desiredDestination) > 0.35f ||

            !agent.hasPath)

        {

            if (TrySetReachableDestination(desiredDestination, 3f))

                lastPushDestination = desiredDestination;

        }

        stuckTimer = 0f;

    }

    private void UpdateLocomotionAnimator()
    {
        if (animator == null)
            return;

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

        // Measure actual transform movement rather than relying on agent.velocity.
        // This also works while the enemy is moved manually during its jump.
        Vector3 worldVelocity =
            (transform.position - lastAnimatorPosition) / deltaTime;

        lastAnimatorPosition = transform.position;

        worldVelocity.y = 0f;

        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        float referenceSpeed = Mathf.Max(
            agent != null ? agent.speed : patrolSpeed,
            0.01f);

        float horizontal = Mathf.Clamp(localVelocity.x / referenceSpeed, -1f, 1f);
        float vertical = Mathf.Clamp(localVelocity.z / referenceSpeed, -1f, 1f);
        float normalizedSpeed = Mathf.Clamp01(worldVelocity.magnitude / referenceSpeed);

        animator.SetFloat("Horizontal", horizontal, animatorDampTime, deltaTime);
        animator.SetFloat("Vertical", vertical, animatorDampTime, deltaTime);
        animator.SetFloat("Speed", normalizedSpeed, animatorDampTime, deltaTime);
    }

    private void HandlePlayerDetection(

        float distanceToPlayer,

        float horizontalDistanceToPlayer)

    {

        isChasing = true;

        waitTimer = 0f;

        agent.speed = chaseSpeed;

        agent.isStopped = false;

        float verticalDifference = player.position.y - transform.position.y;

        bool playerAtDifferentHeight =

            Mathf.Abs(verticalDifference) >= elevatedJumpThreshold;

        bool withinJumpRange =

            horizontalDistanceToPlayer > GetSafeCenterDistance() &&

            horizontalDistanceToPlayer <= jumpMaxDistance;

        // Try a platform jump before ordinary ground navigation when the Player is elevated.

        if (playerAtDifferentHeight &&

            withinJumpRange &&

            Time.time >= nextJumpTime &&

            TryStartJumpToPlayer())

        {

            return;

        }

        float safeCenterDistance = GetSafeCenterDistance();

        bool canSeePlayer = CanSeePlayer();

        if (horizontalDistanceToPlayer > safeCenterDistance + 0.15f || !canSeePlayer)

        {

            Vector3 target = GetPlayerApproachTarget();

            if (!TrySetReachableDestination(target, 3f))

            {

                if (!TryFindApproachPosition(target))

                {

                    if (agent.hasPath &&

                        agent.pathStatus != NavMeshPathStatus.PathComplete)

                    {

                        agent.ResetPath();

                    }

                    FacePlayer();

                }

            }

        }

        else

        {

            agent.ResetPath();

            agent.isStopped = true;

            FacePlayer();

        }

    }

    private float GetSafeCenterDistance()

    {

        float enemyScaleXZ = Mathf.Max(

            Mathf.Abs(transform.lossyScale.x),

            Mathf.Abs(transform.lossyScale.z));

        float enemyCapsuleRadius =

            capsule != null ? capsule.radius * enemyScaleXZ : 0.5f;

        float enemyRadius =

            agent != null ? Mathf.Max(agent.radius, enemyCapsuleRadius) : enemyCapsuleRadius;

        float playerRadius = 0.5f;

        if (playerCharacterController != null)

        {

            Vector3 playerScale = player.lossyScale;

            playerRadius = playerCharacterController.radius *

                           Mathf.Max(

                               Mathf.Abs(playerScale.x),

                               Mathf.Abs(playerScale.z));

        }

        return enemyRadius +

               playerRadius +

               Mathf.Max(0f, desiredSurfaceGap) +

               Mathf.Max(0f, safetyBuffer);

    }


    private bool IsOnCameraSideOfPlayer(Vector3 position)
    {
        if (player == null || mainCameraTransform == null)
            return false;

        Vector3 cameraDirection = mainCameraTransform.position - player.position;
        cameraDirection.y = 0f;

        if (cameraDirection.sqrMagnitude < 0.001f)
            return false;

        cameraDirection.Normalize();

        Vector3 offset = position - player.position;
        offset.y = 0f;

        return Vector3.Dot(offset, cameraDirection) > 0f;
    }

    private Vector3 GetCameraSafeSideDirection()
    {
        if (player == null || mainCameraTransform == null)
            return transform.right;

        Vector3 cameraDirection = mainCameraTransform.position - player.position;
        cameraDirection.y = 0f;

        if (cameraDirection.sqrMagnitude < 0.001f)
            return transform.right;

        cameraDirection.Normalize();

        Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraDirection).normalized;

        Vector3 enemyOffset = transform.position - player.position;
        enemyOffset.y = 0f;

        float side = Vector3.Dot(enemyOffset, cameraRight);

        // Pick a consistent side if the enemy is almost exactly behind the Player.
        if (Mathf.Abs(side) < 0.1f)
            side = transform.GetInstanceID() % 2 == 0 ? 1f : -1f;

        return cameraRight * Mathf.Sign(side);
    }

    private bool IsInsideCameraCorridor(Vector3 position)
    {
        if (player == null || mainCameraTransform == null)
            return false;

        Vector3 cameraDirection = mainCameraTransform.position - player.position;
        cameraDirection.y = 0f;

        float cameraDistance = cameraDirection.magnitude;

        if (cameraDistance < 0.01f)
            return false;

        cameraDirection.Normalize();

        Vector3 offset = position - player.position;
        offset.y = 0f;

        float depth = Vector3.Dot(offset, cameraDirection);

        if (depth <= 0f || depth > Mathf.Min(cameraCorridorDepth, cameraDistance))
            return false;

        Vector3 lateralOffset = offset - cameraDirection * depth;

        float enemyRadius = agent != null ? agent.radius : 0.5f;

        return lateralOffset.magnitude <
               cameraCorridorHalfWidth + enemyRadius;
    }

    private bool EnforceCameraCorridor()
    {
        if (player == null ||
            mainCameraTransform == null ||
            agent == null ||
            !agent.isOnNavMesh ||
            isJumping)
        {
            return false;
        }

        if (!IsInsideCameraCorridor(transform.position))
            return false;

        Vector3 cameraDirection =
            mainCameraTransform.position - player.position;

        cameraDirection.y = 0f;

        if (cameraDirection.sqrMagnitude < 0.001f)
            return false;

        cameraDirection.Normalize();

        Vector3 cameraRight =
            Vector3.Cross(Vector3.up, cameraDirection).normalized;

        Vector3 offset = transform.position - player.position;
        offset.y = 0f;

        float depth = Vector3.Dot(offset, cameraDirection);

        Vector3 lateralOffset = offset - cameraDirection * depth;

        float side = Vector3.Dot(lateralOffset, cameraRight);

        // If the enemy is directly in the middle, choose a consistent side.
        if (Mathf.Abs(side) < 0.05f)
            side = transform.GetInstanceID() % 2 == 0 ? 1f : -1f;

        Vector3 escapeDirection = cameraRight * Mathf.Sign(side);

        float enemyRadius = agent.radius;

        float safeHalfWidth =
            cameraCorridorHalfWidth + enemyRadius + 0.25f;

        float currentLateralDistance = lateralOffset.magnitude;

        float distanceToMove =
            safeHalfWidth - currentLateralDistance;

        if (distanceToMove <= 0f)
            return false;

        // Override the current path while the enemy escapes sideways.
        agent.ResetPath();
        agent.isStopped = false;

        float moveDistance = Mathf.Min(
            distanceToMove,
            Mathf.Max(agent.speed, 1f) * Time.deltaTime);

        agent.Move(escapeDirection * moveDistance);

        return true;
    }


    private Vector3 GetPlayerApproachTarget()
    {
        // Enemies on the camera side approach from the Player's side instead.
        if (IsOnCameraSideOfPlayer(transform.position))
        {
            Vector3 sideDirection = GetCameraSafeSideDirection();

            return player.position +
                   sideDirection * (GetSafeCenterDistance() + 0.25f);
        }

        Vector3 away = transform.position - player.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
        {
            away = -transform.forward;
            away.y = 0f;
        }

        away.Normalize();

        return player.position + away * GetSafeCenterDistance();
    }

    private bool TryStartJumpToPlayer()

    {

        if (player == null || !agent.isOnNavMesh)

            return false;

        Vector3 directionFromTargetToEnemy = transform.position - player.position;

        directionFromTargetToEnemy.y = 0f;

        if (directionFromTargetToEnemy.sqrMagnitude < 0.001f)

            directionFromTargetToEnemy = -transform.forward;

        directionFromTargetToEnemy.Normalize();

        // If the enemy is on the camera side, use a side landing direction.
        if (IsOnCameraSideOfPlayer(transform.position))
        {
            directionFromTargetToEnemy = GetCameraSafeSideDirection();
        }

        float ringDistance = GetSafeCenterDistance() + 0.15f;

        Vector3 desiredLanding =

            player.position + directionFromTargetToEnemy * ringDistance;

        return TryStartJumpToPosition(desiredLanding, player.position, true);

    }

    private bool TryStartJumpToInvestigation()

    {

        if (!agent.isOnNavMesh)

            return false;

        Vector3 horizontal = investigationPosition - transform.position;

        horizontal.y = 0f;

        float horizontalDistance = horizontal.magnitude;

        float verticalDifference = investigationPosition.y - transform.position.y;

        if (Mathf.Abs(verticalDifference) < elevatedJumpThreshold ||

            horizontalDistance <= GetSafeCenterDistance() ||

            horizontalDistance > jumpMaxDistance ||

            Time.time < nextJumpTime)

        {

            return false;

        }

        Vector3 target = investigationPosition;

        bool protectPlayer =

            player != null &&

            GetHorizontalDistance(player.position, investigationPosition) <

            GetSafeCenterDistance() * 1.5f;

        if (protectPlayer)

        {

            Vector3 away = transform.position - player.position;

            away.y = 0f;

            if (away.sqrMagnitude < 0.001f)

                away = -transform.forward;

            away.Normalize();

            target = player.position + away * (GetSafeCenterDistance() + 0.15f);

        }

        return TryStartJumpToPosition(target, investigationPosition, protectPlayer);

    }

    // Finds a NavMesh landing on the target's elevation, outside the Player's safety circle.

    private bool TryStartJumpToPosition(

        Vector3 preferredLanding,

        Vector3 targetPosition,

        bool protectPlayer)

    {

        if (player == null || !agent.isOnNavMesh)

            return false;

        Vector3 baseDirection = preferredLanding - targetPosition;

        baseDirection.y = 0f;

        if (baseDirection.sqrMagnitude < 0.001f)

        {

            baseDirection = transform.position - targetPosition;

            baseDirection.y = 0f;

        }

        if (baseDirection.sqrMagnitude < 0.001f)

            baseDirection = -transform.forward;

        baseDirection.Normalize();

        float ring = protectPlayer ? GetSafeCenterDistance() + 0.15f : 0f;

        float sampleRadius = Mathf.Clamp(landingSearchRadius, 0.5f, 2f);

        float[] angleOffsets =

        {

            0f, 30f, -30f, 60f, -60f, 90f, -90f,

            120f, -120f, 150f, -150f, 180f

        };

        for (int i = 0; i < angleOffsets.Length; i++)

        {

            Vector3 direction =

                Quaternion.Euler(0f, angleOffsets[i], 0f) * baseDirection;

            Vector3 candidate = protectPlayer

                ? player.position + direction * ring

                : preferredLanding + direction * (i == 0 ? 0f : 0.75f);

            if (!NavMesh.SamplePosition(

                    candidate,

                    out NavMeshHit hit,

                    sampleRadius,

                    agent.areaMask))

            {

                continue;

            }

            // Reject samples on the wrong floor/platform.

            if (Mathf.Abs(hit.position.y - targetPosition.y) > 1.25f)
                continue;

            if (protectPlayer &&
                GetHorizontalDistance(hit.position, player.position) <
                GetSafeCenterDistance())
            {
                continue;
            }

            // Do not choose a landing point inside the camera-to-Player corridor.
            if (protectPlayer && IsInsideCameraCorridor(hit.position))
            {
                continue;
            }

            if (GetHorizontalDistance(hit.position, transform.position) < 1f)
                continue;

            jumpStart = transform.position;
            jumpEnd = hit.position;
            jumpElapsed = 0f;

            Vector3 facing = jumpEnd - transform.position;
            facing.y = 0f;

            if (facing.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(facing.normalized);

            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;

            isJumping = true;
            nextJumpTime = Time.time + jumpCooldown;

            animator.ResetTrigger("Jump");
            animator.SetTrigger("Jump");

            stuckTimer = 0f;

            return true;
        }

        return false;
    }

    private void HandleInvestigation()
    {
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 1f;

        if (TryStartJumpToInvestigation())
            return;

        Vector3 destination = investigationPosition;

        if (player != null &&
            GetHorizontalDistance(player.position, investigationPosition) <
            GetSafeCenterDistance() * 1.5f)
        {
            Vector3 away = transform.position - player.position;
            away.y = 0f;

            if (away.sqrMagnitude < 0.001f)
                away = -transform.forward;

            away.Normalize();

            destination = player.position + away * GetSafeCenterDistance();
        }

        if (!TrySetReachableDestination(destination, 3f))
        {
            // The exact gunshot position may be unreachable,
            // especially when the Player is standing on a container.
            // Try finding a reachable NavMesh position nearby.
            if (!TryFindApproachPosition(destination))
            {
                isInvestigating = false;
                agent.isStopped = false;
                ChoosePatrolDestination();
                return;
            }
        }

        agent.isStopped = false;

        agent.isStopped = false;

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            FacePosition(investigationPosition);

            investigationTimer -= Time.deltaTime;

            if (investigationTimer <= 0f)
            {
                isInvestigating = false;
                agent.stoppingDistance = 2f;
                agent.isStopped = false;
                ChoosePatrolDestination();
            }
        }
    }

    private void HandlePatrol()
    {
        if (!agent.pathPending &&
            agent.hasPath &&
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

    private void HearGunshot(Vector3 shotPosition)
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (Vector3.Distance(transform.position, shotPosition) > hearingRadius)
            return;

        // Don't discard a heard shot just because the Player is also inside visual detection range.
        // Player detection takes priority on the next Update.
        investigationPosition = shotPosition;
        investigationTimer = investigationDuration;

        isInvestigating = true;
        isChasing = false;
        waitTimer = 0f;
        stuckTimer = 0f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
        }
    }

    private bool TrySetReachableDestination(
        Vector3 targetPosition,
        float sampleRadius = 6f)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        if (!NavMesh.SamplePosition(
                targetPosition,
                out NavMeshHit hit,
                sampleRadius,
                agent.areaMask))
        {
            return false;
        }

        // If this destination is near the Player, reject a sample inside the safety circle.
        if (player != null &&
            GetHorizontalDistance(targetPosition, player.position) <=
            GetSafeCenterDistance() * 1.5f &&
            GetHorizontalDistance(hit.position, player.position) <
            GetSafeCenterDistance())
        {
            return false;
        }

        NavMeshPath path = new NavMeshPath();

        if (!agent.CalculatePath(hit.position, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        if (agent.hasPath &&
            Vector3.Distance(agent.destination, hit.position) < 0.5f)
        {
            return true;
        }

        return agent.SetDestination(hit.position);
    }

    private bool TryFindApproachPosition(Vector3 targetPosition)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        float[] searchRadii = { 3f, 5f, 8f, 12f };

        for (int r = 0; r < searchRadii.Length; r++)
        {
            float radius = searchRadii[r];

            for (int i = 0; i < 16; i++)
            {
                float angle = i * 22.5f * Mathf.Deg2Rad;

                Vector3 candidate = targetPosition +
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius);

                candidate.y = transform.position.y;

                if (!NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit hit,
                        4f,
                        agent.areaMask))
                {
                    continue;
                }

                if (player != null &&
                    GetHorizontalDistance(hit.position, player.position) <
                    GetSafeCenterDistance())
                {
                    continue;
                }

                NavMeshPath path = new NavMeshPath();

                if (!agent.CalculatePath(hit.position, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                agent.isStopped = false;
                return agent.SetDestination(hit.position);
            }
        }

        return false;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 origin = transform.position + Vector3.up * visionHeight;
        Vector3 target = player.position + Vector3.up * visionHeight;

        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        if (Physics.Raycast(
                origin,
                direction.normalized,
                out RaycastHit hit,
                distance,
                visionMask,
                QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return true;
    }

    private void FacePlayer()
    {
        if (player != null)
            FacePosition(player.position);
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            facePlayerTurnSpeed * Time.deltaTime);
    }

    private void UpdateJump()
    {
        jumpElapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(
            jumpElapsed / Mathf.Max(jumpDuration, 0.1f));

        Vector3 targetPosition = Vector3.Lerp(jumpStart, jumpEnd, progress);

        targetPosition.y +=
            4f * jumpHeight * progress * (1f - progress);

        // Keep the jump trajectory outside the Player's protected horizontal radius.
        targetPosition = ClampOutsidePlayerSafetyCircle(targetPosition);

        Vector3 movement = targetPosition - transform.position;

        if (movement.magnitude > 0.001f &&
            IsJumpMovementBlocked(movement, movement.magnitude))
        {
            if (progress > 0.2f)
            {
                FinishJump();
                return;
            }
        }

        transform.position = targetPosition;

        if (progress >= 1f)
            FinishJump();
    }

    private Vector3 ClampOutsidePlayerSafetyCircle(Vector3 position)
    {
        if (player == null)
            return position;

        Vector3 offset = position - player.position;
        offset.y = 0f;

        float minDistance = GetSafeCenterDistance();

        if (offset.sqrMagnitude >= minDistance * minDistance)
            return position;

        if (offset.sqrMagnitude < 0.001f)
        {
            offset = transform.position - player.position;
            offset.y = 0f;
        }

        if (offset.sqrMagnitude < 0.001f)
            offset = -transform.forward;

        offset.Normalize();

        position.x = player.position.x + offset.x * minDistance;
        position.z = player.position.z + offset.z * minDistance;

        return position;
    }

    private void EnforceSafetyCircle()
    {

        if (EnforceCameraCorridor())
            return;

        if (isJumping || player == null || agent == null || !agent.isOnNavMesh)
            return;

        Vector3 offset = transform.position - player.position;
        offset.y = 0f;

        float minDistance = GetSafeCenterDistance();

        if (offset.sqrMagnitude >= minDistance * minDistance)
            return;

        if (offset.sqrMagnitude < 0.001f)
        {
            offset = -transform.forward;
            offset.y = 0f;
        }

        offset.Normalize();

        Vector3 safeTarget = player.position + offset * minDistance;
        safeTarget.y = transform.position.y;

        // Do not teleport the enemy when the target is blocked or off the NavMesh.
        // Let navigation find a valid route, or stop if no complete route exists.
        if (TrySetReachableDestination(safeTarget, 2f))
        {
            agent.isStopped = false;
        }
        else
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    private bool IsJumpMovementBlocked(Vector3 movement, float movementDistance)
    {
        Vector3 scale = transform.lossyScale;

        float radius = capsule.radius *
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));

        float height = Mathf.Max(
            capsule.height * Mathf.Abs(scale.y),
            radius * 2f);

        Vector3 center = transform.TransformPoint(capsule.center);

        float halfSegment = height * 0.5f - radius;

        Vector3 point1 = center + Vector3.up * halfSegment;
        Vector3 point2 = center - Vector3.up * halfSegment;

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

            if (player != null &&
                (hitTransform == player || hitTransform.IsChildOf(player)))
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

        Vector3 landing = ClampOutsidePlayerSafetyCircle(jumpEnd);

        if (NavMesh.SamplePosition(
                landing,
                out NavMeshHit hit,
                1.5f,
                agent.areaMask))
        {
            if (player == null ||
                GetHorizontalDistance(hit.position, player.position) >=
                GetSafeCenterDistance() - 0.05f)
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
            }
            else
            {
                // The sampled landing would violate the safety circle.
                transform.position = landing;
                agent.Warp(landing);
            }
        }
        else
        {
            Debug.LogWarning(
                "EnemyAI: Could not find a valid NavMesh position after the jump.",
                this);
        }

        agent.updatePosition = true;
        agent.updateRotation = true;

        isJumping = false;
        stuckTimer = 0f;
        lastStuckCheckPosition = transform.position;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;

            if (player != null && isChasing)
            {
                agent.speed = chaseSpeed;
                agent.stoppingDistance = 0.2f;

                TrySetReachableDestination(GetPlayerApproachTarget(), 3f);
            }
        }
    }

    private void StopJumpIfNeeded()
    {
        if (isJumping)
            FinishJump();
    }

    private void MonitorStuck()
    {
        if (isJumping || !agent.isOnNavMesh)
        {
            ResetStuckTimer();
            return;
        }

        float horizontalDistanceToPlayer =
            player != null
                ? GetHorizontalDistance(transform.position, player.position)
                : 0f;

        bool chasingNeedsMovement =
            isChasing &&
            player != null &&
            horizontalDistanceToPlayer > GetSafeCenterDistance() + 0.75f;

        bool pathNeedsMovement =
            !agent.isStopped &&
            (agent.pathPending ||
             (agent.hasPath &&
              agent.remainingDistance > agent.stoppingDistance + 0.5f));

        bool noPathButNeedsMovement =
            !agent.isStopped &&
            !agent.hasPath &&
            (isChasing || isInvestigating);

        bool shouldBeMoving =
            chasingNeedsMovement ||
            pathNeedsMovement ||
            noPathButNeedsMovement;

        if (!shouldBeMoving)
        {
            ResetStuckTimer();
            return;
        }

        float movedDistance =
            Vector3.Distance(transform.position, lastStuckCheckPosition);

        lastStuckCheckPosition = transform.position;

        if (movedDistance >= minimumMovementToResetStuck)
        {
            stuckTimer = 0f;
            return;
        }

        stuckTimer += Time.deltaTime;

        if (stuckTimer >= stuckTime && Time.time >= nextRecoveryTime)
        {
            stuckTimer = 0f;
            nextRecoveryTime = Time.time + recoveryCooldown;

            RecoverMovement();
        }
    }

    private void RecoverMovement()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;

        if (isChasing && player != null)
        {
            float horizontalDistance =
                GetHorizontalDistance(transform.position, player.position);

            float verticalDifference =
                Mathf.Abs(player.position.y - transform.position.y);

            bool withinJumpRange =
                horizontalDistance > GetSafeCenterDistance() &&
                horizontalDistance <= jumpMaxDistance;

            if (verticalDifference >= elevatedJumpThreshold &&
                withinJumpRange &&
                Time.time >= nextJumpTime &&
                TryStartJumpToPlayer())
            {
                return;
            }

            if (TrySetReachableDestination(GetPlayerApproachTarget(), 3f))
                return;

            if (TryFindApproachPosition(GetPlayerApproachTarget()))
                return;

            if (agent.hasPath)
                agent.ResetPath();

            FacePlayer();
            return;
        }

        if (isInvestigating)
        {
            if (TryStartJumpToInvestigation() ||
                TrySetReachableDestination(investigationPosition, 3f))
            {
                return;
            }

            isInvestigating = false;
        }

        ChoosePatrolDestination();
    }

    private void ResetStuckTimer()
    {
        stuckTimer = 0f;
        lastStuckCheckPosition = transform.position;
    }

    private void ChoosePatrolDestination()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPoint = transform.position +
                new Vector3(
                    Random.Range(-patrolRadius, patrolRadius),
                    0f,
                    Random.Range(-patrolRadius, patrolRadius));

            if (NavMesh.SamplePosition(
                    randomPoint,
                    out NavMeshHit hit,
                    patrolRadius,
                    agent.areaMask))
            {
                if (TrySetReachableDestination(hit.position, patrolRadius))
                    return;
            }
        }

        Debug.LogWarning("EnemyAI: Could not find a reachable patrol destination.");
    }

    private float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }
}