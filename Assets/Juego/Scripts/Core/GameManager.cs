using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameStates
{
    GameStart,
    InGame,
    Paused,
    GameOver,
    Victory
}

// Manager principal de la partida: estados, carga/guardado y referencias globales
public class GameManager : MonoBehaviour
{
    #region CLASS_VARIABLES

    public static GameManager Instance { get; private set; }
    public GameStates Estado { get; private set; }
    public bool EnJuego => Estado == GameStates.InGame;
    public string Perfil { get; private set; }

    private VidaJugador vidaJugador;
    private Movimiento3D movimientoJugador;
    private CombateJugador combateJugador;
    private InventarioJugador inventarioJugador;
    private SistemaFijadoObjetivo fijadoObjetivo;
    private CamaraTerceraPersona camaraJugador;
    private ProgresoPartida progreso;
    private HUDManager hud;
    private MazmorraPuzzles puzzles;
    private MejorasMazmorra mejoras;

    private Vector3 posicionInicio;
    private readonly HashSet<string> enemigosMuertos = new HashSet<string>();
    private readonly List<Enemy> enemigosSuscritos = new List<Enemy>();
    private static string perfilPorCargar;
    private bool esperandoGameOver;

    public InventarioJugador Inventario => inventarioJugador;
    public MazmorraPuzzles Puzzles => puzzles;
    public MejorasMazmorra Mejoras => mejoras;

    #endregion

    #region UNITY_METHODS

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        vidaJugador = FindFirstObjectByType<VidaJugador>();
        if (vidaJugador == null)
        {
            Debug.LogError("Falta el componente VidaJugador en la escena.");
            return;
        }

        movimientoJugador = vidaJugador.GetComponent<Movimiento3D>();
        combateJugador = vidaJugador.GetComponent<CombateJugador>();
        inventarioJugador = vidaJugador.GetComponent<InventarioJugador>();
        if (inventarioJugador == null)
        {
            inventarioJugador = vidaJugador.gameObject.AddComponent<InventarioJugador>();
        }

        fijadoObjetivo = vidaJugador.GetComponent<SistemaFijadoObjetivo>();
        if (fijadoObjetivo == null)
        {
            fijadoObjetivo = vidaJugador.gameObject.AddComponent<SistemaFijadoObjetivo>();
        }

        camaraJugador = FindFirstObjectByType<CamaraTerceraPersona>();
        progreso = GetComponent<ProgresoPartida>();
        hud = GetComponent<HUDManager>();
        mejoras = GetComponent<MejorasMazmorra>();
        if (mejoras == null)
        {
            mejoras = gameObject.AddComponent<MejorasMazmorra>();
        }

        posicionInicio = vidaJugador.transform.position;
        mejoras.Construir();

        puzzles = FindFirstObjectByType<MazmorraPuzzles>();
        hud.Construir(
            this,
            vidaJugador,
            movimientoJugador,
            combateJugador,
            inventarioJugador,
            fijadoObjetivo,
            progreso,
            puzzles,
            mejoras
        );

        vidaJugador.Murio += AlMorirJugador;

        if (puzzles != null)
        {
            puzzles.Inicializar(this, vidaJugador, progreso, hud, mejoras);
        }

        Enemy[] enemigos = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemigos.Length; i++)
        {
            SuscribirEnemigo(enemigos[i]);
        }

        // Si se eligio un perfil antes de recargar la escena, se restaura aqui
        if (!string.IsNullOrEmpty(perfilPorCargar))
        {
            string nombre = perfilPorCargar;
            perfilPorCargar = null;

            try
            {
                Restaurar(SaveManager.Cargar(nombre));
                bool victoria = puzzles != null && puzzles.FinalCompletado;
                CambiarEstado(victoria ? GameStates.Victory : GameStates.InGame);
            }
            catch (Exception ex)
            {
                CambiarEstado(GameStates.GameStart);
                hud.Aviso("No se pudo cargar: " + ex.Message);
            }
        }
        else
        {
            CambiarEstado(GameStates.GameStart);
        }
    }

    private void Update()
    {
        if (esperandoGameOver)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (EnJuego)
            {
                CambiarEstado(GameStates.Paused);
            }
            else if (Estado == GameStates.Paused)
            {
                CambiarEstado(GameStates.InGame);
            }
        }

        if (EnJuego && Input.GetKeyDown(KeyCode.F5))
        {
            Guardar();
        }

        // Cambia a musica de jefe cuando el jugador entra a la sala del altar
        if (EnJuego && vidaJugador != null && vidaJugador.transform.position.z > 18.5f)
        {
            if (puzzles == null || !puzzles.FinalCompletado)
            {
                AudioManager.ActivarMusicaJefe();
            }
        }
    }

    private void OnDestroy()
    {
        if (vidaJugador != null)
        {
            vidaJugador.Murio -= AlMorirJugador;
        }

        for (int i = 0; i < enemigosSuscritos.Count; i++)
        {
            if (enemigosSuscritos[i] != null)
            {
                enemigosSuscritos[i].Murio -= RegistrarMuerte;
            }
        }
        enemigosSuscritos.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region CLASS_METHODS

    private void SuscribirEnemigo(Enemy enemigo)
    {
        if (enemigo == null)
            return;

        enemigo.Murio -= RegistrarMuerte;
        enemigo.Murio += RegistrarMuerte;

        if (!enemigosSuscritos.Contains(enemigo))
        {
            enemigosSuscritos.Add(enemigo);
        }
    }

    private void RegistrarMuerte(Enemy enemigo)
    {
        if (enemigo != null)
        {
            enemigosMuertos.Add(enemigo.Id);
            if (inventarioJugador != null)
            {
                inventarioJugador.RegistrarEnemigoDerrotado();
            }
        }

        if (puzzles != null && puzzles.Oleada != null && puzzles.Oleada.Completa)
        {
            AudioManager.ActivarMusicaSinEnemigos();
        }
    }

    public bool EnemigoEliminado(string id)
    {
        return enemigosMuertos.Contains(id);
    }

    public void RegistrarEnemigoNuevo(Enemy enemigo)
    {
        if (enemigo == null)
            return;

        SuscribirEnemigo(enemigo);
        AudioManager.ActivarMusicaJefe();
    }

    public void Nueva(string nombre)
    {
        nombre = nombre != null ? nombre.Trim() : "";

        if (!SaveManager.NombreValido(nombre))
        {
            hud.Aviso("Usa 1-32 letras, números, guion o guion bajo.");
            return;
        }

        if (SaveManager.Existe(nombre))
        {
            hud.Aviso("Ese perfil ya existe. Usa Continuar partida.");
            return;
        }

        try
        {
            DatosPartida datosIniciales = new DatosPartida();
            datosIniciales.nombre = nombre;
            datosIniciales.posicion = posicionInicio;
            datosIniciales.vida = vidaJugador.VidaMaxima;
            datosIniciales.pocionesInicializadas = true;
            datosIniciales.pociones = InventarioJugador.PocionesIniciales;
            datosIniciales.ultimaFechaGuardado = DateTime.Now.ToString("dd/MM HH:mm");

            SaveManager.Guardar(datosIniciales);
            Cargar(nombre);
        }
        catch (Exception ex)
        {
            hud.Aviso("No se pudo crear: " + ex.Message);
        }
    }

    public void Cargar(string nombre)
    {
        try
        {
            SaveManager.Cargar(nombre);
            perfilPorCargar = nombre;
            Recargar();
        }
        catch (Exception ex)
        {
            hud.Aviso("No se pudo cargar: " + ex.Message);
        }
    }

    public bool EliminarPerfil(string nombre)
    {
        try
        {
            bool borrado = SaveManager.Eliminar(nombre);
            if (borrado)
            {
                hud.Aviso("Perfil eliminado: " + nombre);
            }
            return borrado;
        }
        catch (Exception ex)
        {
            hud.Aviso("No se pudo eliminar: " + ex.Message);
            return false;
        }
    }

    private void Restaurar(DatosPartida datos)
    {
        Perfil = datos.nombre;
        Rigidbody cuerpo = vidaJugador.GetComponent<Rigidbody>();
        Vector3 posicion = datos.posicion;

        // Deja al personaje al ras del suelo de la mazmorra
        if (Mathf.Abs(posicion.x) < 18f && posicion.z > -18f && posicion.z < 30f)
        {
            posicion.y = 0.04f;
        }

        if (cuerpo != null)
        {
            cuerpo.position = posicion;
            cuerpo.linearVelocity = Vector3.zero;
        }
        vidaJugador.transform.position = posicion;

        progreso.Restaurar(datos.salasResueltas, datos.objetos);

        if (inventarioJugador != null)
        {
            inventarioJugador.Restaurar(datos);
        }
        if (mejoras != null)
        {
            mejoras.SincronizarConInventario(inventarioJugador);
        }

        enemigosMuertos.Clear();
        if (datos.enemigosMuertos != null)
        {
            enemigosMuertos.UnionWith(datos.enemigosMuertos);
        }

        Enemy[] enemigosEscena = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemigosEscena.Length; i++)
        {
            if (enemigosMuertos.Contains(enemigosEscena[i].Id))
            {
                enemigosEscena[i].EliminarAlCargar();
            }
        }

        if (puzzles != null)
        {
            puzzles.Restaurar(datos);

            // Evita quedar atras de la reja si todavia esta cerrada
            if (datos.posicion.z > 18f && !puzzles.EntradaFinalAbierta)
            {
                if (cuerpo != null)
                {
                    cuerpo.position = posicionInicio;
                }
                vidaJugador.transform.position = posicionInicio;
            }
        }

        bool potenciado = puzzles != null && puzzles.Recuperado;
        int vidaFinal = potenciado && datos.versionFinal == 0 ? VidaJugador.VidaConReliquia : datos.vida;
        vidaJugador.RestaurarMaxima(potenciado, vidaFinal);
        Physics.SyncTransforms();

        bool oleadaVencida = puzzles != null && (puzzles.FinalCompletado || (puzzles.Oleada != null && puzzles.Oleada.Completa));
        if (oleadaVencida)
        {
            AudioManager.ActivarMusicaSinEnemigos();
        }
        else if (vidaJugador.transform.position.z > 18.5f || (puzzles != null && puzzles.Recuperado))
        {
            AudioManager.ActivarMusicaJefe();
        }
    }

    public void Guardar()
    {
        Guardar(true);
    }

    public void Guardar(bool mostrarAviso)
    {
        if (string.IsNullOrEmpty(Perfil) || vidaJugador == null || vidaJugador.Muerto)
            return;

        try
        {
            DatosPartida datos = new DatosPartida();
            datos.nombre = Perfil;
            datos.posicion = vidaJugador.transform.position;
            datos.vida = vidaJugador.Vida;
            datos.salasResueltas = new List<string>(progreso.Salas);
            datos.objetos = new List<string>(progreso.Objetos);
            datos.enemigosMuertos = new List<string>(enemigosMuertos);

            if (puzzles != null)
            {
                puzzles.Escribir(datos);
            }
            if (inventarioJugador != null)
            {
                inventarioJugador.Escribir(datos);
            }

            SaveManager.Guardar(datos);

            if (mostrarAviso)
            {
                AudioManager.Reproducir("UI_Save");
                hud.Aviso("Partida guardada");
            }
            else
            {
                hud.MostrarGuardado();
            }
        }
        catch (Exception ex)
        {
            hud.Aviso("Error al guardar: " + ex.Message);
        }
    }

    public void CambiarEstado(GameStates nuevoEstado)
    {
        Estado = nuevoEstado;
        Time.timeScale = EnJuego ? 1f : 0f;

        if (movimientoJugador != null)
        {
            movimientoJugador.CanMove = EnJuego;
        }
        if (camaraJugador != null)
        {
            camaraJugador.PermitirControl = EnJuego;
        }

        Cursor.lockState = EnJuego ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !EnJuego;

        AudioManager.ActualizarEstadoJuego(nuevoEstado);

        if (hud != null)
        {
            hud.MostrarEstado(nuevoEstado);
        }
    }

    private void AlMorirJugador()
    {
        if (esperandoGameOver)
            return;

        esperandoGameOver = true;

        if (fijadoObjetivo != null)
        {
            fijadoObjetivo.LiberarObjetivo();
        }
        if (movimientoJugador != null)
        {
            movimientoJugador.CanMove = false;
        }
        if (camaraJugador != null)
        {
            camaraJugador.PermitirControl = false;
        }

        StartCoroutine(MostrarGameOverConRetraso());
    }

    private IEnumerator MostrarGameOverConRetraso()
    {
        // Espera un segundo para que se vea la animacion de muerte antes de pausar
        yield return new WaitForSecondsRealtime(1.1f);
        esperandoGameOver = false;
        CambiarEstado(GameStates.GameOver);
    }

    public void Ganar()
    {
        if (fijadoObjetivo != null)
        {
            fijadoObjetivo.LiberarObjetivo();
        }

        AudioManager.Reproducir("Game_Victory");
        CambiarEstado(GameStates.Victory);
    }

    public void Reintentar()
    {
        if (!string.IsNullOrEmpty(Perfil))
        {
            Cargar(Perfil);
        }
        else
        {
            VolverMenu();
        }
    }

    public void VolverMenu()
    {
        perfilPorCargar = null;
        Recargar();
    }

    private void Recargar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Salir()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    #endregion
}
