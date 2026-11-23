using UnityEngine;

// Controla el hundimiento y color de cada placa de presion del puzzle de cajas
public class PlacaPresion : MonoBehaviour
{
    #region CLASS_VARIABLES

    [Header("Referencias")]
    [SerializeField] private Transform plataforma;
    [SerializeField] private Renderer[] indicadores;
    [SerializeField] private Light luz;
    [SerializeField] private float hundimiento = 0.055f;

    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int PropEmission = Shader.PropertyToID("_EmissionColor");

    private bool activa;
    private float alturaBase;
    private float alturaActual;
    private MaterialPropertyBlock bloque;
    private bool? ultimoEstado;

    #endregion

    #region UNITY_METHODS

    private void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (plataforma != null)
        {
            alturaBase = plataforma.localPosition.y;
            alturaActual = alturaBase;
        }
        AplicarColor(false);
    }

    private void Update()
    {
        if (plataforma == null)
            return;

        float objetivo = activa ? alturaBase - hundimiento : alturaBase;
        if (Mathf.Abs(alturaActual - objetivo) > 0.0005f)
        {
            alturaActual = Mathf.Lerp(alturaActual, objetivo, 1f - Mathf.Exp(-14f * Time.deltaTime));
            Vector3 pos = plataforma.localPosition;
            pos.y = alturaActual;
            plataforma.localPosition = pos;
        }
    }

    #endregion

    #region CLASS_METHODS

    public void ConfigurarVisual(Transform pad, Renderer[] renders, Light luzIndicadora)
    {
        plataforma = pad;
        indicadores = renders;
        luz = luzIndicadora;
    }

    public void EstablecerActiva(bool estado)
    {
        activa = estado;
        if (ultimoEstado != estado)
        {
            AplicarColor(estado);
        }
    }

    private void AplicarColor(bool estado)
    {
        ultimoEstado = estado;
        Color color = estado ? new Color(0.18f, 0.90f, 0.42f) : new Color(0.68f, 0.38f, 0.16f);

        if (indicadores != null)
        {
            if (bloque == null) bloque = new MaterialPropertyBlock();
            for (int i = 0; i < indicadores.Length; i++)
            {
                Renderer r = indicadores[i];
                if (r == null)
                    continue;

                r.GetPropertyBlock(bloque);
                bloque.SetColor(PropColor, color);
                bloque.SetColor(PropBaseColor, color);
                bloque.SetColor(PropEmission, color * (estado ? 1.3f : 0.25f));
                r.SetPropertyBlock(bloque);
            }
        }

        if (luz != null)
        {
            luz.color = color;
            luz.intensity = estado ? 2.2f : 0.7f;
        }
    }

    #endregion
}
