using UnityEngine;

// Tortuga con pinchos: recorre una ruta fija, se defiende con su caparazon y embiste al salir
public class TurtleEnemy : Enemy
{
    #region CLASS_VARIABLES

    [Header("Ruta de patrulla")]
    [SerializeField]
    private Vector3[] desplazamientosRuta =
    {
        new Vector3(0.0f, 0f, 0.0f),
        new Vector3(-4.0f, 0f, 0.0f),
        new Vector3(-4.0f, 0f, 2.2f),
        new Vector3(0.0f, 0f, 2.2f)
    };

    private int indiceRuta;
    private float pausaEnPuntoHasta;
    private float defensaHasta;
    private float ataqueVisualHasta;
    private float embestidaHasta;
    private Vector3 direccionEmbestida;
    private bool estabaDefendiendo;

    protected override Color ColorParticulasMuerte => new Color(0.86f, 0.68f, 0.28f);
    protected override float TiempoDesaparicion => 2.0f;
    public override string NombreVisible => EsElite && !string.IsNullOrEmpty(NombreElite) ? NombreElite : "Tortuga Blindada";
    public override string EstadoVisual => EstaAturdido ? "ATURDIDO" : (Time.time < defensaHasta ? "CAPARAZÓN (-50%)" : (Time.time < embestidaHasta ? "¡EMBESTIDA!" : ""));

    #endregion

    #region CLASS_METHODS

    public void ConfigurarRuta(Vector3[] offsetsLocales)
    {
        if (offsetsLocales != null && offsetsLocales.Length >= 2)
        {
            desplazamientosRuta = offsetsLocales;
        }
    }

    protected override float FactorVelocidadActual()
    {
        return Time.time < embestidaHasta ? 2.15f : 1f;
    }

    protected override void AlSerAturdido()
    {
        defensaHasta = 0f;
        embestidaHasta = 0f;
        estabaDefendiendo = false;
    }

    public override bool Patrullar()
    {
        if (!EnJuego || EstaAturdido)
            return false;

        if (Time.time < defensaHasta)
        {
            estabaDefendiendo = true;
            Detener();
            SetDefendiendo(true);
            return true;
        }

        if (estabaDefendiendo)
        {
            estabaDefendiendo = false;
            SetDefendiendo(false);

            if (jugador != null && DentroTerritorio && Distancia < 4.8f)
            {
                Vector3 hacia = jugador.transform.position - transform.position;
                hacia.y = 0f;
                if (hacia.sqrMagnitude > 0.04f)
                {
                    direccionEmbestida = hacia.normalized;
                    embestidaHasta = Time.time + 0.58f;
                    MostrarTelegrafoAtaque();
                }
            }
        }
        else
        {
            SetDefendiendo(false);
        }

        if (Time.time < embestidaHasta)
        {
            MoverEnDireccion(direccionEmbestida, true);
            return true;
        }

        if (Time.time < pausaEnPuntoHasta)
        {
            Detener();
            return true;
        }

        if (desplazamientosRuta == null || desplazamientosRuta.Length < 2)
        {
            Ruta(2.0f);
            return true;
        }

        Vector3 destino = Origen + desplazamientosRuta[indiceRuta];
        Vector3 posPlana = new Vector3(transform.position.x, Origen.y, transform.position.z);
        Vector3 destPlano = new Vector3(destino.x, Origen.y, destino.z);

        if (BloqueoRuta > 0.9f || Vector3.Distance(posPlana, destPlano) < 0.30f)
        {
            indiceRuta = (indiceRuta + 1) % desplazamientosRuta.Length;
            pausaEnPuntoHasta = Time.time + 0.65f;
            ReiniciarBloqueo();
            Detener();
            return true;
        }

        MoverHacia(destino, Time.time >= ataqueVisualHasta);
        return true;
    }

    protected override int ModificarDanoRecibido(int cantidad)
    {
        if (Time.time < defensaHasta && !EstaAturdido)
        {
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 2.15f, "CAPARAZÓN", new Color(0.58f, 0.85f, 1f), 0.85f);
            return Mathf.Max(1, Mathf.RoundToInt(cantidad * 0.5f));
        }

        return cantidad;
    }

    protected override void AlRecibirDano(int danoAplicado)
    {
        if (EstaAturdido)
        {
            ActivarGolpe();
            return;
        }

        if (Time.time >= defensaHasta)
        {
            defensaHasta = Time.time + 1.25f;
            SetDefendiendo(true);
        }
        else
        {
            ActivarGolpe();
        }
    }

    protected override void Contacto()
    {
        if (GolpeDeContacto("Turtle_Contact"))
        {
            ataqueVisualHasta = Time.time + 0.65f;
        }
    }

    #endregion
}
