using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public float ShootRange = 100f;
    public LayerMask ShootingMask;
    public GunController Gun;
    public PlayerHealth PlayerHealth;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !PlayerHealth.IsPlayerDead())
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Gun.Fire();

        Camera MainCamera = Camera.main;

        Ray ShootRay = MainCamera.ViewportPointToRay (new Vector3 (0.5F, 0.5F, 0));

        RaycastHit Hit;
        if (Physics.Raycast(ShootRay,out Hit, ShootRange, ShootingMask))
        {
            Target target = Hit.collider.GetComponent<Target>();

            if (target != null)
            {
                target.Hit();
            }
            Debug.Log ("Shot hit: " + Hit.collider.name);

            EnemyHealth enemyHealth = Hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(10); // Replace 10 with the actual damage value
            }
        }
        else
        {
            Debug.Log("Shot missed");
        }
    }
}
