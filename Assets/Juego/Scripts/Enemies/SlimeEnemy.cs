using UnityEngine;

// Slime: avance lento y erratico en zig-zag con dano al contacto
public class SlimeEnemy : Enemy
{
    #region CLASS_VARIABLES

    private float faseZigZag;
    private float frecuenciaZigZag;
    private float siguienteCambioDireccion;
    private float pausaHasta;
    private float ataqueVisualHasta;
    private Vector3 direccionErratica;
    private Transform visualHijo;
    private Vector3 escalaVisualBase;

    protected override Color ColorParticulasMuerte => new Color(0.28f, 0.88f, 0.42f);
    protected override float TiempoDesaparicion => 1.45f;
    public override string NombreVisible => EsElite && !string.IsNullOrEmpty(NombreElite) ? NombreElite : "Slime de Mazmorra";

    #endregion

    #region UNITY_METHODS

    protected override void Awake()
    {
        base.Awake();
        faseZigZag = transform.position.x * 1.7f + transform.position.z * 2.3f;
        frecuenciaZigZag = 2.6f;
        ElegirDireccionErratica();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo.name != "BarraVida_3D" && hijo.name != "Aura_Elite")
            {
                visualHijo = hijo;
                escalaVisualBase = hijo.localScale;
                break;
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        if (Muerto || visualHijo == null || !EnJuego)
            return;

        // Pulso suave de escala para simular rebote elastico
        float pulso = Mathf.Sin(Time.time * 6.2f + faseZigZag);
        float factorY = 1f + pulso * 0.08f;
        float factorXZ = 1f - pulso * 0.05f;
        visualHijo.localScale = new Vector3(escalaVisualBase.x * factorXZ, escalaVisualBase.y * factorY, escalaVisualBase.z * factorXZ);
    }

    #endregion

    #region CLASS_METHODS

    protected override float FactorVelocidadActual()
    {
        return 0.65f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * 3.1f + faseZigZag));
    }

    public override bool IntentarPerseguir()
    {
        if (EstaAturdido || !VeJugador())
            return false;

        Vector3 haciaJugador = jugador.transform.position - transform.position;
        haciaJugador.y = 0f;
        if (haciaJugador.sqrMagnitude < 0.001f)
            return true;

        float anguloDesvio = Mathf.Sin(Time.time * frecuenciaZigZag + faseZigZag) * 38f;
        Vector3 dirZigZag = Quaternion.Euler(0f, anguloDesvio, 0f) * haciaJugador.normalized;
        MoverEnDireccion(dirZigZag, Time.time >= ataqueVisualHasta);
        return true;
    }

    public override bool Patrullar()
    {
        if (!EnJuego || EstaAturdido)
            return false;

        if (Time.time < pausaHasta)
        {
            Detener();
            return true;
        }

        if (Time.time >= siguienteCambioDireccion || BloqueoRuta > 0.5f)
        {
            ReiniciarBloqueo();
            if (Random.value < 0.30f)
            {
                pausaHasta = Time.time + Random.Range(0.55f, 1.15f);
                Detener();
                return true;
            }
            ElegirDireccionErratica();
        }

        Vector3 desdeOrigen = transform.position - Origen;
        desdeOrigen.y = 0f;
        if (desdeOrigen.magnitude > RadioTerritorio * 0.65f)
        {
            direccionErratica = (-desdeOrigen).normalized;
        }

        MoverEnDireccion(direccionErratica, Time.time >= ataqueVisualHasta);
        return true;
    }

    private void ElegirDireccionErratica()
    {
        float angulo = Random.Range(0f, 360f);
        direccionErratica = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
        siguienteCambioDireccion = Time.time + Random.Range(1.0f, 2.2f);
    }

    protected override void Contacto()
    {
        if (GolpeDeContacto("Slime_Contact"))
        {
            ataqueVisualHasta = Time.time + 0.65f;
        }
    }

    #endregion
}
