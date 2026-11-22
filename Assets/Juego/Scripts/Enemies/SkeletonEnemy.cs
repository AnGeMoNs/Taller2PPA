using UnityEngine;

// Esqueleto con espada: patrulla, telegrafia su golpe para permitir parry/esquiva y persigue al jugador
public class SkeletonEnemy : Enemy
{
    private float momentoImpacto = -1f;
    private float recordarHasta;
    private float pausaPatrullaHasta;
    private int puntoPatrulla;
    private Vector3 ultimaPosicionVista;

    protected override Color ColorParticulasMuerte => new Color(0.92f, 0.88f, 0.76f);
    protected override float TiempoDesaparicion => 1.55f;
    public override string NombreVisible => EsElite && !string.IsNullOrEmpty(NombreElite) ? NombreElite : "Esqueleto Guardián";
    public override string EstadoVisual => EstaAturdido ? "ATURDIDO" : (momentoImpacto >= 0f ? "¡ATACANDO!" : "");

    protected override void AlSerAturdido()
    {
        momentoImpacto = -1f;
    }

    public override bool IntentarAtacar()
    {
        if (!EnJuego || EstaAturdido) return false;

        if (momentoImpacto >= 0f)
        {
            Detener();
            MirarJugador();
            if (Time.time < momentoImpacto) return true;
            momentoImpacto = -1f;
            if (Distancia <= rangoAtaque + 0.32f && VeJugador())
            {
                jugador.RecibirDano(dano, this, transform.position);
            }
            return true;
        }

        if (Distancia > rangoAtaque || !VeJugador()) return false;

        Detener();
        MirarJugador();
        RegistrarAvistamiento();

        if (Time.time >= siguienteAtaque)
        {
            siguienteAtaque = Time.time + (EsElite ? 1.35f : 1.55f);
            momentoImpacto = Time.time + 0.42f;
            ActivarAtaque();
            MostrarTelegrafoAtaque();
            AudioManager.Reproducir("Skeleton_Attack", transform.position, 0.92f, EsElite ? 0.88f : 1f, 0.06f);
        }
        return true;
    }

    private void RegistrarAvistamiento()
    {
        if (!observado)
        {
            AudioManager.Reproducir("Skeleton_Alert", transform.position, 0.85f, 1f, 0.05f);
        }
        observado = true;
        recordarHasta = Time.time + 2.5f;
        ultimaPosicionVista = jugador.transform.position;
    }

    public override bool IntentarPerseguir()
    {
        if (EstaAturdido) return false;

        if (VeJugador())
        {
            RegistrarAvistamiento();
            MoverHacia(ultimaPosicionVista);
            return true;
        }

        if (DentroTerritorio && observado && Time.time < recordarHasta && Vector3.Distance(transform.position, ultimaPosicionVista) > 0.45f)
        {
            MoverHacia(ultimaPosicionVista);
            return true;
        }

        observado = false;
        return false;
    }

    public override bool Patrullar()
    {
        if (!EnJuego || EstaAturdido) return false;
        observado = false;

        if (Time.time < pausaPatrullaHasta)
        {
            Detener();
            return true;
        }

        const float radio = 1.9f;
        float x = (puntoPatrulla == 0 || puntoPatrulla == 3) ? -radio : radio;
        float z = puntoPatrulla < 2 ? radio : -radio;
        Vector3 destino = Origen + new Vector3(x, 0f, z);

        if (BloqueoRuta > 0.85f || Vector3.Distance(new Vector3(transform.position.x, Origen.y, transform.position.z), destino) < 0.30f)
        {
            puntoPatrulla = (puntoPatrulla + 1) % 4;
            pausaPatrullaHasta = Time.time + 0.85f;
            ReiniciarBloqueo();
            Detener();
            return true;
        }

        MoverHacia(destino);
        return true;
    }
}
