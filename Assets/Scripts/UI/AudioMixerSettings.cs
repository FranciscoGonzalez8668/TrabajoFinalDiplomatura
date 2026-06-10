using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Conecta los sliders de volumen del menú con el AudioMixer.
/// Cada slider va de 0 a 1 y se convierte a decibeles para el mixer.
///
/// Setup en Unity:
/// 1. Asignar el GameAudioMixer al campo AudioMixer
/// 2. Asignar cada Slider al campo correspondiente
/// 3. En cada Slider → OnValueChanged → este script → el método correspondiente
/// </summary>
public class AudioMixerSettings : MonoBehaviour
{
    public static AudioMixerSettings Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private const string MasterParam = "MasterVolume";
    private const string SFXParam    = "SFXVolume";
    private const string MusicParam  = "MusicVolume";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Aplicar los valores guardados al mixer apenas arranca
        ApplyVolume(MasterParam, PlayerPrefs.GetFloat(MasterParam, 1f));
        ApplyVolume(SFXParam,    PlayerPrefs.GetFloat(SFXParam,    1f));
        ApplyVolume(MusicParam,  PlayerPrefs.GetFloat(MusicParam,  1f));
    }

    private void Start()
    {
        // Sincronizar los sliders con los valores guardados
        masterSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(MasterParam, 1f));
        sfxSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(SFXParam,    1f));
        musicSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicParam,  1f));
    }

    // -------------------------------------------------------
    // Llamados desde OnValueChanged de cada Slider
    // -------------------------------------------------------

    public void OnMasterVolumeChanged(float value)
    {
        ApplyVolume(MasterParam, value);
        PlayerPrefs.SetFloat(MasterParam, value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        ApplyVolume(SFXParam, value);
        PlayerPrefs.SetFloat(SFXParam, value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        ApplyVolume(MusicParam, value);
        PlayerPrefs.SetFloat(MusicParam, value);
    }

    // -------------------------------------------------------
    // Conversión lineal → decibeles
    // -------------------------------------------------------

    private void ApplyVolume(string parameter, float linearValue)
    {
        if (audioMixer == null) return;
        // Convertir 0-1 a decibeles: 0 = -80dB (silencio), 1 = 0dB (máximo)
        float db = linearValue > 0.0001f ? Mathf.Log10(linearValue) * 20f : -80f;
        audioMixer.SetFloat(parameter, db);
    }
}
