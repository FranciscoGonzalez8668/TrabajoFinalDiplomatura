using UnityEngine;

/// <summary>
/// Centraliza todos los efectos de sonido del jugador.
/// Cada ability llama al método correspondiente — este script
/// decide cómo y cuándo reproducirlo.
/// Agregar un AudioSource en el mismo GameObject (se crea automáticamente si falta).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSFX : MonoBehaviour
{
    [Header("Salto")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    [Header("Wall Run")]
    [SerializeField] private AudioClip wallRunStartClip;
    [SerializeField] private AudioClip wallRunLoopClip;   // opcional — se reproduce en loop mientras corre
    [SerializeField] private AudioClip wallRunStopClip;

    [Header("Wall Jump")]
    [SerializeField] private AudioClip wallJumpClip;

    [Header("Ledge Grab")]
    [SerializeField] private AudioClip ledgeGrabClip;
    [SerializeField] private AudioClip ledgeJumpClip;
    [SerializeField] private AudioClip ledgeClimbClip;

    [Header("Vertical Wall Run")]
    [SerializeField] private AudioClip verticalWallRunClip;

    [Header("Muerte")]
    [SerializeField] private AudioClip deathClip;

    [Header("Volumen")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private AudioSource audioSource;
    private AudioSource loopSource;  // fuente separada para el loop del wall run

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Segunda fuente para el sonido en loop (wall run)
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop        = true;
        loopSource.volume      = sfxVolume;
    }

    // -------------------------------------------------------
    // Salto
    // -------------------------------------------------------

    public void PlayJump()       => Play(jumpClip);
    public void PlayLand()       => Play(landClip);

    // -------------------------------------------------------
    // Wall Run
    // -------------------------------------------------------

    public void PlayWallRunStart()
    {
        Play(wallRunStartClip);

        if (wallRunLoopClip != null && !loopSource.isPlaying)
        {
            loopSource.clip   = wallRunLoopClip;
            loopSource.volume = sfxVolume;
            loopSource.Play();
        }
    }

    public void PlayWallRunStop()
    {
        loopSource.Stop();
        Play(wallRunStopClip);
    }

    // -------------------------------------------------------
    // Wall Jump
    // -------------------------------------------------------

    public void PlayWallJump() => Play(wallJumpClip);

    // -------------------------------------------------------
    // Ledge Grab
    // -------------------------------------------------------

    public void PlayLedgeGrab()  => Play(ledgeGrabClip);
    public void PlayLedgeJump()  => Play(ledgeJumpClip);
    public void PlayLedgeClimb() => Play(ledgeClimbClip);

    // -------------------------------------------------------
    // Vertical Wall Run
    // -------------------------------------------------------

    public void PlayVerticalWallRun() => Play(verticalWallRunClip);

    // -------------------------------------------------------
    // Muerte
    // -------------------------------------------------------

    public void PlayDeath() => Play(deathClip);

    // -------------------------------------------------------
    // Interno
    // -------------------------------------------------------

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, sfxVolume);
    }
}
