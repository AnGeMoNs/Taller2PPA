using UnityEngine;

// Controla el rastro de la espada y el aura dinamica segun reliquia, contraataque de parry o carga giratoria
public class EquipoLinkVisual : MonoBehaviour
{
    [SerializeField] private TrailRenderer rastroEspada;
    [SerializeField] private Light luzEspada;

    private Movimiento3D movimiento;
    private CombateJugador combate;
    private bool potenciado;
    private int modoVisualActual = -1;

    public void Configurar(TrailRenderer rastro, Light luz)
    {
        rastroEspada = rastro;
        luzEspada = luz;
    }

    private void Awake()
    {
        movimiento = GetComponent<Movimiento3D>();
        combate = GetComponent<CombateJugador>();
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
        if (movimiento == null) return;

        bool atacando = movimiento.EstaAtacando;
        if (rastroEspada != null && rastroEspada.emitting != atacando)
        {
            rastroEspada.emitting = atacando;
        }

        bool contraataque = combate != null && combate.ContraataqueListo;
        bool cargandoGiro = movimiento.CargaGiroProgreso > 0.25f;
        int modoDeseado = contraataque ? 3 : (cargandoGiro ? 2 : (potenciado ? 1 : 0));

        if (modoDeseado != modoVisualActual)
        {
            modoVisualActual = modoDeseado;
            AplicarEstiloVisual(modoDeseado);
        }

        if (luzEspada != null && luzEspada.enabled)
        {
            float baseInt = modoDeseado == 3 ? 2.9f : (modoDeseado == 2 ? 2.4f : 2.0f);
            luzEspada.intensity = baseInt + Mathf.Sin(Time.time * 7.5f) * 0.35f;
        }
    }

    public void EstablecerPotenciado(bool activo)
    {
        if (potenciado == activo && modoVisualActual >= 0) return;
        potenciado = activo;
        modoVisualActual = -1;
    }

    private void AplicarEstiloVisual(int modo)
    {
        Color colorInicio;
        Color colorFin;
        Color colorLuz;
        bool encenderLuz;

        switch (modo)
        {
            case 3: // Contraataque tras Parry Perfecto
                colorInicio = new Color(1f, 0.96f, 0.45f, 1f);
                colorFin = new Color(1f, 0.58f, 0.10f, 0f);
                colorLuz = new Color(1f, 0.92f, 0.35f);
                encenderLuz = true;
                break;

            case 2: // Cargando ataque giratorio
                colorInicio = new Color(0.55f, 0.98f, 1f, 0.95f);
                colorFin = new Color(0.18f, 0.65f, 1f, 0f);
                colorLuz = new Color(0.45f, 0.92f, 1f);
                encenderLuz = true;
                break;

            case 1: // Reliquia Sagrada activa
                colorInicio = new Color(1f, 0.86f, 0.28f, 0.95f);
                colorFin = new Color(1f, 0.45f, 0.10f, 0f);
                colorLuz = new Color(1f, 0.84f, 0.30f);
                encenderLuz = true;
                break;

            default: // Normal
                colorInicio = new Color(0.65f, 0.92f, 1f, 0.85f);
                colorFin = new Color(0.25f, 0.65f, 1f, 0f);
                colorLuz = Color.white;
                encenderLuz = false;
                break;
        }

        if (luzEspada != null)
        {
            luzEspada.enabled = encenderLuz;
            luzEspada.color = colorLuz;
        }

        if (rastroEspada != null)
        {
            Gradient gradiente = new Gradient();
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
