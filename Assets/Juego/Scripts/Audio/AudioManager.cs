using System.Collections.Generic;
using UnityEngine;

// Manager de musica con crossfade y efectos de sonido descargados con cache y pool 3D
public class AudioManager : MonoBehaviour
{
    #region CLASS_VARIABLES

    public static AudioManager Instance { get; private set; }

    public enum EfectoExtra
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

    private enum FaseMusica
    {
        Mazmorra,
        Jefe,
        SinEnemigos
    }

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

    private FaseMusica faseJuego = FaseMusica.Mazmorra;
    private string musicaActual;
    private float progresoFade = 1f;
    private float volumenMusica = 0.40f;
    private float volumenEfectos = 0.72f;

    public static float VolumenMusica => Instance != null ? Instance.volumenMusica : PlayerPrefs.GetFloat(PrefVolMusica, 0.40f);
    public static float VolumenEfectos => Instance != null ? Instance.volumenEfectos : PlayerPrefs.GetFloat(PrefVolEfectos, 0.72f);

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

        // Pool de fuentes 3D para evitar crear objetos temporales con cada sonido
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

    private void Update()
    {
        if (progresoFade >= 1f)
            return;

        progresoFade = Mathf.Min(1f, progresoFade + Time.unscaledDeltaTime / DuracionCrossfade);

        if (fuenteActiva != null)
        {
            fuenteActiva.volume = volumenMusica * progresoFade;
        }

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region CLASS_METHODS

    private AudioSource CrearFuenteMusica()
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }

    public static void ConfigurarVolumenMusica(float valor)
    {
        float v = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(PrefVolMusica, v);
        PlayerPrefs.Save();

        if (Instance == null)
            return;

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

        if (Instance == null)
            return;

        Instance.volumenEfectos = v;
        if (Instance.fuenteEfectos2D != null)
        {
            Instance.fuenteEfectos2D.volume = v;
        }
    }

    public static void ActualizarEstadoJuego(GameStates estado)
    {
        if (Instance == null)
            return;

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
        if (Instance == null)
            return;
        if (Instance.faseJuego == FaseMusica.Jefe || Instance.faseJuego == FaseMusica.SinEnemigos)
            return;

        Instance.faseJuego = FaseMusica.Jefe;
        if (GameManager.Instance != null && GameManager.Instance.EnJuego)
        {
            ReproducirMusica("MusicaJefe");
        }
    }

    public static void ActivarMusicaSinEnemigos()
    {
        if (Instance == null)
            return;
        if (Instance.faseJuego == FaseMusica.SinEnemigos)
            return;

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
        if (Instance == null || string.IsNullOrEmpty(nombre))
            return;

        if (Instance.musicaActual == nombre && Instance.fuenteActiva != null && Instance.fuenteActiva.isPlaying)
            return;

        if (!Instance.cacheMusica.TryGetValue(nombre, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>("Audio/Music/" + nombre);
            Instance.cacheMusica[nombre] = clip;
        }

        if (clip == null)
            return;

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
        if (Instance == null)
            return;

        Instance.musicaActual = null;
        Instance.progresoFade = 1f;

        if (Instance.musicaA != null)
        {
            Instance.musicaA.Stop();
            Instance.musicaA.clip = null;
        }
        if (Instance.musicaB != null)
        {
            Instance.musicaB.Stop();
            Instance.musicaB.clip = null;
        }
    }

    public static void Reproducir(string nombre, Vector3? posicion = null, float escalaVolumen = 1f, float pitchBase = 1f, float variacionPitch = 0f)
    {
        if (Instance == null || string.IsNullOrEmpty(nombre))
            return;

        if (!Instance.cacheEfectos.TryGetValue(nombre, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>("Audio/SFX/" + nombre);
            Instance.cacheEfectos[nombre] = clip;
        }

        if (clip == null)
            return;

        Instance.ReproducirClip(clip, posicion, escalaVolumen, pitchBase, variacionPitch);
    }

    public static void ReproducirEfecto(EfectoExtra tipo, float escalaVolumen = 0.75f, Vector3? posicion = null)
    {
        Reproducir("SFX_" + tipo, posicion, escalaVolumen, 1f, 0.04f);
    }

    private void ReproducirClip(AudioClip clip, Vector3? posicion, float escalaVolumen, float pitchBase, float variacionPitch)
    {
        float volFinal = Mathf.Clamp01(volumenEfectos * escalaVolumen);
        if (volFinal <= 0.001f)
            return;

        float pitch = pitchBase;
        if (variacionPitch > 0f)
        {
            pitch += Random.Range(-variacionPitch, variacionPitch);
        }
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

    #endregion
}
