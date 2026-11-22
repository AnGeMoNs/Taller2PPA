using UnityEngine;

// Contrato comun para elementos interactuables adicionales del mundo (cofres, santuarios, etc.)
public interface IInteractuable
{
    Transform Transform { get; }
    string TextoInteraccion { get; }
    bool Disponible { get; }
    void Interactuar(VidaJugador jugador, InventarioJugador inventario, HUDManager hud);
}
