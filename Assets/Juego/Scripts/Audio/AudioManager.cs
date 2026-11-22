using System.Collections.Generic;
using UnityEngine;

// Administra la musica con crossfade suave, pool de efectos 3D, volumen configurable y SFX sinteticos
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum SonidoSintetico
    {
        Parry,
        Esquiva,
        AtaqueGiratorio,
        Curacion,
        Rupia,
        Cofre,
        RomperObjeto,
        FijarObjetivo,
        Aturdimiento
    }

    private enum FaseMusica { Mazmorra, Jefe, SinEnemigos }

    private const string PrefVolMusica = "Mazmorra.VolMusica";
    private const string PrefVolEfectos = "Mazmorra.VolEfectos";
    private const int CantidadFuentes3D = 8;
    private const float DuracionCrossfade = 0.85f;

    private AudioSource musicaA;
    private AudioSource musicaB;
    private AudioSource fuenteActiva;
    private AudioSource fuenteSaliente;
    private AudioSource fuenteEfectos2D;
    private AudioSource[] poolFuentes3D;
    private int indicePool3D;

    private readonly Dictionary<string, AudioClip> cacheEfectos = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> cacheMusica = new Dictionary<string, AudioClip>();
    private readonly Dictionary<SonidoSintetico, AudioClip> cacheSinteticos = new Dictionary<SonidoSintetico, AudioClip>();

    private FaseMusica faseJuego = FaseMusica.Mazmorra;
    private string musicaActual;
    private float progresoFade = 1f;
    private float volumenMusica = 0.40f;
    private float volumenEfectos = 0.72f;

    public static float VolumenMusica => Instance != null ? Instance.volumenMusica : PlayerPrefs.GetFloat(PrefVolMusica, 0.40f);
    public static float VolumenEfectos => Instance != null ? Instance.volumenEfectos : PlayerPrefs.GetFloat(PrefVolEfectos, 0.72f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        volumenMusica = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefVolMusica, 0.40f));
        volumenEfectos = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefVolEfectos, 0.72f));

        musicaA = CrearFuenteMusica();
        musicaB = CrearFuenteMusica();
        fuenteActiva = musicaA;
        fuenteSaliente = musicaB;

        fuenteEfectos2D = gameObject.AddComponent<AudioSource>();
        fuenteEfectos2D.playOnAwake = false;
        fuenteEfectos2D.spatialBlend = 0f;
        fuenteEfectos2D.volume = volumenEfectos;

        poolFuentes3D = new AudioSource[CantidadFuentes3D];
        for (int i = 0; i < CantidadFuentes3D; i++)
        {
            GameObject nodo = new GameObject("SFX_3D_" + i);
            nodo.transform.SetParent(transform, false);
            AudioSource src = nodo.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0.85f;
            src.minDistance = 2.5f;
            src.maxDistance = 24f;
            src.rolloffMode = AudioRolloffMode.Linear;
            poolFuentes3D[i] = src;
        }
    }

    private AudioSource CrearFuenteMusica()
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }

    private void Update()
    {
        if (progresoFade < 1f)
        {
            progresoFade = Mathf.Min(1f, progresoFade + Time.unscaledDeltaTime / DuracionCrossfade);
            if (fuenteActiva != null) fuenteActiva.volume = volumenMusica * progresoFade;
            if (fuenteSaliente != null)
            {
                fuenteSaliente.volume = volumenMusica * (1f - progresoFade);
                if (progresoFade >= 1f && fuenteSaliente.isPlaying)
                {
                    fuenteSaliente.Stop();
                    fuenteSaliente.clip = null;
                }
            }
        }
    }

    public static void ConfigurarVolumenMusica(float valor)
    {
        float v = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(PrefVolMusica, v);
        PlayerPrefs.Save();
        if (Instance == null) return;
        Instance.volumenMusica = v;
        if (Instance.fuenteActiva != null && Instance.fuenteActiva.isPlaying)
        {
            Instance.fuenteActiva.volume = v * Instance.progresoFade;
        }
    }

    public static void ConfigurarVolumenEfectos(float valor)
    {
        float v = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(PrefVolEfectos, v);
        PlayerPrefs.Save();
        if (Instance == null) return;
        Instance.volumenEfectos = v;
        if (Instance.fuenteEfectos2D != null) Instance.fuenteEfectos2D.volume = v;
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

        if (Instance.musicaActual == nombre && Instance.fuenteActiva != null && Instance.fuenteActiva.isPlaying) return;

        if (!Instance.cacheMusica.TryGetValue(nombre, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>("Audio/Music/" + nombre);
            Instance.cacheMusica[nombre] = clip;
        }

        if (clip == null) return;

        // Intercambia fuentes para realizar crossfade suave
        AudioSource anterior = Instance.fuenteActiva;
        Instance.fuenteActiva = Instance.fuenteSaliente;
        Instance.fuenteSaliente = anterior;

        Instance.musicaActual = nombre;
        Instance.fuenteActiva.clip = clip;
        Instance.fuenteActiva.volume = 0f;
        Instance.fuenteActiva.Play();
        Instance.progresoFade = 0f;
    }

    public static void DetenerMusica()
    {
        if (Instance == null) return;
        Instance.musicaActual = null;
        Instance.progresoFade = 1f;
        if (Instance.musicaA != null) { Instance.musicaA.Stop(); Instance.musicaA.clip = null; }
        if (Instance.musicaB != null) { Instance.musicaB.Stop(); Instance.musicaB.clip = null; }
    }

    // Reproduce un efecto desde cache con soporte opcional de posicion 3D, volumen y variacion de tono
    public static void Reproducir(string nombre, Vector3? posicion = null, float escalaVolumen = 1f, float pitchBase = 1f, float variacionPitch = 0f)
    {
        if (Instance == null || string.IsNullOrEmpty(nombre)) return;

        if (!Instance.cacheEfectos.TryGetValue(nombre, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>("Audio/SFX/" + nombre);
            Instance.cacheEfectos[nombre] = clip;
        }

        if (clip == null) return;
        Instance.ReproducirClip(clip, posicion, escalaVolumen, pitchBase, variacionPitch);
    }

    public static void ReproducirSintetico(SonidoSintetico tipo, float escalaVolumen = 0.7f, Vector3? posicion = null)
    {
        if (Instance == null) return;

        if (!Instance.cacheSinteticos.TryGetValue(tipo, out AudioClip clip))
        {
            clip = GenerarClipSintetico(tipo);
            Instance.cacheSinteticos[tipo] = clip;
        }

        if (clip != null)
        {
            Instance.ReproducirClip(clip, posicion, escalaVolumen, 1f, 0.04f);
        }
    }

    private void ReproducirClip(AudioClip clip, Vector3? posicion, float escalaVolumen, float pitchBase, float variacionPitch)
    {
        float volFinal = Mathf.Clamp01(volumenEfectos * escalaVolumen);
        if (volFinal <= 0.001f) return;

        float pitch = pitchBase + (variacionPitch > 0f ? Random.Range(-variacionPitch, variacionPitch) : 0f);
        pitch = Mathf.Clamp(pitch, 0.55f, 1.85f);

        if (posicion.HasValue && poolFuentes3D != null && poolFuentes3D.Length > 0)
        {
            AudioSource src = poolFuentes3D[indicePool3D];
            indicePool3D = (indicePool3D + 1) % poolFuentes3D.Length;
            src.transform.position = posicion.Value;
            src.pitch = pitch;
            src.volume = volFinal;
            src.PlayOneShot(clip, 1f);
        }
        else if (fuenteEfectos2D != null)
        {
            fuenteEfectos2D.pitch = pitch;
            fuenteEfectos2D.PlayOneShot(clip, volFinal);
        }
    }

    private static AudioClip GenerarClipSintetico(SonidoSintetico tipo)
    {
        const int sr = 22050;
        float duracion = tipo == SonidoSintetico.Cofre ? 0.55f : (tipo == SonidoSintetico.Curacion ? 0.45f : 0.28f);
        int muestras = Mathf.RoundToInt(sr * duracion);
        float[] datos = new float[muestras];
        System.Random rng = new System.Random((int)tipo * 997 + 13);

        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / sr;
            float n = (float)i / muestras;
            float env = Mathf.Exp(-n * 5.2f);
            float muestra = 0f;

            switch (tipo)
            {
                case SonidoSintetico.Parry:
                    // Timbre metalico brillante de escudo + espada
                    muestra = (Mathf.Sin(2f * Mathf.PI * 1080f * t) * 0.55f
                        + Mathf.Sin(2f * Mathf.PI * 1640f * t) * 0.35f
                        + Mathf.Sin(2f * Mathf.PI * 2460f * t) * 0.25f) * Mathf.Exp(-n * 4.5f);
                    break;

                case SonidoSintetico.Esquiva:
                    // Silbido de viento rapido
                    float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);
                    float envolventeViento = Mathf.Sin(n * Mathf.PI);
                    muestra = (ruido * 0.32f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(320f, 130f, n) * t) * 0.28f) * envolventeViento;
                    break;

                case SonidoSintetico.AtaqueGiratorio:
                    float ruidoGiro = (float)(rng.NextDouble() * 2.0 - 1.0);
                    muestra = (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(260f, 760f, n) * t) * 0.45f + ruidoGiro * 0.25f) * Mathf.Sin(n * Mathf.PI);
                    break;

                case SonidoSintetico.Curacion:
                    float freqCur = n < 0.33f ? 523.25f : (n < 0.66f ? 659.25f : 783.99f);
                    muestra = (Mathf.Sin(2f * Mathf.PI * freqCur * t) * 0.55f + Mathf.Sin(2f * Mathf.PI * freqCur * 2f * t) * 0.2f) * (1f - n * 0.65f);
                    break;

                case SonidoSintetico.Rupia:
                    float freqRup = n < 0.4f ? 987.77f : 1318.51f;
                    muestra = Mathf.Sin(2f * Mathf.PI * freqRup * t) * 0.5f * Mathf.Exp(-n * 4.0f);
                    break;

                case SonidoSintetico.Cofre:
                    float freqCofre = n < 0.25f ? 523.25f : (n < 0.5f ? 659.25f : (n < 0.75f ? 783.99f : 1046.5f));
                    muestra = (Mathf.Sin(2f * Mathf.PI * freqCofre * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * freqCofre * 1.5f * t) * 0.22f) * (1f - n * 0.5f);
                    break;

                case SonidoSintetico.RomperObjeto:
                    float crj = (float)(rng.NextDouble() * 2.0 - 1.0);
                    muestra = (crj * 0.65f + Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.3f) * Mathf.Exp(-n * 7.5f);
                    break;

                case SonidoSintetico.FijarObjetivo:
                    muestra = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(680f, 920f, n) * t) * 0.42f * env;
                    break;

                case SonidoSintetico.Aturdimiento:
                    muestra = Mathf.Sin(2f * Mathf.PI * (420f + Mathf.Sin(t * 45f) * 90f) * t) * 0.45f * env;
                    break;
            }

            datos[i] = Mathf.Clamp(muestra, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("SFX_" + tipo, muestras, 1, sr, false);
        clip.SetData(datos, 0);
        return clip;
    }

    private void OnDestroy()
    {
        foreach (AudioClip clip in cacheSinteticos.Values)
        {
            if (clip != null) Destroy(clip);
        }
        cacheSinteticos.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
