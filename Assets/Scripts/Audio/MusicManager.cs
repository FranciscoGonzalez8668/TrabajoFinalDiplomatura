using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton que persiste entre escenas y reproduce música de fondo.
/// Colocar en un GameObject vacío en la primera escena (MainMenu).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.4f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.clip        = musicClip;
        audioSource.loop        = true;
        audioSource.volume      = volume;
        audioSource.playOnAwake = false;

        if (musicMixerGroup != null)
            audioSource.outputAudioMixerGroup = musicMixerGroup;

        if (musicClip != null)
            audioSource.Play();
    }
}
