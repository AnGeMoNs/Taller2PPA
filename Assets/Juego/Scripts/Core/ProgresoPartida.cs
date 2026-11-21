using System;
using System.Collections.Generic;
using UnityEngine;

// Guarda en memoria las salas resueltas y los sellos recogidos durante la partida
public class ProgresoPartida : MonoBehaviour
{
    private readonly List<string> salasResueltas = new List<string>();
    private readonly List<string> objetosObtenidos = new List<string>();

    public IReadOnlyList<string> Salas => salasResueltas;
    public IReadOnlyList<string> Objetos => objetosObtenidos;
    public event Action Cambio;

    public bool SalaResuelta(string id) => salasResueltas.Contains(id);
    public bool TieneObjeto(string id) => objetosObtenidos.Contains(id);

    public void ResolverSala(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || salasResueltas.Contains(id)) return;
        salasResueltas.Add(id);
        Cambio?.Invoke();
        AudioManager.Reproducir("Puzzle_Complete");
    }

    public void Recoger(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || objetosObtenidos.Contains(id)) return;
        objetosObtenidos.Add(id);
        Cambio?.Invoke();
        AudioManager.Reproducir("Item_Pickup");
    }

    public void Restaurar(IEnumerable<string> salasGuardadas, IEnumerable<string> objetosGuardados)
    {
        salasResueltas.Clear();
        objetosObtenidos.Clear();

        if (salasGuardadas != null)
        {
            foreach (string id in salasGuardadas)
            {
                if (!salasResueltas.Contains(id)) salasResueltas.Add(id);
            }
        }

        if (objetosGuardados != null)
        {
            foreach (string id in objetosGuardados)
            {
                if (!objetosObtenidos.Contains(id)) objetosObtenidos.Add(id);
            }
        }

        Cambio?.Invoke();
    }
}
