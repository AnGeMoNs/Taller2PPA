using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minimapa interactivo de la mazmorra (tecla M) que muestra salas, estado, enemigos, cofres y jugador
public class MinimapaMazmorra : MonoBehaviour
{
    private const string PrefMostrarMapa = "Mazmorra.MostrarMinimapa";

    private Transform jugador;
    private ProgresoPartida progreso;
    private InventarioJugador inventario;
    private MazmorraPuzzles puzzles;
    private MejorasMazmorra mejoras;

    private GameObject raizPanel;
    private RectTransform contenedorMapa;
    private RectTransform flechaJugador;
    private Image imagenPuerta;
    private readonly Dictionary<string, Image> celdasSalas = new Dictionary<string, Image>();
    private readonly List<RectTransform> puntosEnemigos = new List<RectTransform>();
    private readonly List<RectTransform> puntosCofres = new List<RectTransform>();
    private RectTransform puntoSantuario;
    private float siguienteRefrescoEntidades;

    private static readonly Color ColorNoVisitada = new Color(0.14f, 0.16f, 0.18f, 0.72f);
    private static readonly Color ColorVisitada = new Color(0.25f, 0.30f, 0.34f, 0.92f);
    private static readonly Color ColorCompletada = new Color(0.20f, 0.56f, 0.34f, 0.95f);
    private static readonly Color ColorActual = new Color(0.86f, 0.68f, 0.28f, 0.98f);
    private static readonly Color ColorAltar = new Color(0.45f, 0.34f, 0.62f, 0.92f);

    // Mapeo de mundo (x in [-18,18], z in [-18,30]) a espacio local del minimapa (164 x 164)
    private const float EscalaMapa = 3.1f;
    private const float CentroMundoZ = 6f;

    public bool Visible { get; private set; } = true;

    public static bool PreferenciaVisible
    {
        get => PlayerPrefs.GetInt(PrefMostrarMapa, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(PrefMostrarMapa, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public void Construir(
        Transform padreCanvas,
        TMP_FontAsset fuente,
        Transform player,
        ProgresoPartida prog,
        InventarioJugador inv,
        MazmorraPuzzles puz,
        MejorasMazmorra ext)
    {
        jugador = player;
        progreso = prog;
        inventario = inv;
        puzzles = puz;
        mejoras = ext;
        Visible = PreferenciaVisible;

        GameObject panel = new GameObject("Minimapa_Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(padreCanvas, false);
        raizPanel = panel;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(-224f, -318f);
        rt.offsetMax = new Vector2(-30f, -120f);

        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.06f, 0.08f, 0.09f, 0.88f);
        fondo.raycastTarget = false;

        // Borde dorado superior
        CrearCajaUI(rt, "Borde", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), Vector2.zero, new Color(0.84f, 0.68f, 0.36f));

        // Titulo del minimapa
        GameObject txtObj = new GameObject("TituloMapa", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(rt, false);
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = new Vector2(0f, 1f);
        rtTxt.anchorMax = new Vector2(1f, 1f);
        rtTxt.offsetMin = new Vector2(8f, -24f);
        rtTxt.offsetMax = new Vector2(-8f, -4f);
        TMP_Text titulo = txtObj.GetComponent<TextMeshProUGUI>();
        titulo.font = fuente;
        titulo.text = "MAPA  ·  [M]";
        titulo.fontSize = 12;
        titulo.color = new Color(0.84f, 0.68f, 0.36f);
        titulo.alignment = TextAlignmentOptions.Center;
        titulo.raycastTarget = false;

        // Area interna centrada para dibujar salas y marcadores
        GameObject area = new GameObject("AreaMapa", typeof(RectTransform));
        area.transform.SetParent(rt, false);
        contenedorMapa = area.GetComponent<RectTransform>();
        contenedorMapa.anchorMin = new Vector2(0.5f, 0.45f);
        contenedorMapa.anchorMax = new Vector2(0.5f, 0.45f);
        contenedorMapa.sizeDelta = new Vector2(170f, 165f);

        CrearSalaMapa("Entrada", new Vector2(0f, 0f));
        CrearSalaMapa("Combate", new Vector2(-12f, 0f));
        CrearSalaMapa("Placas", new Vector2(12f, 0f));
        CrearSalaMapa("Secuencia", new Vector2(0f, -12f));
        CrearSalaMapa("Trampas", new Vector2(0f, 12f));
        CrearSalaMapa("Altar", new Vector2(0f, 24f));

        // Indicador de reja norte en z = 18
        RectTransform rtPuerta = CrearMarcador("PuertaNorte", new Vector2(14f, 4f), new Color(0.92f, 0.24f, 0.20f));
        rtPuerta.anchoredPosition = MundoAMapa(new Vector3(0f, 0f, 18f));
        imagenPuerta = rtPuerta.GetComponent<Image>();

        // Indicador del santuario
        puntoSantuario = CrearMarcador("Marcador_Santuario", new Vector2(6f, 6f), new Color(0.35f, 0.95f, 1f));

        // Flecha/marcador del jugador
        flechaJugador = CrearMarcador("Marcador_Jugador", new Vector2(8f, 11f), new Color(1f, 0.96f, 0.72f));

        raizPanel.SetActive(Visible);
    }

    private void CrearSalaMapa(string id, Vector2 centroMundo)
    {
        float tam = 12f * EscalaMapa - 3f;
        RectTransform rt = CrearMarcador("Sala_" + id, new Vector2(tam, tam), ColorNoVisitada);
        rt.anchoredPosition = MundoAMapa(new Vector3(centroMundo.x, 0f, centroMundo.y));
        celdasSalas[id] = rt.GetComponent<Image>();
    }

    private RectTransform CrearMarcador(string nombre, Vector2 tamano, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(contenedorMapa, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamano;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private static void CrearCajaUI(Transform padre, string nombre, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private static Vector2 MundoAMapa(Vector3 posMundo)
    {
        return new Vector2(posMundo.x * EscalaMapa, (posMundo.z - CentroMundoZ) * EscalaMapa);
    }

    public void EstablecerVisible(bool activo)
    {
        Visible = activo;
        PreferenciaVisible = activo;
        if (raizPanel != null) raizPanel.SetActive(Visible);
    }

    private void Update()
    {
        if (raizPanel == null) return;

        if (GameManager.Instance != null && GameManager.Instance.EnJuego && Input.GetKeyDown(KeyCode.M))
        {
            EstablecerVisible(!Visible);
            AudioManager.Reproducir("UI_Click", null, 0.45f, 1.15f);
        }

        if (!Visible || jugador == null) return;

        ActualizarSalas();
        ActualizarJugador();

        if (Time.unscaledTime >= siguienteRefrescoEntidades)
        {
            siguienteRefrescoEntidades = Time.unscaledTime + 0.18f;
            ActualizarEntidades();
        }
    }

    private void ActualizarSalas()
    {
        string salaActual = puzzles != null ? puzzles.IdSalaActual() : "Entrada";

        foreach (KeyValuePair<string, Image> kv in celdasSalas)
        {
            string id = kv.Key;
            Image img = kv.Value;
            if (img == null) continue;

            if (id == salaActual)
            {
                img.color = ColorActual;
            }
            else if (progreso != null && progreso.SalaResuelta(id))
            {
                img.color = ColorCompletada;
            }
            else if (id == "Altar")
            {
                bool abierta = puzzles != null && puzzles.EntradaFinalAbierta;
                img.color = abierta ? ColorAltar : ColorNoVisitada;
            }
            else if (inventario != null && inventario.SalaDescubierta(id))
            {
                img.color = ColorVisitada;
            }
            else
            {
                img.color = ColorNoVisitada;
            }
        }

        if (imagenPuerta != null && puzzles != null)
        {
            imagenPuerta.color = puzzles.EntradaFinalAbierta ? new Color(0.24f, 0.90f, 0.45f) : new Color(0.92f, 0.24f, 0.20f);
        }
    }

    private void ActualizarJugador()
    {
        if (flechaJugador == null || jugador == null) return;
        flechaJugador.anchoredPosition = MundoAMapa(jugador.position);
        flechaJugador.localRotation = Quaternion.Euler(0f, 0f, -jugador.eulerAngles.y);
        flechaJugador.SetAsLastSibling();
    }

    private void ActualizarEntidades()
    {
        // Santuario
        if (puntoSantuario != null && mejoras != null && mejoras.Santuario != null)
        {
            puntoSantuario.gameObject.SetActive(true);
            puntoSantuario.anchoredPosition = MundoAMapa(mejoras.Santuario.transform.position);
        }
        else if (puntoSantuario != null)
        {
            puntoSantuario.gameObject.SetActive(false);
        }

        // Cofres aun cerrados
        int idxCofre = 0;
        if (mejoras != null)
        {
            IReadOnlyList<CofreTesoro> cofres = mejoras.Cofres;
            for (int i = 0; i < cofres.Count; i++)
            {
                CofreTesoro c = cofres[i];
                if (c == null || !c.Disponible) continue;
                RectTransform rt = ObtenerPunto(puntosCofres, idxCofre++, "PuntoCofre", new Vector2(5f, 5f), new Color(1f, 0.84f, 0.24f));
                rt.anchoredPosition = MundoAMapa(c.transform.position);
                rt.gameObject.SetActive(true);
            }
        }
        for (int i = idxCofre; i < puntosCofres.Count; i++) puntosCofres[i].gameObject.SetActive(false);

        // Enemigos vivos
        Enemy[] enemigos = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int idxEnemigo = 0;
        for (int i = 0; i < enemigos.Length; i++)
        {
            Enemy e = enemigos[i];
            if (e == null || e.Muerto || !e.gameObject.activeInHierarchy) continue;
            RectTransform rt = ObtenerPunto(puntosEnemigos, idxEnemigo++, "PuntoEnemigo",
                e.EsElite ? new Vector2(8f, 8f) : new Vector2(5.5f, 5.5f),
                e.EsElite ? new Color(1f, 0.25f, 0.85f) : new Color(0.95f, 0.22f, 0.20f));
            rt.anchoredPosition = MundoAMapa(e.transform.position);
            rt.gameObject.SetActive(true);
        }
        for (int i = idxEnemigo; i < puntosEnemigos.Count; i++) puntosEnemigos[i].gameObject.SetActive(false);
    }

    private RectTransform ObtenerPunto(List<RectTransform> lista, int indice, string prefijo, Vector2 tam, Color color)
    {
        while (lista.Count <= indice)
        {
            lista.Add(CrearMarcador(prefijo + "_" + lista.Count, tam, color));
        }
        RectTransform rt = lista[indice];
        rt.sizeDelta = tam;
        Image img = rt.GetComponent<Image>();
        if (img != null) img.color = color;
        return rt;
    }
}
