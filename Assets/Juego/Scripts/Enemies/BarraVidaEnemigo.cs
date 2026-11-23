using UnityEngine;

// Barra de vida 3D flotante sobre cada enemigo
public class BarraVidaEnemigo : MonoBehaviour
{
    #region CLASS_VARIABLES

    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    private static Material materialFondoCompartido;
    private static Material materialRellenoCompartido;

    private const float AnchoBase = 0.85f;

    private Transform contenedor;
    private Transform barraRelleno;
    private Renderer renderRelleno;
    private MaterialPropertyBlock bloqueColor;
    private Camera camaraPrincipal;
    private float alturaBarra = 2.0f;

    #endregion

    #region UNITY_METHODS

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimpiarEstaticos()
    {
        materialFondoCompartido = null;
        materialRellenoCompartido = null;
    }

    private void LateUpdate()
    {
        if (contenedor == null || !contenedor.gameObject.activeSelf)
            return;

        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }
        if (camaraPrincipal != null)
        {
            contenedor.rotation = camaraPrincipal.transform.rotation;
        }
    }

    #endregion

    #region CLASS_METHODS

    public void Inicializar(float alturaEnemigo)
    {
        alturaBarra = Mathf.Max(1.05f, alturaEnemigo + 0.32f);
        if (contenedor != null)
            return;

        AsegurarMateriales();
        bloqueColor = new MaterialPropertyBlock();

        GameObject raiz = new GameObject("BarraVida_3D");
        raiz.transform.SetParent(transform, false);
        raiz.transform.localPosition = Vector3.up * alturaBarra;
        contenedor = raiz.transform;

        GameObject fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(contenedor, false);
        fondo.transform.localScale = new Vector3(AnchoBase + 0.06f, 0.12f, 1f);
        fondo.GetComponent<Renderer>().sharedMaterial = materialFondoCompartido;

        GameObject relleno = GameObject.CreatePrimitive(PrimitiveType.Quad);
        relleno.name = "Relleno";
        Destroy(relleno.GetComponent<Collider>());
        relleno.transform.SetParent(contenedor, false);
        relleno.transform.localPosition = new Vector3(0f, 0f, -0.006f);
        relleno.transform.localScale = new Vector3(AnchoBase, 0.075f, 1f);

        barraRelleno = relleno.transform;
        renderRelleno = relleno.GetComponent<Renderer>();
        renderRelleno.sharedMaterial = materialRellenoCompartido;

        contenedor.gameObject.SetActive(false);
    }

    private static void AsegurarMateriales()
    {
        Shader unlit = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");

        if (materialFondoCompartido == null)
        {
            materialFondoCompartido = new Material(unlit) { color = new Color(0.06f, 0.07f, 0.08f, 0.9f) };
        }
        if (materialRellenoCompartido == null)
        {
            materialRellenoCompartido = new Material(unlit) { color = new Color(0.25f, 0.85f, 0.35f, 1f) };
        }
    }

    public void Actualizar(int vida, int vidaMax)
    {
        if (contenedor == null)
            return;

        if (vida <= 0)
        {
            contenedor.gameObject.SetActive(false);
            return;
        }

        float proporcion = Mathf.Clamp01((float)vida / Mathf.Max(1, vidaMax));
        contenedor.gameObject.SetActive(proporcion < 0.999f);

        float anchoActual = AnchoBase * proporcion;
        barraRelleno.localScale = new Vector3(Mathf.Max(0.001f, anchoActual), 0.075f, 1f);
        barraRelleno.localPosition = new Vector3(-(AnchoBase - anchoActual) * 0.5f, 0f, -0.006f);

        Color colorBarra = proporcion > 0.55f
            ? new Color(0.25f, 0.85f, 0.38f)
            : (proporcion > 0.25f ? new Color(0.95f, 0.72f, 0.18f) : new Color(0.92f, 0.20f, 0.16f));

        renderRelleno.GetPropertyBlock(bloqueColor);
        bloqueColor.SetColor(PropColor, colorBarra);
        bloqueColor.SetColor(PropBaseColor, colorBarra);
        renderRelleno.SetPropertyBlock(bloqueColor);
    }

    #endregion
}
