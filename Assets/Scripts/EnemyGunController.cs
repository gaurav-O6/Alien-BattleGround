
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
    public float RecoilDistance = 0.01f;
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
        if (!enabled || FirePoint == null || EnergyBoltPrefab == null)
            return;

        RecoilAmount = RecoilDistance;

        if (GunAudio != null)
            GunAudio.Play();

        if (MuzzleFlash != null)
            MuzzleFlash.Play();

        EnergyBolt projectile = Instantiate(
            EnergyBoltPrefab,
            FirePoint.position,
            Quaternion.identity
        );

        projectile.Speed = ProjectileSpeed;
        projectile.Damage = damage;
        projectile.Initialize(FirePoint.forward, owner);
    }
}
