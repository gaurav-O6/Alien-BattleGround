using UnityEngine;

public class GunController : MonoBehaviour
{
    public Transform Slide;
    public AudioSource GunAudio;

    public ParticleSystem MuzzleFlash;
    public PlayerCamera PlayerCamera;
    public float RecoilDistance = 0.0005f;
    public float RecoilRecoverySpeed = 0.005f;

    Vector3 DefaultSlidePosition;
    float RecoilAmount = 0f;
    void Start()
    {
        DefaultSlidePosition = Slide.localPosition;
    }


    void LateUpdate()
    {
        RecoilAmount = Mathf.MoveTowards(RecoilAmount, 0f, RecoilRecoverySpeed * Time.deltaTime);

        Slide.localPosition = DefaultSlidePosition + Vector3.up * RecoilAmount;
    }

    public void Fire()
    {
        Debug.Log("Gun Fire Called!");

        RecoilAmount = RecoilDistance;
        GunAudio.Play();

        if (MuzzleFlash != null)
        {
            MuzzleFlash.Play();
            Debug.Log("Muzzle Flash Playing: " + MuzzleFlash.isPlaying);
        }

        PlayerCamera.Kick();
    }
}
