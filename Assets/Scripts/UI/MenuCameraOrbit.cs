using UnityEngine;

/// <summary>
/// Rota la cámara alrededor de un punto fijo en Y.
/// Colocar en la cámara del menú y asignar el punto central a orbitar
/// (puede ser el centro del escenario o el jugador de fondo).
/// </summary>
public class MenuCameraOrbit : MonoBehaviour
{
    [Tooltip("Punto alrededor del cual orbita la cámara. Si es null usa el origen.")]
    [SerializeField] private Transform target;

    [SerializeField] private float orbitSpeed = 10f;

    private void Update()
    {
        Vector3 pivot = target != null ? target.position : Vector3.zero;
        transform.RotateAround(pivot, Vector3.up, orbitSpeed * Time.deltaTime);
        transform.LookAt(pivot);
    }
}
