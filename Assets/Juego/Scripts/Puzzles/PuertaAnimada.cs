using UnityEngine;

// Eleva suavemente la reja de la camara final cuando se obtienen todos los sellos
public class PuertaAnimada : MonoBehaviour
{
    [SerializeField] private float alturaApertura = 3.35f;
    [SerializeField] private float velocidad = 2.6f;

    private Vector3 posicionCerrada;
    private Collider colision;
    private bool abierta;
    private bool inicializada;

    public bool Abierta => abierta;

    private void Awake()
    {
        posicionCerrada = transform.position;
        colision = GetComponent<Collider>();
        inicializada = true;
    }

    public void EstablecerAbierta(bool abrir, bool instantaneo = false)
    {
        if (!inicializada) Awake();
        bool huboCambio = abierta != abrir;
        abierta = abrir;

        if (colision != null) colision.enabled = !abierta;

        if (instantaneo)
        {
            transform.position = abierta ? posicionCerrada + Vector3.up * alturaApertura : posicionCerrada;
        }
        else if (huboCambio && abierta)
        {
            CamaraTerceraPersona.Sacudir(0.12f, 0.45f);
        }
    }

    private void Update()
    {
        if (!inicializada) return;
        Vector3 objetivo = abierta ? posicionCerrada + Vector3.up * alturaApertura : posicionCerrada;
        if ((transform.position - objetivo).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);
        }
    }
}
