using UnityEngine;

// Interfaz comun para objetos interactuables del mundo con la tecla E
public interface IInteractuable
{
    Transform Transform { get; }
    string TextoInteraccion { get; }
    bool Disponible { get; }
    void Interactuar(VidaJugador jugador, InventarioJugador inventario, HUDManager hud);
}
