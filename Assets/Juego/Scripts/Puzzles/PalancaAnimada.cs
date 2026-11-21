using UnityEngine;

// Anima el brazo giratorio, el cristal y la luz de cada palanca del puzzle de secuencia
public class PalancaAnimada : MonoBehaviour
{
    [SerializeField] private Transform brazo;
    [SerializeField] private Renderer cristal;
    [SerializeField] private Light luz;
    [SerializeField] private float anguloReposo = -32f;
    [SerializeField] private float anguloActivada = 38f;

    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int PropEmission = Shader.PropertyToID("_EmissionColor");

    private bool activada;
    private float anguloActual;
    private float flashErrorHasta;
    private MaterialPropertyBlock bloque;
    private Color ultimoColor = Color.clear;

    public void ConfigurarVisual(Transform pivoteBrazo, Renderer renderCristal, Light luzCristal)
    {
        brazo = pivoteBrazo;
        cristal = renderCristal;
        luz = luzCristal;
    }

    private void Awake()
    {
        bloque = new MaterialPropertyBlock();
        anguloActual = anguloReposo;
        AplicarVisual(true);
    }

    public void EstablecerEstado(bool activa)
    {
        activada = activa;
        AplicarVisual(false);
    }

    public void DestelloError()
    {
        activada = false;
        flashErrorHasta = Time.time + 0.55f;
        AplicarVisual(true);
    }

    private void Update()
    {
        float objetivo = activada ? anguloActivada : anguloReposo;
        if (brazo != null && Mathf.Abs(anguloActual - objetivo) > 0.05f)
        {
            anguloActual = Mathf.Lerp(anguloActual, objetivo, 1f - Mathf.Exp(-12f * Time.deltaTime));
            brazo.localRotation = Quaternion.Euler(anguloActual, 0f, 0f);
        }

        AplicarVisual(false);
    }

    private void AplicarVisual(bool forzar)
    {
        bool enError = Time.time < flashErrorHasta;
        Color color = enError
            ? new Color(0.95f, 0.16f, 0.12f)
            : (activada ? new Color(0.18f, 0.90f, 0.42f) : new Color(0.75f, 0.52f, 0.18f));

        if (!forzar && color == ultimoColor) return;
        ultimoColor = color;

        if (cristal != null)
        {
            if (bloque == null) bloque = new MaterialPropertyBlock();
            cristal.GetPropertyBlock(bloque);
            bloque.SetColor(PropColor, color);
            bloque.SetColor(PropBaseColor, color);
            bloque.SetColor(PropEmission, color * (activada || enError ? 1.4f : 0.35f));
            cristal.SetPropertyBlock(bloque);
        }

        if (luz != null)
        {
            luz.color = color;
            luz.intensity = enError ? 2.6f : (activada ? 2.1f : 0.85f);
        }
    }
}
