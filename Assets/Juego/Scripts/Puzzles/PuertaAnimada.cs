using UnityEngine;

// Levanta la reja de la camara final cuando se juntan los cuatro sellos
public class PuertaAnimada : MonoBehaviour
{
    #region CLASS_VARIABLES

    [SerializeField] private float alturaApertura = 3.35f;
    [SerializeField] private float velocidad = 2.6f;

    private Vector3 posicionCerrada;
    private Collider colision;
    private bool abierta;
    private bool inicializada;

    public bool Abierta => abierta;

    #endregion

    #region UNITY_METHODS

    private void Awake()
    {
        posicionCerrada = transform.position;
        colision = GetComponent<Collider>();
        inicializada = true;
    }

    private void Update()
    {
        if (!inicializada)
            return;

        Vector3 objetivo = abierta ? posicionCerrada + Vector3.up * alturaApertura : posicionCerrada;
        if ((transform.position - objetivo).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);
        }
    }

    #endregion

    #region CLASS_METHODS

    public void EstablecerAbierta(bool abrir, bool instantaneo = false)
    {
        if (!inicializada)
        {
            Awake();
        }

        bool huboCambio = abierta != abrir;
        abierta = abrir;

        if (colision != null)
        {
            colision.enabled = !abierta;
        }

        if (instantaneo)
        {
            transform.position = abierta ? posicionCerrada + Vector3.up * alturaApertura : posicionCerrada;
        }
        else if (huboCambio && abierta)
        {
            CamaraTerceraPersona.Sacudir(0.12f, 0.45f);
        }
    }

    #endregion
}
