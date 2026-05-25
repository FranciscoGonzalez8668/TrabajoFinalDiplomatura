/// <summary>
/// Interfaz para cualquier componente que pueda ser activado, desactivado y reseteado.
/// Cada implementación define qué significa Play/Stop/Reset para ese objeto.
///
/// Ejemplos:
///   ObjectMover   → Play = mover, Stop = pausar, Reset = volver al origen
///   ToggleObject  → Play = aparecer, Stop = desaparecer, Reset = volver al estado inicial
///   Cualquier otra cosa con lógica de activación propia
/// </summary>
public interface IActivatable
{
    void Play();
    void Stop();
    void Reset();
}
