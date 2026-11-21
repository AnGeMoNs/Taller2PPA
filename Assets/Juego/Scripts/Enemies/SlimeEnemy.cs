using UnityEngine;

// Slime: se mueve de forma lenta y erratica en zig-zag hacia el jugador y dana al contacto
public class SlimeEnemy : Enemy
{
    private float faseZigZag;
    private float frecuenciaZigZag;
    private float siguienteCambioDireccion;
    private float pausaHasta;
    private float ataqueVisualHasta;
    private Vector3 direccionErratica;

    protected override Color ColorParticulasMuerte => new Color(0.28f, 0.88f, 0.42f);
    protected override float TiempoDesaparicion => 1.45f;

    protected override void Awake()
    {
        base.Awake();
        faseZigZag = transform.position.x * 1.7f + transform.position.z * 2.3f;
        frecuenciaZigZag = 2.6f;
        ElegirDireccionErratica();
    }

    // Alterna velocidad lentamente simulando saltos gelatinosos
    protected override float FactorVelocidadActual()
    {
        return 0.65f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * 3.1f + faseZigZag));
    }

    public override bool IntentarPerseguir()
    {
        if (!VeJugador()) return false;

        Vector3 haciaJugador = jugador.transform.position - transform.position;
        haciaJugador.y = 0f;
        if (haciaJugador.sqrMagnitude < 0.001f) return true;

        float anguloDesvio = Mathf.Sin(Time.time * frecuenciaZigZag + faseZigZag) * 38f;
        Vector3 dirZigZag = Quaternion.Euler(0f, anguloDesvio, 0f) * haciaJugador.normalized;
        MoverEnDireccion(dirZigZag, Time.time >= ataqueVisualHasta);
        return true;
    }

    public override bool Patrullar()
    {
        if (!EnJuego) return false;

        if (Time.time < pausaHasta)
        {
            Detener();
            if (Time.time >= ataqueVisualHasta) Animar("Idle");
            return true;
        }

        if (Time.time >= siguienteCambioDireccion || BloqueoRuta > 0.5f)
        {
            ReiniciarBloqueo();
            if (Random.value < 0.30f)
            {
                pausaHasta = Time.time + Random.Range(0.55f, 1.15f);
                Detener();
                Animar("Idle");
                return true;
            }
            ElegirDireccionErratica();
        }

        // Regresa hacia su zona si se aleja demasiado
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
}
