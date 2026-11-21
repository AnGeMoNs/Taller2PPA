using UnityEngine;

// Tortuga con pinchos: sigue una ruta definida de forma lenta, dana al contacto y se defiende con su caparazon
public class TurtleEnemy : Enemy
{
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

    protected override Color ColorParticulasMuerte => new Color(0.86f, 0.68f, 0.28f);
    protected override float TiempoDesaparicion => 2.0f;

    public void ConfigurarRuta(Vector3[] offsetsLocales)
    {
        if (offsetsLocales != null && offsetsLocales.Length >= 2)
        {
            desplazamientosRuta = offsetsLocales;
        }
    }

    public override bool Patrullar()
    {
        if (!EnJuego) return false;

        if (Time.time < defensaHasta)
        {
            Detener();
            SetDefendiendo(true);
            return true;
        }

        SetDefendiendo(false);

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
        float distancia = Vector3.Distance(
            new Vector3(transform.position.x, Origen.y, transform.position.z),
            new Vector3(destino.x, Origen.y, destino.z));

        if (BloqueoRuta > 0.9f || distancia < 0.30f)
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
        return Time.time < defensaHasta ? Mathf.Max(1, Mathf.RoundToInt(cantidad * 0.5f)) : cantidad;
    }

    protected override void AlRecibirDano(int danoAplicado)
    {
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
}