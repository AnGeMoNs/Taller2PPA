using System.Collections.Generic;
using UnityEngine;

// Administra la musica de fondo y los efectos de sonido con cache en memoria
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;
    private readonly Dictionary<string, AudioClip> cacheEfectos = new Dictionary<string, AudioClip>();

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
        fuenteMusica.volume = 0.3f;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;
        fuenteEfectos.volume = 0.65f;

        fuenteMusica.clip = Resources.Load<AudioClip>("Audio/Music/Music_Dungeon_Loop");
        if (fuenteMusica.clip != null)
        {
            fuenteMusica.Play();
        }
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
