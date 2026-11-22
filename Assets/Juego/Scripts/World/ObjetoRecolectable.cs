using UnityEngine;

public enum TipoRecolectable
{
    Corazon,
    RupiaVerde,
    RupiaAzul,
    RupiaRoja,
    Pocion
}

// Botin fisico coleccionable con animacion de salto, atraccion magnetica y efectos al recoger
public class ObjetoRecolectable : MonoBehaviour
{
    private static Material materialBase;

    private TipoRecolectable tipo;
    private Vector3 velocidadSalto;
    private bool aterrizado;
    private float alturaBase = 0.42f;
    private float fase;
    private float tiempoVida;
    private VidaJugador jugador;
    private InventarioJugador inventario;
    private Color colorBrillo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimpiarEstaticos()
    {
        materialBase = null;
    }

    public static void SoltarBotin(Vector3 origen, bool esElite, float probabilidadCorazon = 0.45f)
    {
        int cantidadRupias = esElite ? 3 : Random.Range(1, 3);
        for (int i = 0; i < cantidadRupias; i++)
        {
            TipoRecolectable rupia = esElite
                ? (i == 0 ? TipoRecolectable.RupiaRoja : TipoRecolectable.RupiaAzul)
                : (Random.value < 0.28f ? TipoRecolectable.RupiaAzul : TipoRecolectable.RupiaVerde);
            Crear(rupia, origen + Vector3.up * 0.55f);
        }

        if (esElite || Random.value < probabilidadCorazon)
        {
            Crear(TipoRecolectable.Corazon, origen + Vector3.up * 0.65f);
        }

        if (esElite && Random.value < 0.65f)
        {
            Crear(TipoRecolectable.Pocion, origen + Vector3.up * 0.7f);
        }
    }

    public static ObjetoRecolectable Crear(TipoRecolectable tipoItem, Vector3 posicion)
    {
        GameObject go = new GameObject("Botin_" + tipoItem);
        go.transform.position = posicion;
        ObjetoRecolectable item = go.AddComponent<ObjetoRecolectable>();
        item.Inicializar(tipoItem);
        return item;
    }

    private void Inicializar(TipoRecolectable tipoItem)
    {
        tipo = tipoItem;
        fase = Random.Range(0f, Mathf.PI * 2f);
        float angulo = Random.Range(0f, Mathf.PI * 2f);
        float impulsoHoriz = Random.Range(1.2f, 2.5f);
        velocidadSalto = new Vector3(Mathf.Cos(angulo) * impulsoHoriz, Random.Range(3.2f, 4.6f), Mathf.Sin(angulo) * impulsoHoriz);

        ConstruirVisual();
    }

    private void ConstruirVisual()
    {
        if (materialBase == null)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            materialBase = new Material(shader);
        }

        switch (tipo)
        {
            case TipoRecolectable.Corazon:
                colorBrillo = new Color(0.96f, 0.16f, 0.24f);
                CrearForma(PrimitiveType.Sphere, new Vector3(-0.08f, 0.04f, 0f), new Vector3(0.20f, 0.20f, 0.16f), colorBrillo);
                CrearForma(PrimitiveType.Sphere, new Vector3(0.08f, 0.04f, 0f), new Vector3(0.20f, 0.20f, 0.16f), colorBrillo);
                CrearForma(PrimitiveType.Cube, new Vector3(0f, -0.04f, 0f), new Vector3(0.20f, 0.20f, 0.14f), colorBrillo, Quaternion.Euler(0f, 0f, 45f));
                break;

            case TipoRecolectable.Pocion:
                colorBrillo = new Color(0.95f, 0.22f, 0.45f);
                CrearForma(PrimitiveType.Sphere, new Vector3(0f, -0.02f, 0f), new Vector3(0.26f, 0.28f, 0.26f), colorBrillo);
                CrearForma(PrimitiveType.Cylinder, new Vector3(0f, 0.16f, 0f), new Vector3(0.11f, 0.09f, 0.11f), new Color(0.86f, 0.88f, 0.94f));
                CrearForma(PrimitiveType.Cylinder, new Vector3(0f, 0.24f, 0f), new Vector3(0.13f, 0.04f, 0.13f), new Color(0.65f, 0.44f, 0.22f));
                break;

            default:
                colorBrillo = tipo == TipoRecolectable.RupiaRoja
                    ? new Color(0.95f, 0.18f, 0.18f)
                    : (tipo == TipoRecolectable.RupiaAzul ? new Color(0.20f, 0.58f, 1f) : new Color(0.20f, 0.92f, 0.42f));
                CrearForma(PrimitiveType.Cube, Vector3.zero, new Vector3(0.18f, 0.30f, 0.12f), colorBrillo, Quaternion.Euler(0f, 45f, 45f));
                break;
        }

        Light luz = gameObject.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = colorBrillo;
        luz.intensity = 1.4f;
        luz.range = 2.6f;
        luz.shadows = LightShadows.None;
    }

    private void CrearForma(PrimitiveType primitiva, Vector3 offset, Vector3 escala, Color color, Quaternion? rotacion = null)
    {
        GameObject pieza = GameObject.CreatePrimitive(primitiva);
        Collider col = pieza.GetComponent<Collider>();
        if (col != null) Destroy(col);
        pieza.transform.SetParent(transform, false);
        pieza.transform.localPosition = offset;
        pieza.transform.localScale = escala;
        pieza.transform.localRotation = rotacion ?? Quaternion.identity;

        Renderer r = pieza.GetComponent<Renderer>();
        r.sharedMaterial = materialBase;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        bloque.SetColor("_Color", color);
        bloque.SetColor("_BaseColor", color);
        r.SetPropertyBlock(bloque);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.EnJuego) return;

        tiempoVida += Time.deltaTime;
        if (tiempoVida > 45f)
        {
            Destroy(gameObject);
            return;
        }

        if (!aterrizado)
        {
            velocidadSalto.y -= 13.5f * Time.deltaTime;
            Vector3 siguiente = transform.position + velocidadSalto * Time.deltaTime;
            if (siguiente.y <= alturaBase)
            {
                siguiente.y = alturaBase;
                aterrizado = true;
            }
            transform.position = siguiente;
        }
        else
        {
            Vector3 p = transform.position;
            p.y = alturaBase + Mathf.Sin(Time.time * 3.8f + fase) * 0.08f;
            transform.position = p;
        }

        transform.Rotate(0f, 115f * Time.deltaTime, 0f, Space.World);

        if (tiempoVida < 0.25f) return;

        if (jugador == null)
        {
            jugador = FindFirstObjectByType<VidaJugador>();
            if (jugador != null) inventario = jugador.GetComponent<InventarioJugador>();
        }
        if (jugador == null || jugador.Muerto) return;

        Vector3 objetivo = jugador.transform.position + Vector3.up * 0.85f;
        Vector3 delta = objetivo - transform.position;
        float dist = delta.magnitude;

        if (dist < 2.6f)
        {
            transform.position += delta.normalized * (Mathf.Lerp(9.5f, 3.8f, dist / 2.6f) * Time.deltaTime);
        }

        if (dist < 0.72f)
        {
            Recoger();
        }
    }

    private void Recoger()
    {
        switch (tipo)
        {
            case TipoRecolectable.Corazon:
                jugador.Curar(25, true);
                AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Curacion, 0.6f);
                break;

            case TipoRecolectable.Pocion:
                if (inventario != null && inventario.AgregarPocion(1))
                {
                    EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 0.5f, "+1 POCIÓN", colorBrillo, 1.0f);
                    AudioManager.Reproducir("Item_Pickup", null, 0.75f, 1.15f);
                }
                else
                {
                    jugador.Curar(35, true);
                    AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Curacion, 0.65f);
                }
                break;

            case TipoRecolectable.RupiaVerde:
                EntregarRupias(5);
                break;

            case TipoRecolectable.RupiaAzul:
                EntregarRupias(20);
                break;

            case TipoRecolectable.RupiaRoja:
                EntregarRupias(50);
                break;
        }

        EfectosVisuales.RecogerObjeto(transform.position, colorBrillo);
        Destroy(gameObject);
    }

    private void EntregarRupias(int valor)
    {
        inventario?.AgregarRupias(valor);
        EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 0.5f, "+" + valor + " RUPIAS", colorBrillo, 0.92f);
        AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Rupia, 0.55f);
    }
}
