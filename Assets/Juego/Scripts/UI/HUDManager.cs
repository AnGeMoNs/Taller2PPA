using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Construye y actualiza menu principal, ajustes, HUD completo, minimapa, Z-target, combo y pantallas de estado
public class HUDManager : MonoBehaviour
{
    private const int GuardadosPorPagina = 6;

    private GameManager game;
    private VidaJugador vida;
    private Movimiento3D movimiento;
    private CombateJugador combate;
    private InventarioJugador inventario;
    private SistemaFijadoObjetivo fijado;
    private ProgresoPartida progreso;
    private MazmorraPuzzles puzzles;
    private MejorasMazmorra mejoras;
    private MinimapaMazmorra minimapa;

    private GameObject panelMenu;
    private GameObject panelHud;
    private GameObject panelEstado;
    private GameObject subpanelActual;
    private GameObject panelOpcionesPausa;
    private RectTransform raizCanvas;

    private TMP_Text textoVida;
    private TMP_Text textoSellos;
    private TMP_Text textoRupias;
    private TMP_Text textoPociones;
    private TMP_Text mensajeHud;
    private TMP_Text mensajeMenu;
    private TMP_Text textoObjetivo;
    private TMP_Text textoInteraccion;
    private TMP_Text textoGuardado;
    private TMP_Text textoSala;
    private TMP_Text textoPoder;
    private TMP_Text textoBannerSala;
    private TMP_Text textoCombo;
    private TMP_Text textoBonusCombo;
    private TMP_Text textoEnemigoNombre;
    private TMP_Text textoEnemigoEstado;

    private Image barraVida;
    private Image barraEstamina;
    private Image barraComboTiempo;
    private Image barraEnemigoVida;
    private Image flashDano;
    private CanvasGroup grupoBannerSala;
    private GameObject panelCombo;
    private GameObject panelEnemigoObjetivo;
    private InputField campoNombreActivo;

    private Coroutine corrutinaGuardado;
    private Coroutine corrutinaAviso;
    private Coroutine corrutinaFlash;
    private Coroutine corrutinaBannerSala;
    private TMP_FontAsset fuenteTmp;
    private Font fuenteLegacy;
    private bool reliquiaPotenciada;

    private readonly Color colorOscuro = new Color(0.035f, 0.041f, 0.042f, 0.96f);
    private readonly Color colorMarfil = new Color(0.90f, 0.89f, 0.84f);
    private readonly Color colorRojo = new Color(0.78f, 0.18f, 0.16f);
    private readonly Color colorVerde = new Color(0.32f, 0.78f, 0.48f);
    private readonly Color colorCian = new Color(0.30f, 0.82f, 0.96f);
    private readonly Color colorMetal = new Color(0.10f, 0.13f, 0.14f, 0.94f);
    private readonly Color colorDorado = new Color(0.86f, 0.70f, 0.36f);

    public void Construir(
        GameManager gm,
        VidaJugador vidaJugador,
        Movimiento3D mov,
        CombateJugador comb,
        InventarioJugador inv,
        SistemaFijadoObjetivo sistemaFijado,
        ProgresoPartida prog,
        MazmorraPuzzles puz,
        MejorasMazmorra ext)
    {
        game = gm;
        vida = vidaJugador;
        movimiento = mov;
        combate = comb;
        inventario = inv;
        fijado = sistemaFijado;
        progreso = prog;
        puzzles = puz;
        mejoras = ext;

        fuenteLegacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        try { fuenteTmp = TMP_Settings.defaultFontAsset; } catch { fuenteTmp = null; }
        if (fuenteTmp == null)
        {
            fuenteTmp = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (fuenteTmp == null)
        {
            Debug.LogError("Falta la fuente TMP. Importa TMP Essential Resources fuera de Play Mode.");
        }

        GameObject canvasObj = new GameObject("Canvas_Juego", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        raizCanvas = canvasObj.GetComponent<RectTransform>();

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler escalador = canvasObj.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1280, 720);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        EventSystem sistemaEventos = FindFirstObjectByType<EventSystem>();
        if (sistemaEventos == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        else if (sistemaEventos.GetComponent<BaseInputModule>() == null)
        {
            sistemaEventos.gameObject.AddComponent<StandaloneInputModule>();
        }

        CrearMenuPrincipal();
        CrearHudJuego();
        panelHud.SetActive(false);

        vida.VidaCambiada += ActualizarVida;
        vida.DanoRecibido += MostrarFlashDano;
        progreso.Cambio += ActualizarProgreso;
        if (movimiento != null) movimiento.EstaminaCambiada += ActualizarEstamina;
        if (combate != null) combate.ComboCambiado += ActualizarCombo;
        if (inventario != null) inventario.InventarioCambiado += ActualizarInventario;

        ActualizarVida(vida.Vida, vida.VidaMaxima);
        if (movimiento != null) ActualizarEstamina(movimiento.Estamina, movimiento.EstaminaMaxima);
        ActualizarProgreso();
        ActualizarInventario();
    }

    private void Update()
    {
        if (campoNombreActivo != null && campoNombreActivo.gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Return))
        {
            game.Nueva(campoNombreActivo.text);
        }

        if (game != null && game.EnJuego)
        {
            ActualizarIndicadoresDinamicos();
        }
    }

    private void ActualizarIndicadoresDinamicos()
    {
        // 1. Barra de tiempo del combo
        if (combate != null && panelCombo != null && combate.RachaCombo >= 2)
        {
            if (barraComboTiempo != null)
            {
                barraComboTiempo.rectTransform.anchorMax = new Vector2(combate.ProgresoTiempoCombo, 1f);
                barraComboTiempo.rectTransform.offsetMax = Vector2.zero;
            }
        }

        // 2. Indicador de estado especial de espada (contraataque parry / carga giro / reliquia)
        if (textoPoder != null)
        {
            if (combate != null && combate.ContraataqueListo)
            {
                textoPoder.text = "⚡ ¡CONTRAATAQUE LISTO! (DAÑO x1.75)";
                textoPoder.color = new Color(1f, 0.92f, 0.28f);
            }
            else if (movimiento != null && movimiento.CargaGiroProgreso > 0.2f)
            {
                int pct = Mathf.RoundToInt(movimiento.CargaGiroProgreso * 100f);
                textoPoder.text = $"◎ CARGANDO ATAQUE GIRATORIO... {pct}%";
                textoPoder.color = colorCian;
            }
            else if (reliquiaPotenciada)
            {
                textoPoder.text = "★ RELIQUIA ACTIVA · DAÑO x2";
                textoPoder.color = colorDorado;
            }
            else
            {
                textoPoder.text = "";
            }
        }

        // 3. Barra superior de objetivo fijado (Z-Targeting) o progreso de Oleada Final
        if (panelEnemigoObjetivo == null) return;

        Enemy objetivo = fijado != null ? fijado.ObjetivoActual : null;
        if (objetivo != null)
        {
            panelEnemigoObjetivo.SetActive(true);
            float prop = Mathf.Clamp01((float)objetivo.VidaActual / Mathf.Max(1, objetivo.VidaMaxima));
            textoEnemigoNombre.text = objetivo.EsElite ? $"★ {objetivo.NombreVisible.ToUpperInvariant()}" : objetivo.NombreVisible.ToUpperInvariant();
            textoEnemigoEstado.text = string.IsNullOrEmpty(objetivo.EstadoVisual)
                ? $"{objetivo.VidaActual} / {objetivo.VidaMaxima}"
                : $"{objetivo.EstadoVisual}  ·  {objetivo.VidaActual}/{objetivo.VidaMaxima}";
            barraEnemigoVida.color = objetivo.EstaAturdido ? colorDorado : colorRojo;
            barraEnemigoVida.rectTransform.anchorMax = new Vector2(prop, 1f);
            barraEnemigoVida.rectTransform.offsetMax = Vector2.zero;
        }
        else if (puzzles != null && puzzles.Recuperado && !puzzles.FinalCompletado && puzzles.Oleada != null && puzzles.Oleada.Activada)
        {
            panelEnemigoObjetivo.SetActive(true);
            OleadaFinal oleada = puzzles.Oleada;
            float prop = Mathf.Clamp01((float)oleada.VidaTotalActual / Mathf.Max(1, oleada.VidaTotalMaxima));
            textoEnemigoNombre.text = "OLEADA FINAL · GUARDIANES DE LA RELIQUIA";
            textoEnemigoEstado.text = $"Enemigos restantes: {oleada.Restantes} / {oleada.TotalOleada}";
            barraEnemigoVida.color = new Color(0.86f, 0.24f, 0.72f);
            barraEnemigoVida.rectTransform.anchorMax = new Vector2(prop, 1f);
            barraEnemigoVida.rectTransform.offsetMax = Vector2.zero;
        }
        else
        {
            panelEnemigoObjetivo.SetActive(false);
        }
    }

    private RectTransform CrearRect(Transform padre, string nombre, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private RectTransform CrearPanel(Transform padre, string nombre, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        RectTransform rect = CrearRect(padre, nombre, anchorMin, anchorMax, offsetMin, offsetMax);
        Image img = rect.gameObject.AddComponent<Image>();
        img.color = color;
        return rect;
    }

    private RectTransform CrearPanelCompleto(Transform padre, string nombre, Color color)
    {
        return CrearPanel(padre, nombre, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);
    }

    private RectTransform CrearCaja(Transform padre, string nombre, float x, float y, float ancho, float alto, Color color)
    {
        return CrearPanel(padre, nombre, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - alto), new Vector2(x + ancho, -y), color);
    }

    private TMP_Text CrearEtiqueta(Transform padre, string texto, float tamano, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alineacion = TextAlignmentOptions.Left)
    {
        RectTransform rect = CrearRect(padre, "Texto", anchorMin, anchorMax, offsetMin, offsetMax);
        TMP_Text componente = rect.gameObject.AddComponent<TextMeshProUGUI>();
        componente.font = fuenteTmp;
        componente.text = texto;
        componente.fontSize = tamano;
        componente.color = color;
        componente.alignment = alineacion;
        componente.raycastTarget = false;
        return componente;
    }

    private TMP_Text CrearTexto(Transform padre, string texto, float tamano, Color color, float x, float y, float ancho, float alto)
    {
        return CrearEtiqueta(padre, texto, tamano, color, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - alto), new Vector2(x + ancho, -y));
    }

    private Button CrearBoton(Transform padre, string titulo, float x, float y, float ancho, float alto, Action accionClick, float tamFuente = 21f, Color? colorFondo = null)
    {
        RectTransform caja = CrearCaja(padre, titulo, x, y, ancho, alto, colorFondo ?? colorMetal);
        Button boton = caja.gameObject.AddComponent<Button>();

        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1f, 0.84f, 0.56f);
        colores.pressedColor = new Color(0.7f, 0.52f, 0.28f);
        boton.colors = colores;

        boton.targetGraphic = caja.GetComponent<Image>();
        CrearEtiqueta(caja, titulo, tamFuente, colorMarfil, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-8, 0));
        boton.onClick.AddListener(() =>
        {
            AudioManager.Reproducir("UI_Click");
            accionClick();
        });
        return boton;
    }

    private void CrearMenuPrincipal()
    {
        RectTransform raiz = CrearPanelCompleto(raizCanvas, "MenuPrincipal", colorOscuro);
        panelMenu = raiz.gameObject;

        RectTransform panelIzquierdo = CrearPanel(raiz, "Panel_izquierdo", new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(32, 26), new Vector2(-20, -26), colorMetal);
        CrearCaja(panelIzquierdo, "Linea", 30, 38, 5, 108, colorDorado);

        TMP_Text titulo = CrearTexto(panelIzquierdo, "LA MAZMORRA DE LINK", 38, colorMarfil, 52, 42, 540, 62);
        titulo.fontStyle = FontStyles.Bold;
        CrearTexto(panelIzquierdo, "L A   R E L I Q U I A   S A G R A D A", 18, colorDorado, 54, 108, 500, 34);
        CrearTexto(panelIzquierdo, "EDICIÓN MEJORADA · COMBATE, EXPLORACIÓN Y PUZZLES 3D", 14, new Color(0.60f, 0.72f, 0.68f), 54, 148, 520, 26);

        CrearBoton(panelIzquierdo, "NUEVA PARTIDA", 54, 210, 410, 48, MostrarNuevaPartida);
        CrearBoton(panelIzquierdo, "CONTINUAR PARTIDA", 54, 268, 410, 48, () => MostrarPartidasGuardadas(0));
        CrearBoton(panelIzquierdo, "CONTROLES", 54, 326, 410, 48, MostrarControles);
        CrearBoton(panelIzquierdo, "OPCIONES Y AUDIO", 54, 384, 410, 48, () => MostrarOpciones(AbrirSubmenu("OPCIONES")));
        CrearBoton(panelIzquierdo, "CRÉDITOS", 54, 442, 410, 48, MostrarCreditos);
        CrearBoton(panelIzquierdo, "SALIR", 54, 500, 410, 48, game.Salir);

        mensajeMenu = CrearTexto(panelIzquierdo, "", 17, colorDorado, 54, 560, 500, 56);
        CrearEtiqueta(raiz, "REÚNE LOS CUATRO SELLOS, DOMINA EL PARRY Y RECLAMA LA RELIQUIA.", 16, colorDorado, new Vector2(0.52f, 0.08f), new Vector2(0.97f, 0.17f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
    }

    private RectTransform AbrirSubmenu(string titulo)
    {
        campoNombreActivo = null;
        if (subpanelActual != null)
        {
            subpanelActual.SetActive(false);
            Destroy(subpanelActual);
        }

        RectTransform panel = CrearPanel(panelMenu.transform, "Submenu", new Vector2(0.51f, 0.09f), new Vector2(0.98f, 0.91f), Vector2.zero, Vector2.zero, colorMetal);
        subpanelActual = panel.gameObject;

        CrearCaja(panel, "Linea", 24, 29, 4, 55, colorDorado);
        CrearTexto(panel, titulo, 30, colorMarfil, 42, 29, 500, 60);
        CrearBoton(panel, "VOLVER", 30, 505, 220, 42, () =>
        {
            campoNombreActivo = null;
            subpanelActual.SetActive(false);
        });
        return panel;
    }

    private void MostrarNuevaPartida()
    {
        RectTransform panel = AbrirSubmenu("NUEVA PARTIDA");
        CrearTexto(panel, "Nombre del jugador", 20, colorMarfil, 30, 115, 450, 42);

        RectTransform cajaCampo = CrearCaja(panel, "CampoNombre", 30, 163, 450, 50, colorOscuro);
        InputField entradaTexto = cajaCampo.gameObject.AddComponent<InputField>();
        entradaTexto.characterLimit = 32;
        entradaTexto.targetGraphic = cajaCampo.GetComponent<Image>();

        RectTransform rectTexto = CrearRect(cajaCampo, "InputText", Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));
        Text textoInput = rectTexto.gameObject.AddComponent<Text>();
        textoInput.font = fuenteLegacy;
        textoInput.fontSize = 22;
        textoInput.color = colorMarfil;
        textoInput.alignment = TextAnchor.MiddleLeft;

        entradaTexto.textComponent = textoInput;
        entradaTexto.lineType = InputField.LineType.SingleLine;
        entradaTexto.ActivateInputField();
        campoNombreActivo = entradaTexto;

        CrearBoton(panel, "CREAR", 30, 233, 220, 50, () => game.Nueva(entradaTexto.text));
        CrearTexto(panel, "Cada perfil guarda su progreso, rupias, pociones, cofres y estadísticas en un archivo JSON independiente.", 17, colorDorado, 30, 302, 510, 80);
    }

    private void MostrarPartidasGuardadas(int pagina)
    {
        RectTransform panel = AbrirSubmenu("ARCHIVOS DE GUARDADO");
        List<DatosPartida> guardados;

        try
        {
            guardados = SaveManager.Listar();
        }
        catch (Exception ex)
        {
            Aviso(ex.Message);
            return;
        }

        if (guardados.Count == 0)
        {
            CrearTexto(panel, "No hay partidas guardadas.", 22, colorMarfil, 30, 115, 500, 50);
            return;
        }

        pagina = Mathf.Clamp(pagina, 0, (guardados.Count - 1) / GuardadosPorPagina);
        int inicio = pagina * GuardadosPorPagina;
        int fin = Mathf.Min(guardados.Count, inicio + GuardadosPorPagina);

        for (int i = inicio; i < fin; i++)
        {
            DatosPartida datos = guardados[i];
            int fila = i - inicio;
            int sellos = datos.objetos != null ? datos.objetos.Count : 0;
            string tiempo = FormatearTiempo(datos.tiempoJugado);
            float yFila = 98 + fila * 54;

            CrearBoton(
                panel,
                $"{datos.nombre}  ·  HP {datos.vida}  ·  Sellos {sellos}/4  ·  ◆{datos.rupias}  ·  {tiempo}",
                30, yFila, 466, 44,
                () => game.Cargar(datos.nombre),
                17f);

            string nombreBorrar = datos.nombre;
            CrearBoton(
                panel,
                "X",
                504, yFila, 44, 44,
                () =>
                {
                    if (game.EliminarPerfil(nombreBorrar))
                    {
                        MostrarPartidasGuardadas(pagina);
                    }
                },
                18f,
                new Color(0.45f, 0.12f, 0.12f, 0.95f));
        }

        if (pagina > 0)
        {
            CrearBoton(panel, "ANTERIOR", 30, 442, 220, 42, () => MostrarPartidasGuardadas(pagina - 1));
        }
        if (fin < guardados.Count)
        {
            CrearBoton(panel, "SIGUIENTE", 280, 442, 220, 42, () => MostrarPartidasGuardadas(pagina + 1));
        }
    }

    private void MostrarControles()
    {
        RectTransform panel = AbrirSubmenu("CONTROLES");
        CrearTexto(
            panel,
            "WASD              Movimiento\nSHIFT             Correr (usa estamina)\nESPACIO           Esquiva rápida (invulnerable)\nRATÓN             Cámara (Rueda: Zoom)\nCLIC IZQ.         Combo de espada (x3)\nR / MANT. IZQ.    Ataque giratorio 360°\nCLIC DER.         Escudo (-75%) · Justo a tiempo: ¡PARRY!\nTAB / V           Fijar objetivo (Z-Target) / Cambiar\nQ                 Beber poción de curación\nE                 Interactuar (puzzles, cofres, santuario)\nM                 Mostrar / ocultar minimapa\nF5 / ESC          Guardar partida / Pausa",
            17,
            colorMarfil,
            30,
            96,
            530,
            400);
    }

    private void MostrarOpciones(RectTransform panel)
    {
        TMP_Text lblMusica = CrearTexto(panel, "", 20, colorMarfil, 30, 110, 320, 36);
        TMP_Text lblSfx = CrearTexto(panel, "", 20, colorMarfil, 30, 175, 320, 36);
        TMP_Text lblSens = CrearTexto(panel, "", 20, colorMarfil, 30, 240, 320, 36);
        TMP_Text lblMapa = CrearTexto(panel, "", 20, colorMarfil, 30, 305, 320, 36);

        void Refrescar()
        {
            lblMusica.text = $"Volumen Música:  {Mathf.RoundToInt(AudioManager.VolumenMusica * 100f)}%";
            lblSfx.text = $"Volumen Efectos:  {Mathf.RoundToInt(AudioManager.VolumenEfectos * 100f)}%";
            lblSens.text = $"Sensibilidad Ratón:  {CamaraTerceraPersona.Sensibilidad:0.0}";
            bool mapaActivo = minimapa != null ? minimapa.Visible : MinimapaMazmorra.PreferenciaVisible;
            lblMapa.text = $"Minimapa en HUD:  {(mapaActivo ? "ACTIVADO" : "OCULTO")}";
        }

        CrearBoton(panel, "-", 360, 106, 56, 40, () => { AudioManager.ConfigurarVolumenMusica(AudioManager.VolumenMusica - 0.1f); Refrescar(); }, 22f);
        CrearBoton(panel, "+", 426, 106, 56, 40, () => { AudioManager.ConfigurarVolumenMusica(AudioManager.VolumenMusica + 0.1f); Refrescar(); }, 22f);

        CrearBoton(panel, "-", 360, 171, 56, 40, () => { AudioManager.ConfigurarVolumenEfectos(AudioManager.VolumenEfectos - 0.1f); Refrescar(); }, 22f);
        CrearBoton(panel, "+", 426, 171, 56, 40, () => { AudioManager.ConfigurarVolumenEfectos(AudioManager.VolumenEfectos + 0.1f); Refrescar(); }, 22f);

        CrearBoton(panel, "-", 360, 236, 56, 40, () => { CamaraTerceraPersona.ConfigurarSensibilidad(CamaraTerceraPersona.Sensibilidad - 0.3f); Refrescar(); }, 22f);
        CrearBoton(panel, "+", 426, 236, 56, 40, () => { CamaraTerceraPersona.ConfigurarSensibilidad(CamaraTerceraPersona.Sensibilidad + 0.3f); Refrescar(); }, 22f);

        CrearBoton(panel, "CAMBIAR", 360, 301, 132, 40, () =>
        {
            bool nuevo = !(minimapa != null ? minimapa.Visible : MinimapaMazmorra.PreferenciaVisible);
            if (minimapa != null) minimapa.EstablecerVisible(nuevo);
            else MinimapaMazmorra.PreferenciaVisible = nuevo;
            Refrescar();
        }, 17f);

        CrearTexto(panel, "Consejo: Haz clic derecho justo antes de un golpe enemigo para ejecutar un PARRY PERFECTO y aturdirlo.", 16, colorDorado, 30, 378, 490, 75);
        Refrescar();
    }

    private void MostrarCreditos()
    {
        RectTransform panel = AbrirSubmenu("CRÉDITOS");
        CrearTexto(
            panel,
            "La Mazmorra de Link: La Reliquia Sagrada\nProyecto de Programación Avanzada\nUniversidad Católica del Norte\n\nPersonaje y animaciones: Link (Mixamo Humanoid)\nEscenario y enemigos: paquete del taller\nSistemas: Combate con Parry, Z-Targeting, Minimapa,\nCofres, Santuario, Botín y Guardado JSON por perfil.",
            19,
            colorMarfil,
            30,
            105,
            505,
            350);
    }

    private void CrearHudJuego()
    {
        RectTransform raiz = CrearRect(raizCanvas, "HUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panelHud = raiz.gameObject;

        // Destello de pantalla al recibir dano o bloquear
        RectTransform rectFlash = CrearPanelCompleto(raiz, "FlashDano", new Color(0.8f, 0.1f, 0.1f, 0f));
        flashDano = rectFlash.GetComponent<Image>();
        flashDano.raycastTarget = false;

        // Panel de vida y estamina abajo a la izquierda
        RectTransform panelVida = CrearPanel(raiz, "EstadoVital", new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 24), new Vector2(394, 168), colorMetal);
        CrearCaja(panelVida, "BordeVida", 0, 0, 4, 144, colorDorado);
        CrearTexto(panelVida, "ESTADO VITAL", 14, colorDorado, 18, 9, 300, 20);
        textoVida = CrearTexto(panelVida, "", 22, colorMarfil, 18, 30, 325, 30);

        RectTransform fondoBarra = CrearCaja(panelVida, "BarraFondo", 18, 66, 326, 12, colorOscuro);
        barraVida = CrearPanelCompleto(fondoBarra, "BarraVida", colorVerde).GetComponent<Image>();

        CrearTexto(panelVida, "ESTAMINA", 11, colorCian, 18, 84, 120, 16);
        RectTransform fondoEst = CrearCaja(panelVida, "EstaminaFondo", 18, 102, 326, 9, colorOscuro);
        barraEstamina = CrearPanelCompleto(fondoEst, "BarraEstamina", colorCian).GetComponent<Image>();

        textoPoder = CrearTexto(panelVida, "", 13, colorDorado, 18, 117, 336, 20);

        // Panel compacto de pociones y rupias junto al estado vital
        RectTransform panelBolsa = CrearPanel(raiz, "BolsaRapida", new Vector2(0, 0), new Vector2(0, 0), new Vector2(404, 24), new Vector2(582, 106), colorMetal);
        CrearCaja(panelBolsa, "BordeBolsa", 0, 0, 4, 82, colorDorado);
        textoPociones = CrearTexto(panelBolsa, "[Q] POCIÓN  x1", 17, new Color(1f, 0.45f, 0.62f), 14, 14, 160, 26);
        textoRupias = CrearTexto(panelBolsa, "◆ 0 RUPIAS", 17, colorVerde, 14, 46, 160, 26);

        // Indicador de sala arriba a la izquierda
        RectTransform panelSala = CrearPanel(raiz, "ZonaActual", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -76), new Vector2(326, -24), colorMetal);
        CrearCaja(panelSala, "BordeSala", 0, 0, 4, 52, colorDorado);
        textoSala = CrearTexto(panelSala, "VESTÍBULO CENTRAL", 16, colorDorado, 16, 15, 278, 26);

        // Contador de sellos arriba a la derecha
        RectTransform panelObjetos = CrearPanel(raiz, "Objetos", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-346, -110), new Vector2(-30, -24), colorMetal);
        CrearCaja(panelObjetos, "BordeObj", 0, 0, 4, 86, colorDorado);
        CrearTexto(panelObjetos, "OBJETIVO · LA RELIQUIA SAGRADA", 14, colorDorado, 16, 10, 294, 22);
        textoSellos = CrearTexto(panelObjetos, "", 21, colorMarfil, 16, 39, 286, 34);

        // Panel de enemigo fijado (Z-Targeting) o barra de oleada final arriba al centro
        RectTransform panelObj = CrearPanel(raiz, "PanelEnemigo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-215, -138), new Vector2(215, -80), colorMetal);
        panelEnemigoObjetivo = panelObj.gameObject;
        CrearCaja(panelObj, "BordeObjEn", 0, 0, 430, 3, colorDorado);
        textoEnemigoNombre = CrearEtiqueta(panelObj, "ENEMIGO", 15, colorMarfil, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -24), new Vector2(-12, -4), TextAlignmentOptions.Left);
        textoEnemigoEstado = CrearEtiqueta(panelObj, "", 13, colorDorado, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -24), new Vector2(-12, -4), TextAlignmentOptions.Right);
        RectTransform fondoEnVida = CrearCaja(panelObj, "BarraEnFondo", 12, 32, 406, 14, colorOscuro);
        barraEnemigoVida = CrearPanelCompleto(fondoEnVida, "BarraEnVida", colorRojo).GetComponent<Image>();
        panelEnemigoObjetivo.SetActive(false);

        // Panel de racha de combo al costado derecho
        RectTransform pCombo = CrearPanel(raiz, "PanelCombo", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-224, -56), new Vector2(-30, 14), colorMetal);
        panelCombo = pCombo.gameObject;
        CrearCaja(pCombo, "BordeCombo", 0, 0, 4, 70, colorDorado);
        textoCombo = CrearTexto(pCombo, "COMBO x2", 22, colorDorado, 14, 8, 174, 28);
        textoCombo.fontStyle = FontStyles.Bold;
        textoBonusCombo = CrearTexto(pCombo, "+10% DAÑO", 13, colorMarfil, 14, 38, 174, 18);
        RectTransform fondoCombo = CrearCaja(pCombo, "ComboTiempoFondo", 14, 58, 166, 6, colorOscuro);
        barraComboTiempo = CrearPanelCompleto(fondoCombo, "ComboTiempo", colorDorado).GetComponent<Image>();
        panelCombo.SetActive(false);

        // Banner cinematico al entrar en una sala
        RectTransform pBanner = CrearPanel(raiz, "BannerSala", new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(-240, -28), new Vector2(240, 28), new Color(0.05f, 0.06f, 0.07f, 0.80f));
        grupoBannerSala = pBanner.gameObject.AddComponent<CanvasGroup>();
        grupoBannerSala.alpha = 0f;
        grupoBannerSala.blocksRaycasts = false;
        textoBannerSala = CrearEtiqueta(pBanner, "", 24, colorDorado, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        textoBannerSala.fontStyle = FontStyles.Bold;

        // Minimapa interactivo debajo del contador de sellos
        minimapa = gameObject.AddComponent<MinimapaMazmorra>();
        minimapa.Construir(raiz, fuenteTmp, vida != null ? vida.transform : null, progreso, inventario, puzzles, mejoras);

        mensajeHud = CrearEtiqueta(raiz, "", 19, colorDorado, new Vector2(0.26f, 0.06f), new Vector2(0.74f, 0.14f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        textoGuardado = CrearEtiqueta(raiz, "", 14, colorMarfil, new Vector2(0.74f, 0.09f), new Vector2(0.97f, 0.14f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
        textoObjetivo = CrearEtiqueta(raiz, "", 17, colorMarfil, new Vector2(0.24f, 0.90f), new Vector2(0.76f, 0.98f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        textoInteraccion = CrearEtiqueta(raiz, "", 23, colorDorado, new Vector2(0.24f, 0.18f), new Vector2(0.76f, 0.25f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        CrearEtiqueta(raiz, "Clic Izq. Espada · R Giro · Espacio Esquivar · Clic Der. Escudo/Parry · Tab Fijar · Q Poción · M Mapa", 13, colorMarfil, new Vector2(0.45f, 0.015f), new Vector2(0.97f, 0.055f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
    }

    public void MostrarSala(string sala, bool potenciado)
    {
        reliquiaPotenciada = potenciado;
        if (textoSala != null) textoSala.text = sala;
    }

    public void MostrarBannerSala(string nombreSala)
    {
        if (grupoBannerSala == null || textoBannerSala == null) return;
        textoBannerSala.text = nombreSala;
        if (corrutinaBannerSala != null) StopCoroutine(corrutinaBannerSala);
        corrutinaBannerSala = StartCoroutine(AnimarBannerSala());
    }

    private IEnumerator AnimarBannerSala()
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            grupoBannerSala.alpha = Mathf.Clamp01(t / 0.35f);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(1.6f);
        t = 0f;
        while (t < 0.55f)
        {
            t += Time.unscaledDeltaTime;
            grupoBannerSala.alpha = 1f - Mathf.Clamp01(t / 0.55f);
            yield return null;
        }
        grupoBannerSala.alpha = 0f;
    }

    private void MostrarFlashDano(bool bloqueado)
    {
        if (flashDano == null) return;
        if (corrutinaFlash != null) StopCoroutine(corrutinaFlash);
        Color colorInicial = bloqueado ? new Color(0.45f, 0.84f, 1f, 0.24f) : new Color(0.88f, 0.12f, 0.10f, 0.36f);
        corrutinaFlash = StartCoroutine(AnimarFlash(colorInicial));
    }

    private IEnumerator AnimarFlash(Color colorInicial)
    {
        float tiempo = 0f;
        const float duracion = 0.28f;
        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            float alfa = Mathf.Lerp(colorInicial.a, 0f, tiempo / duracion);
            if (flashDano != null) flashDano.color = new Color(colorInicial.r, colorInicial.g, colorInicial.b, alfa);
            yield return null;
        }
        if (flashDano != null) flashDano.color = new Color(colorInicial.r, colorInicial.g, colorInicial.b, 0f);
    }

    public void MostrarGuardado()
    {
        if (textoGuardado == null) return;
        textoGuardado.text = "Guardado automático";
        if (corrutinaGuardado != null) StopCoroutine(corrutinaGuardado);
        corrutinaGuardado = StartCoroutine(LimpiarTextoGuardado());
    }

    private IEnumerator LimpiarTextoGuardado()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (textoGuardado != null) textoGuardado.text = "";
    }

    public void MostrarObjetivo(string objetivo, string interaccion)
    {
        if (textoObjetivo != null) textoObjetivo.text = objetivo;
        if (textoInteraccion != null) textoInteraccion.text = interaccion;
    }

    private void ActualizarVida(int actual, int maxima)
    {
        if (textoVida == null) return;
        float proporcion = (float)actual / Mathf.Max(1, maxima);
        string estadoSalud = proporcion > 0.6f ? "ÓPTIMO" : (proporcion > 0.25f ? "PRECAUCIÓN" : "PELIGRO");
        Color color = proporcion > 0.6f ? colorVerde : (proporcion > 0.25f ? colorDorado : colorRojo);

        textoVida.text = $"{estadoSalud}  ·  {actual} / {maxima}";
        textoVida.color = color;
        barraVida.color = color;
        barraVida.rectTransform.anchorMax = new Vector2(proporcion, 1f);
        barraVida.rectTransform.offsetMax = Vector2.zero;
    }

    private void ActualizarEstamina(float actual, float maxima)
    {
        if (barraEstamina == null) return;
        float prop = Mathf.Clamp01(actual / Mathf.Max(1f, maxima));
        barraEstamina.rectTransform.anchorMax = new Vector2(prop, 1f);
        barraEstamina.rectTransform.offsetMax = Vector2.zero;
        barraEstamina.color = movimiento != null && movimiento.EstaminaAgotada ? new Color(0.95f, 0.45f, 0.18f) : colorCian;
    }

    private void ActualizarCombo(int racha, float progresoTiempo)
    {
        if (panelCombo == null) return;
        if (racha < 2)
        {
            panelCombo.SetActive(false);
            return;
        }

        panelCombo.SetActive(true);
        textoCombo.text = $"COMBO x{racha}";
        int bonusPct = Mathf.Min(30, racha * 5);
        textoBonusCombo.text = $"+{bonusPct}% DAÑO DE ESPADA";
    }

    private void ActualizarInventario()
    {
        if (inventario == null) return;
        if (textoPociones != null)
        {
            textoPociones.text = $"[Q] POCIÓN  x{inventario.Pociones}";
            textoPociones.color = inventario.Pociones > 0 ? new Color(1f, 0.48f, 0.64f) : new Color(0.55f, 0.55f, 0.55f);
        }
        if (textoRupias != null)
        {
            textoRupias.text = $"◆ {inventario.Rupias} RUPIAS";
        }
    }

    private void ActualizarProgreso()
    {
        if (textoSellos != null)
        {
            textoSellos.text = $"Sellos reunidos  {progreso.Objetos.Count}/4";
        }
    }

    public void Aviso(string texto)
    {
        if (mensajeMenu != null) mensajeMenu.text = texto;
        if (mensajeHud != null) mensajeHud.text = texto;
        if (corrutinaAviso != null) StopCoroutine(corrutinaAviso);
        corrutinaAviso = StartCoroutine(LimpiarAviso());
    }

    private IEnumerator LimpiarAviso()
    {
        yield return new WaitForSecondsRealtime(4f);
        if (mensajeMenu != null) mensajeMenu.text = "";
        if (mensajeHud != null) mensajeHud.text = "";
    }

    public void MostrarEstado(GameStates estado)
    {
        panelMenu.SetActive(estado == GameStates.GameStart);
        panelHud.SetActive(estado == GameStates.InGame || estado == GameStates.Paused);

        if (panelEstado != null)
        {
            panelEstado.SetActive(false);
            Destroy(panelEstado);
        }
        panelOpcionesPausa = null;

        if (estado == GameStates.InGame || estado == GameStates.GameStart) return;

        RectTransform fondo = CrearPanelCompleto(raizCanvas, "PantallaEstado", new Color(0.015f, 0.018f, 0.019f, 0.92f));
        panelEstado = fondo.gameObject;

        if (estado == GameStates.Paused)
        {
            RectTransform cuadro = CrearPanel(fondo, "PanelCentral", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -250), new Vector2(280, 250), colorMetal);
            CrearCaja(cuadro, "LineaPausa", 0, 0, 560, 4, colorDorado);
            CrearTexto(cuadro, "PAUSA", 44, colorMarfil, 35, 24, 500, 62);

            if (inventario != null)
            {
                CrearTexto(cuadro, $"Tiempo: {FormatearTiempo(inventario.TiempoJugado)}  ·  Enemigos: {inventario.EnemigosDerrotados}  ·  Parries: {inventario.ParriesPerfectos}  ·  ◆{inventario.Rupias}", 15, colorDorado, 35, 92, 495, 28);
            }

            CrearBoton(cuadro, "REANUDAR", 35, 134, 490, 50, () => game.CambiarEstado(GameStates.InGame));
            CrearBoton(cuadro, "GUARDAR PARTIDA", 35, 196, 490, 50, () =>
            {
                game.Guardar();
                game.CambiarEstado(GameStates.InGame);
            });
            CrearBoton(cuadro, "OPCIONES Y AUDIO", 35, 258, 490, 50, () => AbrirOpcionesDesdePausa(fondo, cuadro.gameObject));
            CrearBoton(cuadro, "MENÚ PRINCIPAL", 35, 320, 490, 50, game.VolverMenu);
            CrearBoton(cuadro, "SALIR DEL JUEGO", 35, 382, 490, 50, game.Salir, 19f, new Color(0.32f, 0.11f, 0.11f, 0.95f));
        }
        else if (estado == GameStates.GameOver)
        {
            RectTransform cuadro = CrearPanel(fondo, "PanelCentral", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -220), new Vector2(280, 220), colorMetal);
            CrearTexto(cuadro, "¡Fin del juego!", 50, colorRojo, 35, 30, 490, 86);
            CrearTexto(cuadro, "Haz clic en la pantalla para reiniciar desde el último guardado.", 20, colorMarfil, 35, 136, 490, 72);

            Button clickPantalla = fondo.gameObject.AddComponent<Button>();
            clickPantalla.targetGraphic = fondo.GetComponent<Image>();
            clickPantalla.transition = Selectable.Transition.None;
            clickPantalla.onClick.AddListener(game.Reintentar);

            CrearBoton(cuadro, "REINTENTAR", 35, 240, 490, 58, game.Reintentar);
            CrearBoton(cuadro, "MENÚ PRINCIPAL", 35, 314, 490, 52, game.VolverMenu);
        }
        else if (estado == GameStates.Victory)
        {
            RectTransform cuadro = CrearPanel(fondo, "PanelCentral", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330, -265), new Vector2(330, 265), colorMetal);
            CrearCaja(cuadro, "LineaVic", 0, 0, 660, 5, colorDorado);
            TMP_Text tituloVic = CrearTexto(cuadro, "RELIQUIA RECUPERADA", 42, colorDorado, 35, 24, 590, 64);
            tituloVic.fontStyle = FontStyles.Bold;
            CrearTexto(cuadro, "¡Has reclamado la Reliquia Sagrada y derrotado a la oleada final!", 19, colorMarfil, 35, 92, 590, 36);

            if (inventario != null)
            {
                string rango = inventario.CalcularRangoFinal();
                string resumen =
                    $"Tiempo total:         {FormatearTiempo(inventario.TiempoJugado)}\n" +
                    $"Enemigos derrotados:  {inventario.EnemigosDerrotados}\n" +
                    $"Parries perfectos:    {inventario.ParriesPerfectos}\n" +
                    $"Combo máximo:         x{inventario.ComboMaximo}\n" +
                    $"Rupias recolectadas:  ◆ {inventario.Rupias}\n" +
                    $"Cofres abiertos:      {inventario.CofresAbiertos.Count}\n" +
                    $"Daño recibido:        {inventario.DanoRecibidoTotal}";

                CrearTexto(cuadro, resumen, 19, colorMarfil, 40, 146, 380, 225);

                RectTransform cajaRango = CrearCaja(cuadro, "CajaRango", 448, 160, 170, 175, colorOscuro);
                CrearEtiqueta(cajaRango, "RANGO", 18, colorDorado, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(0, -10), TextAlignmentOptions.Center);
                TMP_Text txtRango = CrearEtiqueta(cajaRango, rango, 86, rango == "S" ? colorDorado : colorVerde, Vector2.zero, Vector2.one, new Vector2(0, 10), new Vector2(0, -35), TextAlignmentOptions.Center);
                txtRango.fontStyle = FontStyles.Bold;
            }

            CrearBoton(cuadro, "MENÚ PRINCIPAL", 35, 424, 590, 56, game.VolverMenu);
        }
    }

    private void AbrirOpcionesDesdePausa(RectTransform fondo, GameObject cuadroPausa)
    {
        cuadroPausa.SetActive(false);
        RectTransform panel = CrearPanel(fondo, "OpcionesPausa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -255), new Vector2(280, 255), colorMetal);
        panelOpcionesPausa = panel.gameObject;
        CrearCaja(panel, "LineaOpt", 0, 0, 560, 4, colorDorado);
        CrearTexto(panel, "OPCIONES", 34, colorMarfil, 30, 26, 480, 54);
        MostrarOpciones(panel);
        CrearBoton(panel, "VOLVER A PAUSA", 30, 446, 240, 44, () =>
        {
            if (panelOpcionesPausa != null) Destroy(panelOpcionesPausa);
            cuadroPausa.SetActive(true);
        });
    }

    private static string FormatearTiempo(float segundos)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(segundos));
        int min = total / 60;
        int seg = total % 60;
        return $"{min:00}:{seg:00}";
    }

    private void OnDestroy()
    {
        if (vida != null)
        {
            vida.VidaCambiada -= ActualizarVida;
            vida.DanoRecibido -= MostrarFlashDano;
        }
        if (progreso != null)
        {
            progreso.Cambio -= ActualizarProgreso;
        }
        if (movimiento != null)
        {
            movimiento.EstaminaCambiada -= ActualizarEstamina;
        }
        if (combate != null)
        {
            combate.ComboCambiado -= ActualizarCombo;
        }
        if (inventario != null)
        {
            inventario.InventarioCambiado -= ActualizarInventario;
        }
    }
}
