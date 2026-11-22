using UnityEngine;

// Camara en tercera persona con rotacion por raton, suavizado de muros, Z-Targeting, FOV dinamico y sacudida
public class CamaraTerceraPersona : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 4.5f;
    [SerializeField] private float minDistance = 3.0f;
    [SerializeField] private float maxDistance = 6.5f;
    [SerializeField] private float sensitivity = 2.5f;
    [SerializeField] private float height = 1.55f;

    private const string PrefSensibilidad = "Mazmorra.Sensibilidad";

    public bool PermitirControl { get; set; } = true;
    public static float Sensibilidad => activa != null ? activa.sensitivity : PlayerPrefs.GetFloat(PrefSensibilidad, 2.5f);

    private static CamaraTerceraPersona activa;
    private Camera camaraComponente;
    private Movimiento3D movimientoTarget;
    private SistemaFijadoObjetivo fijadoTarget;

    private float yaw;
    private float pitch = 18f;
    private float distanciaActual;
    private float fovBase = 60f;
    private float tiempoSacudida;
    private float duracionSacudida = 0.1f;
    private float intensidadSacudida;
    private readonly RaycastHit[] impactosCamara = new RaycastHit[32];

    public static void Sacudir(float intensidad, float duracion)
    {
        if (activa == null) return;
        activa.intensidadSacudida = Mathf.Max(activa.intensidadSacudida, intensidad);
        activa.tiempoSacudida = Mathf.Max(activa.tiempoSacudida, duracion);
        activa.duracionSacudida = Mathf.Max(activa.duracionSacudida, duracion);
    }

    public static void ConfigurarSensibilidad(float valor)
    {
        float clamped = Mathf.Clamp(valor, 0.8f, 6.0f);
        PlayerPrefs.SetFloat(PrefSensibilidad, clamped);
        PlayerPrefs.Save();
        if (activa != null) activa.sensitivity = clamped;
    }

    private void Awake()
    {
        activa = this;
        camaraComponente = GetComponent<Camera>();
        if (camaraComponente != null) fovBase = camaraComponente.fieldOfView;
        sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(PrefSensibilidad, sensitivity), 0.8f, 6.0f);
        distanciaActual = distance;
    }

    private void Start()
    {
        if (target != null)
        {
            movimientoTarget = target.GetComponent<Movimiento3D>();
            fijadoTarget = target.GetComponent<SistemaFijadoObjetivo>();
        }

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

        if (target != null && fijadoTarget == null)
        {
            fijadoTarget = target.GetComponent<SistemaFijadoObjetivo>();
        }

        // Encuadre suave hacia el enemigo cuando Z-Targeting esta activo
        if (fijadoTarget != null && fijadoTarget.TieneObjetivo && target != null)
        {
            Vector3 haciaEnemigo = fijadoTarget.ObjetivoActual.transform.position - target.position;
            haciaEnemigo.y = 0f;
            if (haciaEnemigo.sqrMagnitude > 0.04f)
            {
                float yawObjetivo = Quaternion.LookRotation(haciaEnemigo).eulerAngles.y;
                yaw = Mathf.LerpAngle(yaw, yawObjetivo, 1f - Mathf.Exp(-7.5f * Time.deltaTime));
                pitch = Mathf.Lerp(pitch, 19f, 1f - Mathf.Exp(-5f * Time.deltaTime));
            }
            yaw += Input.GetAxisRaw("Mouse X") * sensitivity * 0.35f;
        }
        else
        {
            yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * sensitivity, -10f, 65f);
        }

        float rueda = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(rueda) > 0.001f)
        {
            distance = Mathf.Clamp(distance - rueda * 2.5f, minDistance, maxDistance);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (movimientoTarget == null) movimientoTarget = target.GetComponent<Movimiento3D>();

        Vector3 pivote = target.position + Vector3.up * height;
        Quaternion rotacion = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direccionAtras = rotacion * Vector3.back;
        float distanciaDeseada = distance;

        // Evita que la camara atraviese paredes o pilares de la mazmorra
        int cantidad = Physics.SphereCastNonAlloc(pivote, 0.16f, direccionAtras, impactosCamara, distance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
        {
            RaycastHit hit = impactosCamara[i];
            if (hit.collider.transform.IsChildOf(target)) continue;
            if (hit.collider.GetComponentInParent<Enemy>() != null) continue;
            distanciaDeseada = Mathf.Min(distanciaDeseada, Mathf.Max(0.35f, hit.distance - 0.12f));
        }

        // Acerca rapido al chocar con un muro y aleja suavemente al despejarse
        float velInterp = distanciaDeseada < distanciaActual ? 22f : 8f;
        distanciaActual = Mathf.Lerp(distanciaActual, distanciaDeseada, 1f - Mathf.Exp(-velInterp * Time.unscaledDeltaTime));

        // FOV dinamico sutil al correr o esquivar
        if (camaraComponente != null && movimientoTarget != null)
        {
            float extraFov = movimientoTarget.EstaEsquivando ? 6.5f : (movimientoTarget.EstaCorriendo ? 4.0f : 0f);
            camaraComponente.fieldOfView = Mathf.Lerp(camaraComponente.fieldOfView, fovBase + extraFov, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        }

        Vector3 desplazamientoSacudida = Vector3.zero;
        if (tiempoSacudida > 0f && Time.timeScale > 0f)
        {
            tiempoSacudida = Mathf.Max(0f, tiempoSacudida - Time.unscaledDeltaTime);
            float factorDecaimiento = duracionSacudida > 0.001f ? Mathf.Clamp01(tiempoSacudida / duracionSacudida) : 0f;
            desplazamientoSacudida = Random.insideUnitSphere * (intensidadSacudida * factorDecaimiento);
            if (tiempoSacudida <= 0f)
            {
                intensidadSacudida = 0f;
                duracionSacudida = 0.1f;
            }
        }

        transform.SetPositionAndRotation(pivote + direccionAtras * distanciaActual + desplazamientoSacudida, rotacion);
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
