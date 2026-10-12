using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    public Transform player;
    public float detectionRange = 20f;
    public float rotationSpeed = 5f;
    public LayerMask VisionMask;
    public Transform FirePoint;
    public float FireCooldown = 1.5f;
    public int Damage = 10;
    public PlayerHealth playerHealth;
    public ParticleSystem EnemyMuzzleFlash;
    public AudioSource EnemyAudio;

    float NextFireTime = 0f;
    bool PlayerDetected = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float DistanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool CurrentlyDetected = DistanceToPlayer <= detectionRange;
        if (CurrentlyDetected && !PlayerDetected)
        {
            Debug.Log("Player Detected");
        }
        if (!CurrentlyDetected && PlayerDetected)
        {
            Debug.Log("Player Lost");
        }
        if (CurrentlyDetected)
        {
            Vector3 DirectionToPlayer = (player.position - transform.position);
            DirectionToPlayer.y = 0f;

            RaycastHit Hit;
            bool CanSeePlayer = !Physics.Raycast(transform.position, DirectionToPlayer.normalized, out Hit, DistanceToPlayer, VisionMask);


            if (DirectionToPlayer != Vector3.zero && CanSeePlayer)
            {
                Quaternion TargetRotation = Quaternion.LookRotation(DirectionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation, rotationSpeed * Time.deltaTime);

                if (Time.time >= NextFireTime && !playerHealth.IsPlayerDead())
                {
                    Shoot();
                }
            }
            
        }
        
        PlayerDetected = CurrentlyDetected;
    }
    void Shoot()
    {
        Debug.Log("Enemy Fired");

        EnemyMuzzleFlash.Play();
        EnemyAudio.Play();

        playerHealth.TakeDamage(Damage);

        NextFireTime = Time.time + FireCooldown;
    }
}
