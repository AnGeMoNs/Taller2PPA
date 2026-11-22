using System;
using System.Collections.Generic;
using UnityEngine;

// Administra pociones de curacion, rupias, cofres abiertos y estadisticas de la aventura
public class InventarioJugador : MonoBehaviour
{
    public const int MaxPociones = 5;
    public const int PocionesIniciales = 1;
    private const float CooldownPocion = 1.0f;

    private VidaJugador vida;
    private float siguientePocion;
    private readonly HashSet<string> cofresAbiertos = new HashSet<string>();
    private readonly HashSet<string> salasDescubiertas = new HashSet<string>();

    public int Pociones { get; private set; } = PocionesIniciales;
    public int Rupias { get; private set; }
    public float TiempoJugado { get; private set; }
    public int EnemigosDerrotados { get; private set; }
    public int ParriesPerfectos { get; private set; }
    public int ComboMaximo { get; private set; }
    public int DanoRecibidoTotal { get; private set; }
    public int ObjetosRotos { get; private set; }

    public IReadOnlyCollection<string> CofresAbiertos => cofresAbiertos;
    public IReadOnlyCollection<string> SalasDescubiertas => salasDescubiertas;

    public event Action InventarioCambiado;
    public event Action EstadisticasCambiadas;

    private void Awake()
    {
        vida = GetComponent<VidaJugador>();
        salasDescubiertas.Add("Entrada");
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.EnJuego) return;

        TiempoJugado += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            UsarPocion();
        }
    }

    public bool UsarPocion()
    {
        if (vida == null || vida.Muerto || Time.time < siguientePocion) return false;

        if (Pociones <= 0)
        {
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 2.0f, "SIN POCIONES", new Color(0.95f, 0.45f, 0.35f), 0.9f);
            AudioManager.Reproducir("UI_Click", null, 0.45f, 0.75f);
            return false;
        }

        if (vida.Vida >= vida.VidaMaxima)
        {
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 2.0f, "VIDA AL MÁXIMO", new Color(0.55f, 0.92f, 0.65f), 0.9f);
            return false;
        }

        Pociones--;
        siguientePocion = Time.time + CooldownPocion;
        int curacion = Mathf.Max(40, Mathf.RoundToInt(vida.VidaMaxima * 0.5f));
        vida.Curar(curacion, true);
        AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Curacion, 0.72f);
        InventarioCambiado?.Invoke();
        return true;
    }

    public bool AgregarPocion(int cantidad = 1)
    {
        if (cantidad <= 0 || Pociones >= MaxPociones) return false;
        Pociones = Mathf.Min(MaxPociones, Pociones + cantidad);
        InventarioCambiado?.Invoke();
        return true;
    }

    public void AgregarRupias(int cantidad)
    {
        if (cantidad <= 0) return;
        Rupias += cantidad;
        InventarioCambiado?.Invoke();
    }

    public bool GastarRupias(int cantidad)
    {
        if (cantidad <= 0) return true;
        if (Rupias < cantidad) return false;
        Rupias -= cantidad;
        InventarioCambiado?.Invoke();
        return true;
    }

    public bool CofreAbierto(string id) => !string.IsNullOrEmpty(id) && cofresAbiertos.Contains(id);

    public void MarcarCofreAbierto(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (cofresAbiertos.Add(id))
        {
            InventarioCambiado?.Invoke();
        }
    }

    public bool SalaDescubierta(string id) => !string.IsNullOrEmpty(id) && salasDescubiertas.Contains(id);

    public bool RegistrarSalaDescubierta(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return salasDescubiertas.Add(id);
    }

    public void RegistrarEnemigoDerrotado()
    {
        EnemigosDerrotados++;
        EstadisticasCambiadas?.Invoke();
    }

    public void RegistrarParryPerfecto()
    {
        ParriesPerfectos++;
        EstadisticasCambiadas?.Invoke();
    }

    public void RegistrarCombo(int racha)
    {
        if (racha > ComboMaximo)
        {
            ComboMaximo = racha;
            EstadisticasCambiadas?.Invoke();
        }
    }

    public void RegistrarDanoRecibido(int dano)
    {
        if (dano <= 0) return;
        DanoRecibidoTotal += dano;
        EstadisticasCambiadas?.Invoke();
    }

    public void RegistrarObjetoRoto()
    {
        ObjetosRotos++;
        EstadisticasCambiadas?.Invoke();
    }

    public string CalcularRangoFinal()
    {
        int puntos = 100;
        if (TiempoJugado > 420f) puntos -= 25;
        else if (TiempoJugado > 240f) puntos -= 10;

        puntos -= Mathf.Min(35, DanoRecibidoTotal / 12);
        puntos += Mathf.Min(25, ParriesPerfectos * 6);
        puntos += Mathf.Min(15, ComboMaximo * 2);
        puntos += Mathf.Min(15, cofresAbiertos.Count * 3);

        if (puntos >= 100) return "S";
        if (puntos >= 80) return "A";
        if (puntos >= 55) return "B";
        return "C";
    }

    public void Escribir(DatosPartida datos)
    {
        if (datos == null) return;
        datos.pocionesInicializadas = true;
        datos.pociones = Pociones;
        datos.rupias = Rupias;
        datos.tiempoJugado = TiempoJugado;
        datos.enemigosDerrotados = EnemigosDerrotados;
        datos.parriesPerfectos = ParriesPerfectos;
        datos.comboMaximo = ComboMaximo;
        datos.danoRecibido = DanoRecibidoTotal;
        datos.objetosRotos = ObjetosRotos;
        datos.cofresAbiertos = new List<string>(cofresAbiertos);
        datos.salasDescubiertas = new List<string>(salasDescubiertas);
        datos.ultimaFechaGuardado = DateTime.Now.ToString("dd/MM HH:mm");
    }

    public void Restaurar(DatosPartida datos)
    {
        if (datos == null) return;

        Pociones = datos.pocionesInicializadas ? Mathf.Clamp(datos.pociones, 0, MaxPociones) : PocionesIniciales;
        Rupias = Mathf.Max(0, datos.rupias);
        TiempoJugado = Mathf.Max(0f, datos.tiempoJugado);
        EnemigosDerrotados = Mathf.Max(0, datos.enemigosDerrotados);
        ParriesPerfectos = Mathf.Max(0, datos.parriesPerfectos);
        ComboMaximo = Mathf.Max(0, datos.comboMaximo);
        DanoRecibidoTotal = Mathf.Max(0, datos.danoRecibido);
        ObjetosRotos = Mathf.Max(0, datos.objetosRotos);

        cofresAbiertos.Clear();
        if (datos.cofresAbiertos != null)
        {
            cofresAbiertos.UnionWith(datos.cofresAbiertos);
        }

        salasDescubiertas.Clear();
        salasDescubiertas.Add("Entrada");
        if (datos.salasDescubiertas != null)
        {
            salasDescubiertas.UnionWith(datos.salasDescubiertas);
        }

        InventarioCambiado?.Invoke();
        EstadisticasCambiadas?.Invoke();
    }
}
