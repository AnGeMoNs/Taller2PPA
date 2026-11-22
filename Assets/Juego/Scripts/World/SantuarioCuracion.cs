using UnityEngine;

// Fuente sagrada en el vestibulo: cura gratis (se recarga al superar salas) o intercambia rupias por pociones
public class SantuarioCuracion : MonoBehaviour, IInteractuable
{
    public const int CostoPocionRupias = 25;

    private bool curacionGratisDisponible = true;
    private Transform hadaOrbita;
    private Light luzFuente;
    private VidaJugador jugadorCache;
    private InventarioJugador inventarioCache;

    public Transform Transform => transform;
    public bool Disponible => gameObject.activeInHierarchy;

    public string TextoInteraccion
    {
        get
        {
            if (jugadorCache == null) jugadorCache = FindFirstObjectByType<VidaJugador>();
            if (inventarioCache == null && jugadorCache != null) inventarioCache = jugadorCache.GetComponent<InventarioJugador>();

            bool necesitaVida = jugadorCache != null && jugadorCache.Vida < jugadorCache.VidaMaxima;
            if (curacionGratisDisponible && necesitaVida)
            {
                return "Santuario · Restaurar toda la vida (Gratis)";
            }
            if (inventarioCache != null && inventarioCache.Pociones < InventarioJugador.MaxPociones)
            {
                return $"Santuario · Comprar Poción ({CostoPocionRupias} Rupias)";
            }
            return "Santuario · Bendición de energía";
        }
    }

    public static SantuarioCuracion Crear(Vector3 posicion, Transform padre)
    {
        GameObject raiz = new GameObject("Santuario_Curacion");
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = posicion;

        Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
        Material matPiedra = new Material(shader) { color = new Color(0.28f, 0.31f, 0.34f) };
        Material matAgua = new Material(shader) { color = new Color(0.22f, 0.86f, 0.96f) };
        Material matHada = new Material(Shader.Find("Unlit/Color") ?? shader) { color = new Color(0.65f, 1f, 0.96f) };

        CrearPieza(raiz.transform, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.22f, 0f), new Vector3(1.55f, 0.22f, 1.55f), matPiedra, true);
        CrearPieza(raiz.transform, PrimitiveType.Cylinder, "Agua", new Vector3(0f, 0.44f, 0f), new Vector3(1.22f, 0.04f, 1.22f), matAgua, false);
        CrearPieza(raiz.transform, PrimitiveType.Cylinder, "Columna", new Vector3(0f, 0.72f, 0f), new Vector3(0.32f, 0.42f, 0.32f), matPiedra, false);

        GameObject hada = CrearPieza(raiz.transform, PrimitiveType.Sphere, "Hada", new Vector3(0f, 1.35f, 0f), Vector3.one * 0.20f, matHada, false);

        GameObject nodoLuz = new GameObject("Luz_Santuario");
        nodoLuz.transform.SetParent(raiz.transform, false);
        nodoLuz.transform.localPosition = Vector3.up * 1.45f;
        Light luz = nodoLuz.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.38f, 0.94f, 1f);
        luz.intensity = 2.1f;
        luz.range = 5.4f;
        luz.shadows = LightShadows.None;

        // Rotulo flotante
        GameObject rotulo = new GameObject("Rotulo_Santuario");
        rotulo.transform.SetParent(raiz.transform, false);
        rotulo.transform.localPosition = Vector3.up * 2.05f;
        TextMesh tm = rotulo.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.text = "SANTUARIO DE HADAS";
        tm.fontSize = 44;
        tm.characterSize = 0.032f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.58f, 0.96f, 1f);
        rotulo.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        rotulo.AddComponent<RotuloOrientado>();

        SantuarioCuracion santuario = raiz.AddComponent<SantuarioCuracion>();
        santuario.hadaOrbita = hada.transform;
        santuario.luzFuente = luz;
        return santuario;
    }

    private static GameObject CrearPieza(Transform padre, PrimitiveType tipo, string nombre, Vector3 pos, Vector3 escala, Material mat, bool conCollider)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        if (!conCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    public void RecargarBendicion()
    {
        curacionGratisDisponible = true;
    }

    private void Update()
    {
        if (hadaOrbita != null)
        {
            float a = Time.time * 2.1f;
            hadaOrbita.localPosition = new Vector3(Mathf.Cos(a) * 0.34f, 1.32f + Mathf.Sin(Time.time * 3.4f) * 0.14f, Mathf.Sin(a) * 0.34f);
        }
        if (luzFuente != null)
        {
            luzFuente.intensity = (curacionGratisDisponible ? 2.1f : 1.35f) + Mathf.Sin(Time.time * 2.8f) * 0.25f;
        }
    }

    public void Interactuar(VidaJugador jugador, InventarioJugador inventario, HUDManager hud)
    {
        if (jugador == null) return;

        if (curacionGratisDisponible && jugador.Vida < jugador.VidaMaxima)
        {
            curacionGratisDisponible = false;
            jugador.Curar(jugador.VidaMaxima, true);
            AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Curacion, 0.85f);
            hud?.Aviso("El Santuario ha restaurado toda tu salud.");
            return;
        }

        if (inventario != null && inventario.Pociones < InventarioJugador.MaxPociones)
        {
            if (inventario.GastarRupias(CostoPocionRupias))
            {
                inventario.AgregarPocion(1);
                EfectosVisuales.RecogerObjeto(transform.position + Vector3.up * 1.1f, new Color(0.96f, 0.28f, 0.52f));
                EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 1.45f, "+1 POCIÓN", new Color(0.96f, 0.35f, 0.58f), 1.0f);
                AudioManager.Reproducir("Item_Pickup", null, 0.78f, 1.15f);
                hud?.Aviso("Poción adquirida. Presiona [Q] para beberla.");
                GameManager.Instance?.Guardar(false);
            }
            else
            {
                AudioManager.Reproducir("UI_Click", null, 0.5f, 0.75f);
                hud?.Aviso($"Necesitas {CostoPocionRupias} Rupias para una poción (tienes {inventario.Rupias}).");
            }
            return;
        }

        Movimiento3D mov = jugador.GetComponent<Movimiento3D>();
        mov?.RestaurarEstaminaCompleta();
        EfectosVisuales.Curacion(jugador.transform.position);
        AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Curacion, 0.55f);
        hud?.Aviso("Energía restaurada al máximo.");
    }
}
