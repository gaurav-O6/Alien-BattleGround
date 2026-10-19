
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

    [Header("Legacy Weapon Effects")]
    public ParticleSystem EnemyMuzzleFlash;
    public AudioSource EnemyAudio;

    private float NextFireTime = 0f;
    private bool PlayerDetected = false;

    void Update()
    {
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
        if (EnemyGun == null)
        {
            Debug.LogWarning(
                "EnemyShooter: EnemyGun is not assigned!",
                this);

            return;
        }

        Debug.Log("Enemy Fired");

        EnemyGun.Fire(Damage, transform);

        NextFireTime = Time.time + FireCooldown;
    }
}
