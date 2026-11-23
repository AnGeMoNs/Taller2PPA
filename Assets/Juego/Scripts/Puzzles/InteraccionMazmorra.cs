using UnityEngine;

public enum TipoInteraccion
{
    Recompensa,
    Palanca,
    ResetCajas,
    ControlTrampa,
    Pedestal,
    Reliquia
}

// Identifica un objeto de puzzle interactuable con la tecla E
public class InteraccionMazmorra : MonoBehaviour
{
    #region CLASS_VARIABLES

    [SerializeField] private TipoInteraccion tipo;
    [SerializeField] private int indice;
    [SerializeField] private string texto;

    public TipoInteraccion Tipo => tipo;
    public int Indice => indice;
    public string Texto => texto;

    #endregion

    #region CLASS_METHODS

    public void Configurar(TipoInteraccion nuevoTipo, int nuevoIndice, string etiqueta)
    {
        tipo = nuevoTipo;
        indice = nuevoIndice;
        texto = etiqueta;
    }

    #endregion
}
