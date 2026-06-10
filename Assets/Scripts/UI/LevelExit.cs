using UnityEngine;

/// <summary>
/// Trigger que carga la escena final al ser tocado por el jugador.
/// Agregar un Collider con Is Trigger = true en el mismo GameObject.
/// </summary>
public class LevelExit : MonoBehaviour
{
    [SerializeField] private string endSceneName = "EndScene";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SceneFader.Instance.LoadScene(endSceneName);
    }
}
