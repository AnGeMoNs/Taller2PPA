using UnityEngine;

// Camara en tercera persona con rotacion por raton, zoom, colision con muros y sacudida
public class CamaraTerceraPersona : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 4.5f;
    [SerializeField] private float minDistance = 3.0f;
    [SerializeField] private float maxDistance = 6.5f;
    [SerializeField] private float sensitivity = 2.5f;
    [SerializeField] private float height = 1.55f;

    public bool PermitirControl { get; set; } = true;

    private static CamaraTerceraPersona activa;
    private float yaw;
    private float pitch = 18f;
    private float tiempoSacudida;
    private float intensidadSacudida;
    private readonly RaycastHit[] impactosCamara = new RaycastHit[32];

    public static void Sacudir(float intensidad, float duracion)
    {
        if (activa == null) return;
        activa.intensidadSacudida = Mathf.Max(activa.intensidadSacudida, intensidad);
        activa.tiempoSacudida = Mathf.Max(activa.tiempoSacudida, duracion);
    }

    private void Awake()
    {
        activa = this;
    }

    private void Start()
    {
        if (FindFirstObjectByType<GameManager>() == null)
        {
            BloquearCursor(true);
        }
    }

    private void Update()
    {
        if (!PermitirControl)
        {
            tiempoSacudida = 0f;
            intensidadSacudida = 0f;
            return;
        }

        if (GameManager.Instance == null)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) BloquearCursor(false);
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) BloquearCursor(true);
        }

        if (Cursor.lockState != CursorLockMode.Locked) return;

        yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * sensitivity, -10f, 65f);

        float rueda = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(rueda) > 0.001f)
        {
            distance = Mathf.Clamp(distance - rueda * 2.5f, minDistance, maxDistance);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pivote = target.position + Vector3.up * height;
        Quaternion rotacion = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direccionAtras = rotacion * Vector3.back;
        float distanciaDisponible = distance;

        // Evita que la camara atraviese paredes o pilares de la mazmorra
        int cantidad = Physics.SphereCastNonAlloc(pivote, 0.15f, direccionAtras, impactosCamara, distance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
        {
            RaycastHit hit = impactosCamara[i];
            if (hit.collider.transform.IsChildOf(target)) continue;
            distanciaDisponible = Mathf.Min(distanciaDisponible, Mathf.Max(0.2f, hit.distance - 0.1f));
        }

        Vector3 desplazamientoSacudida = Vector3.zero;
        if (tiempoSacudida > 0f && Time.timeScale > 0f)
        {
            tiempoSacudida = Mathf.Max(0f, tiempoSacudida - Time.deltaTime);
            desplazamientoSacudida = Random.insideUnitSphere * intensidadSacudida;
            if (tiempoSacudida <= 0f) intensidadSacudida = 0f;
        }

        transform.SetPositionAndRotation(pivote + direccionAtras * distanciaDisponible + desplazamientoSacudida, rotacion);
    }

    private void BloquearCursor(bool bloqueado)
    {
        Cursor.lockState = bloqueado ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloqueado;
    }

    private void OnDisable()
    {
        BloquearCursor(false);
    }

    private void OnDestroy()
    {
        if (activa == this) activa = null;
    }
}
