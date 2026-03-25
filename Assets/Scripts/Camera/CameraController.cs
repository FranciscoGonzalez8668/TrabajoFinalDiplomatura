using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform target;          // CameraTarget del Player
    [SerializeField] private Transform playerBody;      // Transform del Player

    [Header("Configuración")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float distance = 4f;       // distancia al personaje
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 60f;
    [SerializeField] private float rotationSmoothSpeed = 10f;
    [SerializeField] private float playerRotationSpeed = 10f;

    [Header("Colisión")]
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private LayerMask collisionLayers;  // qué layers esquiva la cámara

    // --- Estado interno ---
    private float yaw;    // rotación horizontal acumulada
    private float pitch;  // rotación vertical acumulada

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Iniciamos con la rotación actual del player
        yaw = playerBody.eulerAngles.y;
    }

    void LateUpdate()
    {
        // LateUpdate garantiza que la cámara se mueve DESPUÉS del personaje
        HandleRotation();
        HandlePosition();
        HandlePlayerRotation();
    }

    // -------------------------------------------------------
    // ROTACIÓN DE LA CÁMARA CON EL MOUSE
    // -------------------------------------------------------
    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch  = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    // -------------------------------------------------------
    // POSICIÓN DE LA CÁMARA CON COLISIÓN
    // -------------------------------------------------------
    void HandlePosition()
    {
        // Dirección deseada de la cámara desde el target
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredDirection = rotation * Vector3.back; // atrás del target

        // Distancia real — reducida si hay geometría en el camino
        float actualDistance = GetCollisionDistance(desiredDirection);

        // Posición final de la cámara
        Vector3 desiredPosition = target.position + desiredDirection * actualDistance;

        // Movemos la cámara suavemente
        transform.position = Vector3.Lerp(transform.position, desiredPosition, rotationSmoothSpeed * Time.deltaTime);

        // La cámara siempre mira al target
        transform.LookAt(target.position);
    }

    // -------------------------------------------------------
    // ROTACIÓN DEL PERSONAJE HACIA DONDE SE MUEVE
    // -------------------------------------------------------
    void HandlePlayerRotation()
    {
        // El personaje rota para mirar hacia donde apunta la cámara horizontalmente
        // Solo cuando hay input de movimiento — lo maneja PlayerStateMachine
        // Acá solo exponemos la dirección forward de la cámara en el plano horizontal
        Vector3 cameraForwardFlat = new Vector3(
            Mathf.Sin(yaw * Mathf.Deg2Rad),
            0f,
            Mathf.Cos(yaw * Mathf.Deg2Rad)
        );

        // Rotamos el player body suavemente hacia esa dirección
        // Solo si la cámara se está moviendo horizontalmente
        if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForwardFlat);
            playerBody.rotation = Quaternion.Slerp(
                playerBody.rotation,
                targetRotation,
                playerRotationSpeed * Time.deltaTime
            );
        }
    }

    // -------------------------------------------------------
    // COLISIÓN DE CÁMARA
    // -------------------------------------------------------
    float GetCollisionDistance(Vector3 direction)
    {
        // Spherecast desde el target hacia la posición deseada de la cámara
        if (Physics.SphereCast(
            target.position,
            collisionRadius,
            direction,
            out RaycastHit hit,
            distance,
            collisionLayers))
        {
            // Si hay algo en el camino, la cámara se acerca al personaje
            return Mathf.Clamp(hit.distance, 0.5f, distance);
        }

        return distance;
    }

    // -------------------------------------------------------
    // PROPIEDAD PÚBLICA — otros scripts pueden leer el forward de la cámara
    // -------------------------------------------------------
    public Vector3 CameraForward
    {
        get
        {
            return new Vector3(
                Mathf.Sin(yaw * Mathf.Deg2Rad),
                0f,
                Mathf.Cos(yaw * Mathf.Deg2Rad)
            ).normalized;
        }
    }

    public Vector3 CameraRight
    {
        get
        {
            return new Vector3(
                Mathf.Cos(yaw * Mathf.Deg2Rad),
                0f,
                -Mathf.Sin(yaw * Mathf.Deg2Rad)
            ).normalized;
        }
    }
}