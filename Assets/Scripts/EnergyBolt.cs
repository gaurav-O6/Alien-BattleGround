
using UnityEngine;

public class EnergyBolt : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float Speed = 25f;
    public float Lifetime = 3f;
    public int Damage = 10;

    private Vector3 Direction;
    private Transform Owner;
    private bool HasHit = false;

    public void Initialize(Vector3 firingDirection, Transform owner)
    {
        Direction = firingDirection.normalized;
        Owner = owner;

        if (Direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(Direction);
        }

        Destroy(gameObject, Lifetime);
    }

    private void Update()
    {
        if (HasHit)
            return;

        transform.position += Direction * Speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasHit)
            return;

        // Ignore the alien that fired this projectile.
        if (Owner != null &&
            (other.transform == Owner ||
             other.transform.IsChildOf(Owner)))
        {
            return;
        }

        // Check whether the projectile hit the player.
        PlayerHealth HitPlayer =
            other.GetComponentInParent<PlayerHealth>();

        if (HitPlayer != null)
        {
            // Don't damage a player who is already dead.
            if (HitPlayer.IsPlayerDead())
                return;

            HasHit = true;

            HitPlayer.TakeDamage(Damage);

            Destroy(gameObject);
            return;
        }

        // Any other collider, such as a wall or obstacle,
        // destroys the projectile without damaging the player.
        HasHit = true;
        Destroy(gameObject);
    }
}
