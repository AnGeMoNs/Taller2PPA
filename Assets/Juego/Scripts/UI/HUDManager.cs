using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Construye y actualiza el menu principal, el HUD de juego y las pantallas de pausa, derrota y victoria
public class HUDManager : MonoBehaviour
{
    private const int GuardadosPorPagina = 6;

    private GameManager game;
    private VidaJugador vida;
    private ProgresoPartida progreso;
    private GameObject panelMenu;
    private GameObject panelHud;
    private GameObject panelEstado;
    private GameObject subpanelActual;
    private RectTransform raizCanvas;

    private TMP_Text textoVida;
    private TMP_Text textoSellos;
    private TMP_Text mensajeHud;
    private TMP_Text mensajeMenu;
    private TMP_Text textoObjetivo;
    private TMP_Text textoInteraccion;
    private TMP_Text textoGuardado;
    private TMP_Text textoSala;
    private TMP_Text textoPoder;
    private Image barraVida;
    private Image flashDano;
    private InputField campoNombreActivo;

    private Coroutine corrutinaGuardado;
    private Coroutine corrutinaAviso;
    private Coroutine corrutinaFlash;
    private TMP_FontAsset fuenteTmp;
    private Font fuenteLegacy;

    private readonly Color colorOscuro = new Color(0.035f, 0.041f, 0.042f, 0.96f);
    private readonly Color colorMarfil = new Color(0.90f, 0.89f, 0.84f);
    private readonly Color colorRojo = new Color(0.72f, 0.16f, 0.14f);
    private readonly Color colorVerde = new Color(0.32f, 0.72f, 0.46f);
    private readonly Color colorMetal = new Color(0.10f, 0.13f, 0.14f, 0.96f);
    private readonly Color colorDorado = new Color(0.84f, 0.68f, 0.36f);

    public void Construir(GameManager gm, VidaJugador vidaJugador, ProgresoPartida prog)
    {
        game = gm;
        vida = vidaJugador;
        progreso = prog;
        fuenteLegacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Carga segura de fuente TMP para evitar errores si faltan recursos iniciales
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

        ActualizarVida(vida.Vida, vida.VidaMaxima);
        ActualizarProgreso();
    }

    private void Update()
    {
        // Permite confirmar el nombre de nueva partida presionando Enter
        if (campoNombreActivo != null && campoNombreActivo.gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Return))
        {
            game.Nueva(campoNombreActivo.text);
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
        rect.gameObject.AddComponent<Image>().color = color;
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

    private Button CrearBoton(Transform padre, string titulo, float x, float y, float ancho, float alto, Action accionClick)
    {
        RectTransform caja = CrearCaja(padre, titulo, x, y, ancho, alto, colorMetal);
        Button boton = caja.gameObject.AddComponent<Button>();

        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1f, 0.84f, 0.56f);
        colores.pressedColor = new Color(0.7f, 0.52f, 0.28f);
        boton.colors = colores;

        boton.targetGraphic = caja.GetComponent<Image>();
        CrearEtiqueta(caja, titulo, 22, colorMarfil, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-8, 0));
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

        TMP_Text titulo = CrearTexto(panelIzquierdo, "LA MAZMORRA DE LINK", 40, colorMarfil, 52, 42, 540, 62);
        titulo.fontStyle = FontStyles.Bold;
        CrearTexto(panelIzquierdo, "L A   R E L I Q U I A   S A G R A D A", 18, colorDorado, 54, 110, 500, 36);
        CrearTexto(panelIzquierdo, "AVENTURA / MAZMORRA 3D", 15, new Color(0.60f, 0.68f, 0.64f), 54, 154, 500, 28);

        CrearBoton(panelIzquierdo, "NUEVA PARTIDA", 54, 232, 400, 50, MostrarNuevaPartida);
        CrearBoton(panelIzquierdo, "CONTINUAR PARTIDA", 54, 294, 400, 50, () => MostrarPartidasGuardadas(0));
        CrearBoton(panelIzquierdo, "CONTROLES", 54, 356, 400, 50, MostrarControles);
        CrearBoton(panelIzquierdo, "CRÉDITOS", 54, 418, 400, 50, MostrarCreditos);
        CrearBoton(panelIzquierdo, "SALIR", 54, 480, 400, 50, game.Salir);

        mensajeMenu = CrearTexto(panelIzquierdo, "", 17, colorDorado, 54, 544, 500, 64);
        CrearEtiqueta(raiz, "REÚNE LOS CUATRO SELLOS Y RECLAMA LA RELIQUIA.", 17, colorDorado, new Vector2(0.52f, 0.09f), new Vector2(0.97f, 0.18f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
    }

    private RectTransform AbrirSubmenu(string titulo)
    {
        campoNombreActivo = null;
        if (subpanelActual != null)
        {
            subpanelActual.SetActive(false);
            Destroy(subpanelActual);
        }

        RectTransform panel = CrearPanel(panelMenu.transform, "Submenu", new Vector2(0.51f, 0.10f), new Vector2(0.98f, 0.90f), Vector2.zero, Vector2.zero, colorMetal);
        subpanelActual = panel.gameObject;

        CrearCaja(panel, "Linea", 24, 29, 4, 55, colorDorado);
        CrearTexto(panel, titulo, 30, colorMarfil, 42, 29, 500, 60);
        CrearBoton(panel, "VOLVER", 30, 480, 220, 42, () =>
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
        CrearTexto(panel, "Cada perfil guarda su progreso en un archivo JSON independiente.", 18, colorDorado, 30, 302, 500, 70);
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
            CrearBoton(panel, $"{datos.nombre}  ·  Vida {datos.vida}  ·  {sellos}/4", 30, 98 + fila * 51, 510, 42, () => game.Cargar(datos.nombre));
        }

        if (pagina > 0)
        {
            CrearBoton(panel, "ANTERIOR", 30, 421, 220, 42, () => MostrarPartidasGuardadas(pagina - 1));
        }
        if (fin < guardados.Count)
        {
            CrearBoton(panel, "SIGUIENTE", 280, 421, 220, 42, () => MostrarPartidasGuardadas(pagina + 1));
        }
    }

    private void MostrarControles()
    {
        RectTransform panel = AbrirSubmenu("CONTROLES");
        CrearTexto(
            panel,
            "WASD           Movimiento\nSHIFT          Correr\nRATÓN          Cámara (Rueda: Zoom)\nCLIC IZQ.      Combo de espada (x3)\nCLIC DER.      Bloquear con escudo (-75% daño)\nE              Interactuar\nF5             Guardar partida\nESC            Pausa",
            21,
            colorMarfil,
            30,
            102,
            520,
            360);
    }

    private void MostrarCreditos()
    {
        RectTransform panel = AbrirSubmenu("CRÉDITOS");
        CrearTexto(
            panel,
            "La Mazmorra de Link: La Reliquia Sagrada\nProyecto de Programación Avanzada\nUniversidad Católica del Norte\n\nDesarrolladores: completar nombres\nPersonaje y animaciones: Link (Mixamo Humanoid)\nEscenario y enemigos: paquete del taller",
            20,
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

        // Capa transparente para el destello rojo al recibir dano o celeste al bloquear
        RectTransform rectFlash = CrearPanelCompleto(raiz, "FlashDano", new Color(0.8f, 0.1f, 0.1f, 0f));
        flashDano = rectFlash.GetComponent<Image>();
        flashDano.raycastTarget = false;

        // Panel de vida abajo a la izquierda
        RectTransform panelVida = CrearPanel(raiz, "EstadoVital", new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 26), new Vector2(376, 134), colorMetal);
        CrearCaja(panelVida, "BordeVida", 0, 0, 4, 108, colorDorado);
        CrearTexto(panelVida, "ESTADO VITAL", 15, colorDorado, 18, 10, 300, 22);
        textoVida = CrearTexto(panelVida, "", 23, colorMarfil, 18, 34, 310, 32);

        RectTransform fondoBarra = CrearCaja(panelVida, "BarraFondo", 18, 72, 310, 12, colorOscuro);
        barraVida = CrearPanelCompleto(fondoBarra, "BarraVida", colorVerde).GetComponent<Image>();
        textoPoder = CrearTexto(panelVida, "", 14, colorDorado, 18, 88, 310, 20);

        // Indicador de sala arriba a la izquierda
        RectTransform panelSala = CrearPanel(raiz, "ZonaActual", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -76), new Vector2(320, -24), colorMetal);
        CrearCaja(panelSala, "BordeSala", 0, 0, 4, 52, colorDorado);
        textoSala = CrearTexto(panelSala, "VESTÍBULO CENTRAL", 16, colorDorado, 16, 15, 270, 26);

        // Contador de sellos arriba a la derecha
        RectTransform panelObjetos = CrearPanel(raiz, "Objetos", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-346, -112), new Vector2(-30, -24), colorMetal);
        CrearCaja(panelObjetos, "BordeObj", 0, 0, 4, 88, colorDorado);
        CrearTexto(panelObjetos, "OBJETIVO · LA RELIQUIA SAGRADA", 14, colorDorado, 16, 10, 294, 22);
        textoSellos = CrearTexto(panelObjetos, "", 21, colorMarfil, 16, 40, 286, 34);

        mensajeHud = CrearEtiqueta(raiz, "", 20, colorDorado, new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.12f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        textoGuardado = CrearEtiqueta(raiz, "", 15, colorMarfil, new Vector2(0.73f, 0.12f), new Vector2(0.97f, 0.17f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
        textoObjetivo = CrearEtiqueta(raiz, "", 18, colorMarfil, new Vector2(0.24f, 0.86f), new Vector2(0.76f, 0.96f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        textoInteraccion = CrearEtiqueta(raiz, "", 24, colorDorado, new Vector2(0.25f, 0.18f), new Vector2(0.75f, 0.26f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        CrearEtiqueta(raiz, "Clic Izq. Espada · Clic Der. Escudo · F5 Guardar · ESC Pausa", 14, colorMarfil, new Vector2(0.50f, 0.025f), new Vector2(0.97f, 0.065f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
    }

    public void MostrarSala(string sala, bool potenciado)
    {
        if (textoSala != null) textoSala.text = sala;
        if (textoPoder != null) textoPoder.text = potenciado ? "★ RELIQUIA ACTIVA · DAÑO x2" : "";
    }

    private void MostrarFlashDano(bool bloqueado)
    {
        if (flashDano == null) return;
        if (corrutinaFlash != null) StopCoroutine(corrutinaFlash);
        Color colorInicial = bloqueado ? new Color(0.45f, 0.78f, 1f, 0.22f) : new Color(0.85f, 0.12f, 0.10f, 0.34f);
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

        if (estado == GameStates.InGame || estado == GameStates.GameStart) return;

        RectTransform fondo = CrearPanelCompleto(raizCanvas, "PantallaEstado", new Color(0.015f, 0.018f, 0.019f, 0.94f));
        panelEstado = fondo.gameObject;
        RectTransform cuadro = CrearPanel(fondo, "PanelCentral", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -220), new Vector2(280, 220), colorMetal);

        if (estado == GameStates.Paused)
        {
            CrearTexto(cuadro, "PAUSA", 48, colorMarfil, 35, 30, 500, 70);
            CrearBoton(cuadro, "REANUDAR", 35, 135, 490, 52, () => game.CambiarEstado(GameStates.InGame));
            CrearBoton(cuadro, "GUARDAR", 35, 204, 490, 52, () =>
            {
                game.Guardar();
                game.CambiarEstado(GameStates.InGame);
            });
            CrearBoton(cuadro, "MENÚ PRINCIPAL", 35, 273, 490, 52, game.VolverMenu);
        }
        else if (estado == GameStates.GameOver)
        {
            CrearTexto(cuadro, "¡Fin del juego!", 50, colorRojo, 35, 30, 490, 100);
            CrearTexto(cuadro, "Haz clic en la pantalla para reiniciar desde el último guardado.", 21, colorMarfil, 35, 150, 490, 80);

            Button clickPantalla = fondo.gameObject.AddComponent<Button>();
            clickPantalla.targetGraphic = fondo.GetComponent<Image>();
            clickPantalla.transition = Selectable.Transition.None;
            clickPantalla.onClick.AddListener(game.Reintentar);

            CrearBoton(cuadro, "REINTENTAR", 35, 260, 490, 60, game.Reintentar);
        }
        else if (estado == GameStates.Victory)
        {
            CrearTexto(cuadro, "RELIQUIA RECUPERADA", 42, colorDorado, 35, 30, 500, 80);
            CrearTexto(cuadro, "¡Has reclamado la Reliquia Sagrada y derrotado a los guardianes!\nCréditos: completar nombres de los desarrolladores.", 21, colorMarfil, 35, 130, 490, 120);
            CrearBoton(cuadro, "MENÚ PRINCIPAL", 35, 285, 490, 56, game.VolverMenu);
        }
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
    }
}
