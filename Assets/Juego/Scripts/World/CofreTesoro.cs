using UnityEngine;

// Cofre del tesoro interactuable con animacion de apertura, luz dorada y recompensas persistentes
public class CofreTesoro : MonoBehaviour, IInteractuable
{
    private static Material materialMadera;
    private static Material materialOro;

    [SerializeField] private string idCofre;
    [SerializeField] private int recompensaRupias = 25;
    [SerializeField] private int recompensaPociones = 1;

    private Transform tapa;
    private Light luzTesoro;
    private bool abierto;
    private float anguloTapa;

    public string Id => idCofre;
    public Transform Transform => transform;
    public bool Disponible => !abierto && gameObject.activeInHierarchy;
    public string TextoInteraccion => "Abrir cofre del tesoro";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimpiarEstaticos()
    {
        materialMadera = null;
        materialOro = null;
    }

    public void ConfigurarExistente(string id, int rupias, int pociones)
    {
        idCofre = id;
        recompensaRupias = rupias;
        recompensaPociones = pociones;
        AgregarLuz();
    }

    public static CofreTesoro CrearProcedural(string id, Vector3 posicion, Quaternion rotacion, Transform padre, int rupias, int pociones)
    {
        AsegurarMateriales();

        GameObject raiz = new GameObject(id);
        raiz.transform.SetParent(padre, false);
        raiz.transform.SetPositionAndRotation(posicion, rotacion);

        BoxCollider col = raiz.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.36f, 0f);
        col.size = new Vector3(0.95f, 0.72f, 0.62f);

        // Base de madera y ribetes dorados
        CrearParte(raiz.transform, "Base", new Vector3(0f, 0.24f, 0f), new Vector3(0.88f, 0.48f, 0.56f), materialMadera);
        CrearParte(raiz.transform, "Banda_Izq", new Vector3(-0.34f, 0.24f, 0f), new Vector3(0.08f, 0.50f, 0.59f), materialOro);
        CrearParte(raiz.transform, "Banda_Der", new Vector3(0.34f, 0.24f, 0f), new Vector3(0.08f, 0.50f, 0.59f), materialOro);
        CrearParte(raiz.transform, "Cerradura", new Vector3(0f, 0.38f, 0.29f), new Vector3(0.14f, 0.16f, 0.05f), materialOro);

        // Pivote de tapa abatible
        GameObject pivote = new GameObject("Pivote_Tapa");
        pivote.transform.SetParent(raiz.transform, false);
        pivote.transform.localPosition = new Vector3(0f, 0.48f, -0.28f);

        CrearParte(pivote.transform, "Tapa", new Vector3(0f, 0.11f, 0.28f), new Vector3(0.90f, 0.22f, 0.58f), materialMadera);
        CrearParte(pivote.transform, "BandaTapa_Izq", new Vector3(-0.34f, 0.11f, 0.28f), new Vector3(0.09f, 0.24f, 0.60f), materialOro);
        CrearParte(pivote.transform, "BandaTapa_Der", new Vector3(0.34f, 0.11f, 0.28f), new Vector3(0.09f, 0.24f, 0.60f), materialOro);

        CofreTesoro cofre = raiz.AddComponent<CofreTesoro>();
        cofre.idCofre = id;
        cofre.recompensaRupias = rupias;
        cofre.recompensaPociones = pociones;
        cofre.tapa = pivote.transform;
        cofre.AgregarLuz();
        return cofre;
    }

    private static void AsegurarMateriales()
    {
        Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
        if (materialMadera == null)
        {
            materialMadera = new Material(shader) { color = new Color(0.38f, 0.21f, 0.10f) };
        }
        if (materialOro == null)
        {
            materialOro = new Material(shader) { color = new Color(0.92f, 0.72f, 0.22f) };
            if (materialOro.HasProperty("_Metallic")) materialOro.SetFloat("_Metallic", 0.78f);
            if (materialOro.HasProperty("_Glossiness")) materialOro.SetFloat("_Glossiness", 0.65f);
        }
    }

    private static void CrearParte(Transform padre, string nombre, Vector3 localPos, Vector3 escala, Material mat)
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        Collider c = cubo.GetComponent<Collider>();
        if (c != null) Destroy(c);
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = localPos;
        cubo.transform.localScale = escala;
        cubo.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private void AgregarLuz()
    {
        if (luzTesoro != null) return;
        GameObject nodoLuz = new GameObject("Luz_Cofre");
        nodoLuz.transform.SetParent(transform, false);
        nodoLuz.transform.localPosition = Vector3.up * 0.85f;
        luzTesoro = nodoLuz.AddComponent<Light>();
        luzTesoro.type = LightType.Point;
        luzTesoro.color = new Color(1f, 0.84f, 0.28f);
        luzTesoro.intensity = 1.55f;
        luzTesoro.range = 3.8f;
        luzTesoro.shadows = LightShadows.None;
    }

    public void EstablecerEstadoCargado(bool estaAbierto)
    {
        abierto = estaAbierto;
        if (abierto)
        {
            anguloTapa = -88f;
            if (tapa != null) tapa.localRotation = Quaternion.Euler(anguloTapa, 0f, 0f);
            if (luzTesoro != null) luzTesoro.enabled = false;
        }
    }

    private void Update()
    {
        if (abierto && tapa != null && anguloTapa > -88f)
        {
            anguloTapa = Mathf.MoveTowards(anguloTapa, -88f, 210f * Time.deltaTime);
            tapa.localRotation = Quaternion.Euler(anguloTapa, 0f, 0f);
        }

        if (!abierto && luzTesoro != null)
        {
            luzTesoro.intensity = 1.35f + Mathf.Sin(Time.time * 3.2f) * 0.35f;
        }
    }

    public void Interactuar(VidaJugador jugador, InventarioJugador inventario, HUDManager hud)
    {
        if (abierto) return;
        abierto = true;
        if (luzTesoro != null) luzTesoro.enabled = false;

        inventario?.MarcarCofreAbierto(idCofre);

        string resumen = "";
        if (recompensaRupias > 0)
        {
            inventario?.AgregarRupias(recompensaRupias);
            resumen = "+" + recompensaRupias + " Rupias";
        }

        if (recompensaPociones > 0)
        {
            bool anadida = inventario != null && inventario.AgregarPocion(recompensaPociones);
            if (anadida)
            {
                resumen += (resumen.Length > 0 ? " y " : "") + "+" + recompensaPociones + " Poción";
            }
            else if (jugador != null)
            {
                jugador.Curar(45, true);
                resumen += (resumen.Length > 0 ? " y " : "") + "Curación +45";
            }
        }

        EfectosVisuales.RecogerObjeto(transform.position + Vector3.up * 0.7f, new Color(1f, 0.86f, 0.25f));
        EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 1.2f, resumen, new Color(1f, 0.88f, 0.32f), 1.05f);
        AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Cofre, 0.8f);
        CamaraTerceraPersona.Sacudir(0.05f, 0.12f);
        hud?.Aviso("Cofre abierto: " + resumen);
        GameManager.Instance?.Guardar(false);
    }
}
