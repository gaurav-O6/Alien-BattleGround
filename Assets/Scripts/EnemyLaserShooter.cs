
using UnityEngine;

public class EnemyLaserShooter : MonoBehaviour
{
    public Transform LaserFirePoint;
    public float FireCooldown = 1.5f;
    public float LaserRange = 30f;
    public int Damage = 10;
    public float BeamDuration = 0.12f;
    public float detectionRange = 15f;

    private float NextFireTime;
    private LineRenderer LaserBeam;
    private float BeamTimer;
    private Transform Player;
    private PlayerHealth playerHealth;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            Player = playerObject.transform;
            playerHealth = playerObject.GetComponent<PlayerHealth>();
        }

        GameObject beamObject = new GameObject("LaserBeam");
        LaserBeam = beamObject.AddComponent<LineRenderer>();

        LaserBeam.material = new Material(Shader.Find("Sprites/Default"));
        LaserBeam.startColor = Color.cyan;
        LaserBeam.endColor = Color.blue;
        LaserBeam.startWidth = 0.04f;
        LaserBeam.endWidth = 0.015f;
        LaserBeam.positionCount = 2;
        LaserBeam.useWorldSpace = true;
        LaserBeam.enabled = false;
    }

    void Update()
    {
        if (LaserBeam != null && LaserBeam.enabled)
        {
            BeamTimer -= Time.deltaTime;

            if (BeamTimer <= 0f)
            {
                LaserBeam.enabled = false;
            }
        }

        if (LaserFirePoint == null || Player == null)
        {
            return;
        }

        if (playerHealth != null && playerHealth.IsPlayerDead())
        {
            return;
        }

        if (Vector3.Distance(transform.position, Player.position) > detectionRange)
        {
            return;
        }

        if (Time.time >= NextFireTime)
        {
            ShootLaser();
            NextFireTime = Time.time + FireCooldown;
        }
    }

    void ShootLaser()
    {
        Vector3 origin = LaserFirePoint.position;
        Vector3 direction = -LaserFirePoint.right;

        Vector3 endPoint = origin + direction * LaserRange;
        int laserLayerMask = ~LayerMask.GetMask("Enemy");

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            LaserRange,
            laserLayerMask))
        {
            endPoint = hit.point;

            PlayerHealth hitPlayerHealth =
                hit.collider.GetComponentInParent<PlayerHealth>();

            if (hitPlayerHealth != null && !hitPlayerHealth.IsPlayerDead())
            {
                hitPlayerHealth.TakeDamage(Damage);
            }
        }

        LaserBeam.SetPosition(0, origin);
        LaserBeam.SetPosition(1, endPoint);
        LaserBeam.enabled = true;
        BeamTimer = BeamDuration;
    }
}
