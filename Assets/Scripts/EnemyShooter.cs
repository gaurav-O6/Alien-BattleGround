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

    private float NextFireTime = 0f;
    private bool PlayerDetected = false;

    void Update()
    {
        // Stop all shooting logic as soon as this enemy starts dying.
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (player == null || playerHealth == null)
            return;

        float DistanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        bool CurrentlyDetected =
            DistanceToPlayer <= detectionRange;

        if (CurrentlyDetected && !PlayerDetected)
            Debug.Log("Player Detected");

        if (!CurrentlyDetected && PlayerDetected)
            Debug.Log("Player Lost");

        if (CurrentlyDetected)
        {
            Vector3 DirectionToPlayer =
                player.position - transform.position;

            DirectionToPlayer.y = 0f;

            RaycastHit Hit;

            bool CanSeePlayer = !Physics.Raycast(
                transform.position,
                DirectionToPlayer.normalized,
                out Hit,
                DistanceToPlayer,
                VisionMask);

            if (DirectionToPlayer != Vector3.zero && CanSeePlayer)
            {
                Quaternion TargetRotation =
                    Quaternion.LookRotation(DirectionToPlayer);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    TargetRotation,
                    rotationSpeed * Time.deltaTime);

                if (Time.time >= NextFireTime &&
                    !playerHealth.IsPlayerDead())
                {
                    Shoot();
                }
            }
        }

        PlayerDetected = CurrentlyDetected;
    }

    void Shoot()
    {
        // Extra safety check in case Shoot() is called after death.
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (EnemyGun == null)
        {
            Debug.LogWarning(
                "EnemyShooter: EnemyGun is not assigned!",
                this);

            return;
        }

        Debug.Log("Enemy Fired");

        Vector3 targetPosition = player.position + Vector3.up * 1.2f;

        EnemyGun.Fire(Damage, transform, targetPosition);

        if (EnemyMuzzleFlash != null)
        {
            EnemyMuzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            EnemyMuzzleFlash.Play();
        }

        NextFireTime = Time.time + FireCooldown;
    }
}