using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Player Detection")]
    public Transform player;
    public float detectionRange = 20f;
    public float rotationSpeed = 5f;
    public LayerMask VisionMask;

    [Header("Weapon")]
    public EnemyGunController EnemyGun;
    public Transform FirePoint;
    public float FireCooldown = 1.5f;

    [Header("Player Damage")]
    public int Damage = 10;
    public PlayerHealth playerHealth;

    [Header("Enemy Health")]
    public EnemyHealth enemyHealth;

    [Header("Weapon Effects")]
    public ParticleSystem EnemyMuzzleFlash;
    public AudioSource EnemyAudio;

    private float NextFireTime;
    private bool PlayerDetected;

    private void Update()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (player == null || playerHealth == null || EnemyGun == null)
            return;

        float distanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        bool currentlyDetected = distanceToPlayer <= detectionRange;

        if (currentlyDetected && !PlayerDetected)
            Debug.Log("Player Detected");

        if (!currentlyDetected && PlayerDetected)
            Debug.Log("Player Lost");

        if (currentlyDetected)
        {
            Vector3 eyePosition =
                transform.position + Vector3.up * 1.2f;

            Vector3 targetPosition =
                player.position + Vector3.up * 1.2f;

            Vector3 directionToPlayer =
                targetPosition - eyePosition;

            float rayDistance = directionToPlayer.magnitude;

            bool canSeePlayer = false;

            if (rayDistance > 0.001f)
            {
                canSeePlayer = Physics.Raycast(
                    eyePosition,
                    directionToPlayer.normalized,
                    out RaycastHit hit,
                    rayDistance,
                    VisionMask,
                    QueryTriggerInteraction.Ignore
                ) && (
                    hit.transform == player ||
                    hit.transform.IsChildOf(player)
                );
            }

            if (canSeePlayer)
            {
                Vector3 flatDirection = player.position - transform.position;
                flatDirection.y = 0f;

                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation =
                        Quaternion.LookRotation(flatDirection);

                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }

                if (Time.time >= NextFireTime &&
                    !playerHealth.IsPlayerDead())
                {
                    TryShoot();
                }
            }
        }

        PlayerDetected = currentlyDetected;
    }

    private void TryShoot()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        Transform muzzle = FirePoint != null
            ? FirePoint
            : EnemyGun.FirePoint;

        if (muzzle == null)
        {
            Debug.LogWarning(
                "EnemyShooter: No FirePoint assigned!",
                this);

            return;
        }

        Shoot(muzzle);
    }

    private void Shoot(Transform muzzle)
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (player == null)
            return;

        Debug.Log("Enemy Fired");

        // Aim toward the player's current position, including height.
        Vector3 projectileTarget =
            player.position + Vector3.up * 1.2f;

        EnemyGun.Fire(
            Damage,
            transform,
            projectileTarget
        );

        if (EnemyMuzzleFlash != null)
        {
            EnemyMuzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            EnemyMuzzleFlash.Play();
        }

        NextFireTime = Time.time + FireCooldown;
    }
}