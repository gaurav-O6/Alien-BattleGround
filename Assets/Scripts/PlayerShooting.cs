
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public float ShootRange = 100f;
    public LayerMask ShootingMask;
    public GunController Gun;
    public PlayerHealth PlayerHealth;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) &&
            !PlayerHealth.IsPlayerDead())
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Gun.Fire();

        Camera MainCamera = Camera.main;

        if (MainCamera == null)
        {
            Debug.LogError("PlayerShooting: No MainCamera found!");
            return;
        }

        Vector3 rayOrigin = new Vector3(0.5f, 0.5f, 0f);
        Ray ShootRay = MainCamera.ViewportPointToRay(rayOrigin);

        RaycastHit Hit;

        if (Physics.Raycast(
            ShootRay,
            out Hit,
            ShootRange,
            ShootingMask))
        {
            Debug.Log("Shot hit: " + Hit.collider.name);

            EnemyHealth enemyHealth =
                Hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                Debug.Log(
                    "EnemyHealth found on: " +
                    enemyHealth.gameObject.name
                );

                enemyHealth.TakeDamage(10);
            }
            else
            {
                Debug.LogWarning(
                    "No EnemyHealth found on hit object or its parents: " +
                    Hit.collider.name
                );
            }

            Target target = Hit.collider.GetComponent<Target>();

            if (target != null)
            {
                target.Hit();
            }
        }
        else
        {
            Debug.Log("Shot missed");
        }
    }
}
