using System;
using UnityEngine;

// Administra la salud del jugador, invulnerabilidad breve, reduccion por escudo y muerte
public class VidaJugador : MonoBehaviour
{
    [SerializeField] private int vidaMaxima = 100;
    [SerializeField] private float invulnerabilidad = 0.65f;

    private float tiempoSiguienteDano;
    private Movimiento3D movimiento;

    public int Vida { get; private set; }
    public int VidaMaxima { get; private set; }
    public const int VidaConReliquia = 300;
    public bool Muerto => Vida <= 0;

    public event Action<int, int> VidaCambiada;
    public event Action<bool> DanoRecibido;
    public event Action Murio;

    private void Awake()
    {
        VidaMaxima = vidaMaxima;
        Vida = VidaMaxima;
        movimiento = GetComponent<Movimiento3D>();
    }

    public void PotenciarConReliquia()
    {
        VidaMaxima = Mathf.Max(vidaMaxima, VidaConReliquia);
        Restaurar(VidaMaxima);
        tiempoSiguienteDano = Time.time + 1.8f;
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

    public void RecibirDano(int cantidad)
    {
        if (Muerto || cantidad <= 0 || Time.time < tiempoSiguienteDano) return;

        // Si el jugador esta cubriendose con el escudo, reduce el dano en un 75 por ciento
        bool bloqueado = movimiento != null && movimiento.IsBlocking;
        int danoFinal = bloqueado ? Mathf.Max(1, Mathf.RoundToInt(cantidad * 0.25f)) : cantidad;

        tiempoSiguienteDano = Time.time + invulnerabilidad;
        Vida = Mathf.Max(0, Vida - danoFinal);
        VidaCambiada?.Invoke(Vida, VidaMaxima);
        DanoRecibido?.Invoke(bloqueado);
        CamaraTerceraPersona.Sacudir(bloqueado ? 0.06f : 0.16f, bloqueado ? 0.1f : 0.18f);

        if (Muerto)
        {
            movimiento?.ReproducirMuerte();
            AudioManager.Reproducir("Player_Death");
            Murio?.Invoke();
        }
        else
        {
            if (!bloqueado) movimiento?.ReproducirGolpe();
            AudioManager.Reproducir(bloqueado ? "UI_Click" : "Player_Hurt");
        }
    }
}
