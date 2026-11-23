using System.Collections.Generic;
using UnityEngine;

// Clona y activa los 5 enemigos de la oleada final al reclamar la reliquia
public sealed class OleadaFinal
{
    #region CLASS_VARIABLES

    private readonly GameManager game;
    private readonly Transform contenedor;
    private readonly List<GameObject> plantillas = new List<GameObject>();
    private readonly List<Enemy> enemigosInvocados = new List<Enemy>();

    private static readonly Vector3[] PuntosAparicion =
    {
        new Vector3(-4.4f, 0.025f, 24f),
        new Vector3( 4.4f, 0.025f, 24f),
        new Vector3(-1.4f, 0.025f, 28f),
        new Vector3( 1.4f, 0.025f, 28f),
        new Vector3( 0.0f, 0.025f, 20f)
    };

    public static readonly string[] IdsOleada =
    {
        "Oleada_Esqueleto_1",
        "Oleada_Esqueleto_2",
        "Oleada_Slime_1",
        "Oleada_Slime_2",
        "Oleada_Tortuga_1"
    };

    private bool activada;

    public bool Activada => activada;
    public bool Disponible => plantillas.Count == IdsOleada.Length;
    public bool Completa => activada && Restantes == 0;
    public int TotalOleada => IdsOleada.Length;

    public int Restantes
    {
        get
        {
            int pendientes = 0;
            for (int i = 0; i < IdsOleada.Length; i++)
            {
                if (!game.EnemigoEliminado(IdsOleada[i])) pendientes++;
            }
            return pendientes;
        }
    }

    public int VidaTotalActual
    {
        get
        {
            int suma = 0;
            for (int i = 0; i < enemigosInvocados.Count; i++)
            {
                Enemy e = enemigosInvocados[i];
                if (e != null && !e.Muerto) suma += e.VidaActual;
            }
            return suma;
        }
    }

    public int VidaTotalMaxima
    {
        get
        {
            int suma = 0;
            for (int i = 0; i < enemigosInvocados.Count; i++)
            {
                Enemy e = enemigosInvocados[i];
                if (e != null) suma += e.VidaMaxima;
            }
            return Mathf.Max(1, suma);
        }
    }

    #endregion

    #region CLASS_METHODS

    public OleadaFinal(Transform parent, GameManager manager)
    {
        game = manager;
        contenedor = new GameObject("Oleada_final").transform;
        contenedor.SetParent(parent, false);

        GameObject deposito = new GameObject("Plantillas_inactivas");
        deposito.SetActive(false);
        deposito.transform.SetParent(contenedor, false);

        Enemy[] existentes = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        SkeletonEnemy esqueleto = null;
        SlimeEnemy slime = null;
        TurtleEnemy tortuga = null;

        for (int i = 0; i < existentes.Length; i++)
        {
            Enemy enemigo = existentes[i];
            if (enemigo is SkeletonEnemy s && (esqueleto == null || s.Id == "Esqueleto_guardian_final"))
            {
                esqueleto = s;
            }
            if (enemigo is SlimeEnemy l && (slime == null || l.Id == "Slime_guardian_final"))
            {
                slime = l;
            }
            if (enemigo is TurtleEnemy t)
            {
                tortuga = t;
            }
        }

        if (esqueleto == null || slime == null || tortuga == null)
        {
            Debug.LogError("Oleada final: faltan enemigos base en la escena.");
            return;
        }

        Enemy[] fuentes = { esqueleto, esqueleto, slime, slime, tortuga };
        for (int i = 0; i < fuentes.Length; i++)
        {
            GameObject copia = Object.Instantiate(fuentes[i].gameObject, deposito.transform);
            copia.SetActive(false);
            plantillas.Add(copia);
        }
    }

    public void Activar()
    {
        if (activada || !Disponible)
            return;

        activada = true;
        enemigosInvocados.Clear();

        for (int i = 0; i < IdsOleada.Length; i++)
        {
            if (game.EnemigoEliminado(IdsOleada[i]))
                continue;

            GameObject nuevo = Object.Instantiate(plantillas[i], PuntosAparicion[i], Quaternion.identity, contenedor);
            nuevo.name = IdsOleada[i];
            nuevo.transform.localScale = Vector3.one;

            Enemy enemigo = nuevo.GetComponent<Enemy>();
            nuevo.SetActive(true);

            // El primer esqueleto aparece como lider elite de la oleada
            if (i == 0 && enemigo != null)
            {
                enemigo.ConfigurarElite("Capitán Esqueleto", 1.65f, 1.25f, 1.22f);
            }

            if (enemigo != null)
            {
                enemigosInvocados.Add(enemigo);
                game.RegistrarEnemigoNuevo(enemigo);
            }

            EfectosVisuales.PortalInvocacion(PuntosAparicion[i]);
        }

        Physics.SyncTransforms();
    }

    #endregion
}
