using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puente entre los sliders del pause menu (que viven en un prefab)
/// y el AudioMixerSettings singleton.
/// Poner este script en el panel de settings del canvas de pausa.
/// Los sliders apuntan a este componente en su OnValueChanged.
/// </summary>
public class AudioSettingsBridge : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private void OnEnable()
    {
        // Sincronizar sliders con los valores actuales al abrir el panel
        if (AudioMixerSettings.Instance == null) return;

        masterSlider?.SetValueWithoutNotify(AudioMixerSettings.Instance.GetMasterVolume());
        sfxSlider?.SetValueWithoutNotify(AudioMixerSettings.Instance.GetSFXVolume());
        musicSlider?.SetValueWithoutNotify(AudioMixerSettings.Instance.GetMusicVolume());
    }

    // Asignar estos métodos en el OnValueChanged de cada slider
    public void OnMasterVolumeChanged(float value) => AudioMixerSettings.Instance?.OnMasterVolumeChanged(value);
    public void OnSFXVolumeChanged(float value)    => AudioMixerSettings.Instance?.OnSFXVolumeChanged(value);
    public void OnMusicVolumeChanged(float value)  => AudioMixerSettings.Instance?.OnMusicVolumeChanged(value);
}
