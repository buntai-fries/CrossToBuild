using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;

    [Tooltip("Pitch at Round 1.")]
    [SerializeField] private float basePitch = 1f;

    [Tooltip("How much pitch increases per round. 0.1 = 10% faster each round.")]
    [SerializeField] private float pitchIncreasePerRound = 0.1f;

    [Tooltip("Maximum pitch cap — music won't go faster than this.")]
    [SerializeField] private float maxPitch = 2f;

    [Header("SFX (optional)")]
    [SerializeField] private AudioClip orbCollectSFX;
    [SerializeField] private AudioClip roundCompleteSFX;
    [SerializeField] private AudioClip gameOverSFX;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    // ─── Runtime ─────────────────────────────────────────────────────────────

    private AudioSource musicSource;
    private int lastKnownRound = 1;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        musicSource = GetComponent<AudioSource>();

        // ── Configure music source ────────────────────────────────────────────
        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.pitch = basePitch;
        musicSource.playOnAwake = false;

        if (musicClip != null)
        {
            musicSource.Play();
            Debug.Log($"[AudioManager] Music started — pitch={musicSource.pitch:F2}");
        }
        else
        {
            Debug.LogWarning("[AudioManager] No music clip assigned! " +
                             "Drag a music AudioClip into the Music Clip slot.");
        }
    }

    void Update()
    {
        if (RoundManager.Instance == null) return;

        int currentRound = RoundManager.Instance.CurrentRound;

        // Only update pitch when round changes — not every frame
        if (currentRound != lastKnownRound)
        {
            lastKnownRound = currentRound;
            UpdatePitch(currentRound);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    void UpdatePitch(int round)
    {
        float targetPitch = Mathf.Min(
            maxPitch,
            basePitch + (round - 1) * pitchIncreasePerRound);

        musicSource.pitch = targetPitch;

        Debug.Log($"[AudioManager] Round {round} — pitch={targetPitch:F2}");
    }

    // ─── SFX Public API ───────────────────────────────────────────────────────

    /// <summary>Call from Player.CollectOrb()</summary>
    public void PlayOrbCollect()
    {
        PlaySFX(orbCollectSFX);
    }

    /// <summary>Call from RoundManager.OnBuildChosen()</summary>
    public void PlayRoundComplete()
    {
        PlaySFX(roundCompleteSFX);
    }

    /// <summary>Call from GameOverManager.ShowGameOver()</summary>
    public void PlayGameOver()
    {
        // Stop music and play game over sting
        musicSource.Stop();
        PlaySFX(gameOverSFX);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        // PlayOneShot doesn't interrupt music source
        musicSource.PlayOneShot(clip, sfxVolume);
    }
}
