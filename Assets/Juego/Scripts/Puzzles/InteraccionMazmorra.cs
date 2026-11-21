using UnityEngine;

public enum TipoInteraccion { Recompensa, Palanca, ResetCajas, ControlTrampa, Pedestal, Reliquia }

// Marca un objeto interactuable con la tecla E dentro de la mazmorra
public class InteraccionMazmorra : MonoBehaviour
{
    [SerializeField] private TipoInteraccion tipo;
    [SerializeField] private int indice;
    [SerializeField] private string texto;

    public TipoInteraccion Tipo => tipo;
    public int Indice => indice;
    public string Texto => texto;

    public void Configurar(TipoInteraccion nuevoTipo, int nuevoIndice, string etiqueta)
    {
        tipo = nuevoTipo;
        indice = nuevoIndice;
        texto = etiqueta;
    }
}
