using System.Collections.Generic;
using UnityEngine;

// Administra la musica de fondo por fases del juego y los efectos de sonido con cache en memoria
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private enum FaseMusica { Mazmorra, Jefe, SinEnemigos }

    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;
    private readonly Dictionary<string, AudioClip> cacheEfectos = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> cacheMusica = new Dictionary<string, AudioClip>();

    private FaseMusica faseJuego = FaseMusica.Mazmorra;
    private string musicaActual;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.loop = true;
        fuenteMusica.playOnAwake = false;
        fuenteMusica.spatialBlend = 0f;
        fuenteMusica.volume = 0.35f;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;
        fuenteEfectos.spatialBlend = 0f;
        fuenteEfectos.volume = 0.65f;
    }

    // Decide que pista reproducir segun el estado general de la partida
    public static void ActualizarEstadoJuego(GameStates estado)
    {
        if (Instance == null) return;

        if (estado == GameStates.GameStart)
        {
            Instance.faseJuego = FaseMusica.Mazmorra;
            ReproducirMusica("DekuMenu");
        }
        else if (estado == GameStates.InGame)
        {
            ReproducirMusica(Instance.ObtenerPistaFaseActual());
        }
        else if (estado == GameStates.Victory)
        {
            Instance.faseJuego = FaseMusica.SinEnemigos;
            ReproducirMusica("MusicaSinEnemigos");
        }
        else if (estado == GameStates.GameOver)
        {
            DetenerMusica();
        }
    }

    public static void ActivarMusicaJefe()
    {
        if (Instance == null) return;
        if (Instance.faseJuego == FaseMusica.Jefe || Instance.faseJuego == FaseMusica.SinEnemigos) return;

        Instance.faseJuego = FaseMusica.Jefe;
        if (GameManager.Instance != null && GameManager.Instance.EnJuego)
        {
            ReproducirMusica("MusicaJefe");
        }
    }

    public static void ActivarMusicaSinEnemigos()
    {
        if (Instance == null) return;
        if (Instance.faseJuego == FaseMusica.SinEnemigos) return;

        Instance.faseJuego = FaseMusica.SinEnemigos;
        if (GameManager.Instance != null && (GameManager.Instance.EnJuego || GameManager.Instance.Estado == GameStates.Victory))
        {
            ReproducirMusica("MusicaSinEnemigos");
        }
    }

    private string ObtenerPistaFaseActual()
    {
        switch (faseJuego)
        {
            case FaseMusica.Jefe:
                return "MusicaJefe";
            case FaseMusica.SinEnemigos:
                return "MusicaSinEnemigos";
            case FaseMusica.Mazmorra:
            default:
                return "MusicaMazmorra";
        }
    }

    private static void ReproducirMusica(string nombre)
    {
        if (Instance == null || string.IsNullOrEmpty(nombre)) return;

        // Si esa cancion ya esta sonando (ej. al volver de Pausa), no la reinicia
        if (Instance.musicaActual == nombre && Instance.fuenteMusica.isPlaying) return;

        if (!Instance.cacheMusica.TryGetValue(nombre, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>("Audio/Music/" + nombre);
            Instance.cacheMusica[nombre] = clip;
        }

        if (clip == null) return;

        Instance.musicaActual = nombre;
        Instance.fuenteMusica.clip = clip;
        Instance.fuenteMusica.Play();
    }

    public static void DetenerMusica()
    {
        if (Instance == null) return;
        Instance.musicaActual = null;
        Instance.fuenteMusica.Stop();
        Instance.fuenteMusica.clip = null;
    }

    // Reproduce un efecto desde cache para evitar llamadas repetidas a Resources.Load
    public static void Reproducir(string nombre, Vector3? posicion = null)
    {
        if (Instance == null || string.IsNullOrEmpty(nombre)) return;

        if (!Instance.cacheEfectos.TryGetValue(nombre, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>("Audio/SFX/" + nombre);
            Instance.cacheEfectos[nombre] = clip;
        }

        if (clip == null) return;

        if (posicion.HasValue)
        {
            AudioSource.PlayClipAtPoint(clip, posicion.Value, 0.6f);
        }
        else
        {
            Instance.fuenteEfectos.PlayOneShot(clip);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}