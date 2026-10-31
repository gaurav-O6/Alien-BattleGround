using UnityEngine;

public class EnemyGunController : MonoBehaviour
{
    [Header("Blaster Parts")]
    public Transform Slide;
    public Transform FirePoint;

    [Header("Projectile")]
    public EnergyBolt EnergyBoltPrefab;
    public float ProjectileSpeed = 25f;

    [Header("Blaster Effects")]
    public AudioSource GunAudio;
    public ParticleSystem MuzzleFlash;

    [Header("Slide Recoil")]
    public float RecoilDistance = 0.04f;
    public float RecoilRecoverySpeed = 0.1f;

    private Vector3 DefaultSlidePosition;
    private float RecoilAmount = 0f;

    private void Start()
    {
        if (Slide == null)
        {
            Debug.LogError(
                "EnemyGunController: Slide is not assigned!", this);
            enabled = false;
            return;
        }

        if (FirePoint == null)
        {
            Debug.LogError(
                "EnemyGunController: FirePoint is not assigned!", this);
        }

        if (EnergyBoltPrefab == null)
        {
            Debug.LogError(
                "EnemyGunController: EnergyBolt prefab is not assigned!", this);
        }

        DefaultSlidePosition = Slide.localPosition;
    }

    private void LateUpdate()
    {
        if (Slide == null)
            return;

        RecoilAmount = Mathf.MoveTowards(
            RecoilAmount,
            0f,
            RecoilRecoverySpeed * Time.deltaTime);

        Slide.localPosition =
            DefaultSlidePosition + Vector3.back * RecoilAmount;
    }

    public void Fire()
    {
        Fire(10, transform.root);
    }

    public void Fire(int damage, Transform owner)
    {
        if (FirePoint == null)
            return;

        Fire(damage, owner, FirePoint.position + FirePoint.forward * 10f);
    }

    public void Fire(int damage, Transform owner, Vector3 targetPosition)
    {
        if (!enabled || FirePoint == null || EnergyBoltPrefab == null)
            return;

        RecoilAmount = RecoilDistance;

        if (GunAudio != null)
            GunAudio.Play();

        if (MuzzleFlash != null)
            MuzzleFlash.Play();

        Vector3 firingDirection =
            targetPosition - FirePoint.position;

        if (firingDirection.sqrMagnitude < 0.001f)
            return;

        EnergyBolt projectile = Instantiate(
            EnergyBoltPrefab,
            FirePoint.position,
            Quaternion.identity
        );

        projectile.Speed = ProjectileSpeed;
        projectile.Damage = damage;
        projectile.Initialize(firingDirection, owner);
    }
}