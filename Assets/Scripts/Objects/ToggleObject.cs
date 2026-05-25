using UnityEngine;

/// <summary>
/// Hace aparecer y desaparecer un objeto (renderer + collider).
/// Implementa IActivatable para ser controlado por ActivatorSwitch.
///
/// Play()  → aparece
/// Stop()  → desaparece
/// Reset() → vuelve al estado inicial configurado en el Inspector
/// </summary>
public class ToggleObject : MonoBehaviour, IActivatable
{
    [SerializeField] private bool startVisible = true;

    private Renderer[]  rends;
    private Collider[]  cols;

    private void Awake()
    {
        rends = GetComponentsInChildren<Renderer>();
        cols  = GetComponentsInChildren<Collider>();
        SetVisible(startVisible);
    }

    public void Play()  => SetVisible(true);
    public void Stop()  => SetVisible(false);
    public void Reset() => SetVisible(startVisible);

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in rends) r.enabled = visible;
        foreach (Collider c in cols)  c.enabled = visible;
    }
}
