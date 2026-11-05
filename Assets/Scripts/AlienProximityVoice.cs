using UnityEngine;

public class AlienProximityVoice : MonoBehaviour
{
    [Header("References")]
    public Transform Player;
    public AudioSource VoiceAudio;

    [Header("Proximity Settings")]
    public float StartDistance = 8f;
    public float StopDistance = 10f;

    [Header("Voice Timing")]
    public float MinimumDelay = 4f;
    public float MaximumDelay = 8f;

    [Header("Fade Settings")]
    public float FadeOutDuration = 0.5f;

    private float NextVoiceTime;
    private float OriginalVolume;
    private float FadeTimer;
    private bool IsFadingOut;

    private void Awake()
    {
        if (VoiceAudio == null)
        {
            VoiceAudio = GetComponent<AudioSource>();
        }

        if (VoiceAudio != null)
        {
            OriginalVolume = VoiceAudio.volume;
        }
    }

    private void Update()
    {
        if (Player == null || VoiceAudio == null || VoiceAudio.clip == null)
        {
            return;
        }

        float Distance = Vector3.Distance(
            transform.position,
            Player.position
        );

        if (VoiceAudio.isPlaying)
        {
            if (Distance > StopDistance)
            {
                IsFadingOut = true;
                FadeTimer += Time.deltaTime;

                float FadeProgress = Mathf.Clamp01(
                    FadeTimer / FadeOutDuration
                );

                VoiceAudio.volume = Mathf.Lerp(
                    OriginalVolume,
                    0f,
                    FadeProgress
                );

                if (FadeProgress >= 1f)
                {
                    VoiceAudio.Stop();
                    VoiceAudio.volume = OriginalVolume;

                    IsFadingOut = false;
                    FadeTimer = 0f;

                    NextVoiceTime = Time.time
                        + Random.Range(MinimumDelay, MaximumDelay);
                }
            }
            else if (IsFadingOut)
            {
                IsFadingOut = false;
                FadeTimer = 0f;
                VoiceAudio.volume = OriginalVolume;
            }

            return;
        }

        if (Distance <= StartDistance && Time.time >= NextVoiceTime)
        {
            VoiceAudio.volume = OriginalVolume;
            VoiceAudio.Play();

            NextVoiceTime = Time.time
                + VoiceAudio.clip.length
                + Random.Range(MinimumDelay, MaximumDelay);
        }
    }
}