using UnityEngine;

// Rotacion y flotacion suave para llaves, gemas y la reliquia
public class EfectoReliquia : MonoBehaviour
{
    #region CLASS_VARIABLES

    [SerializeField] private float velocidadGiro = 55f;
    [SerializeField] private float amplitudFlote = 0.12f;
    [SerializeField] private float frecuenciaFlote = 2.2f;

    private Vector3 posicionBase;
    private float faseInicial;

    #endregion

    #region UNITY_METHODS

    private void Awake()
    {
        posicionBase = transform.localPosition;
        faseInicial = (transform.position.x + transform.position.z) * 0.5f;
    }

    private void Update()
    {
        transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
        float desplazamientoY = Mathf.Sin(Time.time * frecuenciaFlote + faseInicial) * amplitudFlote;
        transform.localPosition = new Vector3(posicionBase.x, posicionBase.y + desplazamientoY, posicionBase.z);
    }

    #endregion
}
