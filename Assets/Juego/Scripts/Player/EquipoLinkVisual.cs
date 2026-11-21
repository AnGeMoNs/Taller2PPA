using UnityEngine;

// Controla el rastro de la espada al atacar y el aura dorada al conseguir la reliquia
public class EquipoLinkVisual : MonoBehaviour
{
    [SerializeField] private TrailRenderer rastroEspada;
    [SerializeField] private Light luzEspada;

    private Movimiento3D movimiento;
    private bool potenciado;

    public void Configurar(TrailRenderer rastro, Light luz)
    {
        rastroEspada = rastro;
        luzEspada = luz;
    }

    private void Awake()
    {
        movimiento = GetComponent<Movimiento3D>();
        if (rastroEspada == null) rastroEspada = GetComponentInChildren<TrailRenderer>(true);
        if (luzEspada == null)
        {
            Transform nodoLuz = BuscarHijo(transform, "Luz_Espada");
            if (nodoLuz != null) luzEspada = nodoLuz.GetComponent<Light>();
        }

        if (rastroEspada != null) rastroEspada.emitting = false;
        if (luzEspada != null) luzEspada.enabled = false;
    }

    private void Update()
    {
        if (movimiento == null || rastroEspada == null) return;
        bool atacando = movimiento.EstaAtacando;
        if (rastroEspada.emitting != atacando)
        {
            rastroEspada.emitting = atacando;
        }
    }

    public void EstablecerPotenciado(bool activo)
    {
        if (potenciado == activo && (luzEspada == null || luzEspada.enabled == activo)) return;
        potenciado = activo;

        if (luzEspada != null)
        {
            luzEspada.enabled = potenciado;
            luzEspada.color = new Color(1f, 0.84f, 0.30f);
            luzEspada.intensity = 2.0f;
        }

        if (rastroEspada != null)
        {
            Gradient gradiente = new Gradient();
            Color colorInicio = potenciado ? new Color(1f, 0.86f, 0.28f, 0.95f) : new Color(0.65f, 0.92f, 1f, 0.85f);
            Color colorFin = potenciado ? new Color(1f, 0.45f, 0.10f, 0f) : new Color(0.25f, 0.65f, 1f, 0f);
            gradiente.SetKeys(
                new[] { new GradientColorKey(colorInicio, 0f), new GradientColorKey(colorFin, 1f) },
                new[] { new GradientAlphaKey(colorInicio.a, 0f), new GradientAlphaKey(0f, 1f) });
            rastroEspada.colorGradient = gradiente;
        }
    }

    private static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform hijo in padre.GetComponentsInChildren<Transform>(true))
        {
            if (hijo.name == nombre) return hijo;
        }
        return null;
    }
}
