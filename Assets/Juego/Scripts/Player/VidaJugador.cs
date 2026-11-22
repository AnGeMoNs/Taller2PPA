using System;
using UnityEngine;

// Administra salud del jugador, curacion, esquiva con i-frames, parry perfecto, bloqueo con escudo y muerte
public class VidaJugador : MonoBehaviour
{
    [SerializeField] private int vidaMaxima = 100;
    [SerializeField] private float invulnerabilidad = 0.65f;

    private float tiempoSiguienteDano;
    private Movimiento3D movimiento;
    private CombateJugador combate;
    private InventarioJugador inventario;

    public int Vida { get; private set; }
    public int VidaMaxima { get; private set; }
    public const int VidaConReliquia = 300;
    public bool Muerto => Vida <= 0;

    public event Action<int, int> VidaCambiada;
    public event Action<bool> DanoRecibido;
    public event Action<int> Curado;
    public event Action ParryEjecutado;
    public event Action Murio;

    private void Awake()
    {
        VidaMaxima = vidaMaxima;
        Vida = VidaMaxima;
        movimiento = GetComponent<Movimiento3D>();
        combate = GetComponent<CombateJugador>();
        inventario = GetComponent<InventarioJugador>();
    }

    public void PotenciarConReliquia()
    {
        VidaMaxima = Mathf.Max(vidaMaxima, VidaConReliquia);
        Restaurar(VidaMaxima);
        tiempoSiguienteDano = Time.time + 2.0f;
        EfectosVisuales.Curacion(transform.position);
    }

    public void RestaurarMaxima(bool tieneReliquia, int valor)
    {
        VidaMaxima = tieneReliquia ? Mathf.Max(vidaMaxima, VidaConReliquia) : vidaMaxima;
        Restaurar(valor);
    }

    public void Restaurar(int valor)
    {
        Vida = Mathf.Clamp(valor, 1, VidaMaxima);
        tiempoSiguienteDano = 0f;
        VidaCambiada?.Invoke(Vida, VidaMaxima);
    }

    public void Curar(int cantidad, bool mostrarEfecto = true)
    {
        if (Muerto || cantidad <= 0) return;

        int vidaAnterior = Vida;
        Vida = Mathf.Min(VidaMaxima, Vida + cantidad);
        int curadoReal = Vida - vidaAnterior;

        VidaCambiada?.Invoke(Vida, VidaMaxima);
        if (curadoReal > 0)
        {
            Curado?.Invoke(curadoReal);
        }

        if (mostrarEfecto)
        {
            EfectosVisuales.Curacion(transform.position);
            string texto = curadoReal > 0 ? $"+{curadoReal} HP" : "HP MÁX";
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 2.0f, texto, new Color(0.32f, 0.96f, 0.56f), 1.0f);
        }
    }

    public void RecibirDano(int cantidad) => RecibirDano(cantidad, null, null);

    public void RecibirDano(int cantidad, Enemy atacante, Vector3? origenDano)
    {
        if (Muerto || cantidad <= 0 || Time.time < tiempoSiguienteDano) return;

        if (inventario == null) inventario = GetComponent<InventarioJugador>();
        if (combate == null) combate = GetComponent<CombateJugador>();

        // 1. Invulnerabilidad durante esquiva rapida (i-frames)
        if (movimiento != null && movimiento.EstaEsquivando)
        {
            tiempoSiguienteDano = Time.time + 0.35f;
            movimiento.RecuperarEstamina(12f);
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 2.0f, "ESQUIVADO", new Color(0.55f, 0.92f, 1f), 0.95f);
            return;
        }

        // 2. Parry Perfecto: bloquear justo antes del impacto de un enemigo anula el dano, lo aturde y potencia el contraataque
        if (atacante != null && movimiento != null && movimiento.EsParryPerfecto(origenDano))
        {
            tiempoSiguienteDano = Time.time + 0.50f;
            movimiento.RecuperarEstamina(28f);
            atacante?.Aturdir(2.35f, transform.position);
            combate?.ActivarContraataque();
            combate?.SolicitarHitStop(0.065f);
            inventario?.RegistrarParryPerfecto();

            EfectosVisuales.OndaParry(transform.position + Vector3.up * 1.05f + transform.forward * 0.45f);
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 2.1f, "¡PARRY!", new Color(1f, 0.90f, 0.25f), 1.25f);
            AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Parry, 0.88f);
            CamaraTerceraPersona.Sacudir(0.11f, 0.14f);
            DanoRecibido?.Invoke(true);
            ParryEjecutado?.Invoke();
            return;
        }

        // 3. Bloqueo normal con escudo: reduce el dano un 75 por ciento si queda estamina
        bool bloqueado = movimiento != null && movimiento.IsBlocking && movimiento.ConsumirEstamina(12f);
        int danoFinal = bloqueado ? Mathf.Max(1, Mathf.RoundToInt(cantidad * 0.25f)) : cantidad;

        tiempoSiguienteDano = Time.time + invulnerabilidad;
        Vida = Mathf.Max(0, Vida - danoFinal);
        inventario?.RegistrarDanoRecibido(danoFinal);
        VidaCambiada?.Invoke(Vida, VidaMaxima);
        DanoRecibido?.Invoke(bloqueado);
        CamaraTerceraPersona.Sacudir(bloqueado ? 0.06f : 0.16f, bloqueado ? 0.1f : 0.18f);

        Vector3 puntoImpacto = transform.position + Vector3.up * 1.1f;
        EfectosVisuales.ChispasGolpe(puntoImpacto, false, bloqueado);

        if (origenDano.HasValue && movimiento != null)
        {
            Vector3 empuje = transform.position - origenDano.Value;
            movimiento.AplicarEmpuje(empuje, bloqueado ? 2.4f : 4.2f);
        }

        if (bloqueado)
        {
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 2.0f, $"-{danoFinal} ESCUDO", new Color(0.50f, 0.85f, 1f), 0.9f);
        }
        else
        {
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 2.0f, $"-{danoFinal}", new Color(1f, 0.28f, 0.24f), 1.0f);
        }

        if (Muerto)
        {
            movimiento?.ReproducirMuerte();
            AudioManager.Reproducir("Player_Death");
            Murio?.Invoke();
        }
        else
        {
            if (!bloqueado) movimiento?.ReproducirGolpe();
            AudioManager.Reproducir(bloqueado ? "UI_Click" : "Player_Hurt", null, bloqueado ? 0.75f : 0.95f, bloqueado ? 1.25f : 1f, 0.06f);
        }
    }
}
