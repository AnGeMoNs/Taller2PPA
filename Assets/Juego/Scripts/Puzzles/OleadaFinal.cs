using System.Collections.Generic;
using UnityEngine;

// Clona y activa los enemigos de la oleada final al reclamar la reliquia sagrada
public sealed class OleadaFinal
{
    private readonly GameManager game;
    private readonly Transform contenedor;
    private readonly List<GameObject> plantillas = new List<GameObject>();

    private static readonly Vector3[] PuntosAparicion =
    {
        new Vector3(-4.4f, 0.025f, 24f),
        new Vector3( 4.4f, 0.025f, 24f),
        new Vector3(-1.4f, 0.025f, 28f),
        new Vector3( 1.4f, 0.025f, 28f),
        new Vector3( 0.0f, 0.025f, 20f)
    };

    private static readonly string[] IdsOleada =
    {
        "Oleada_Esqueleto_1",
        "Oleada_Esqueleto_2",
        "Oleada_Slime_1",
        "Oleada_Slime_2",
        "Oleada_Tortuga_1"
    };

    private bool activada;

    public bool Disponible => plantillas.Count == IdsOleada.Length;
    public bool Completa => activada && Restantes == 0;

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

        foreach (Enemy enemigo in existentes)
        {
            if (enemigo is SkeletonEnemy s && (esqueleto == null || s.Id == "Esqueleto_guardian_final")) esqueleto = s;
            if (enemigo is SlimeEnemy l && (slime == null || l.Id == "Slime_guardian_final")) slime = l;
            if (enemigo is TurtleEnemy t) tortuga = t;
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
        if (activada || !Disponible) return;
        activada = true;

        for (int i = 0; i < IdsOleada.Length; i++)
        {
            if (game.EnemigoEliminado(IdsOleada[i])) continue;

            GameObject nuevo = Object.Instantiate(plantillas[i], PuntosAparicion[i], Quaternion.identity, contenedor);
            nuevo.name = IdsOleada[i];
            nuevo.transform.localScale = Vector3.one;
            Enemy enemigo = nuevo.GetComponent<Enemy>();
            game.RegistrarEnemigoNuevo(enemigo);
            nuevo.SetActive(true);
        }

        Physics.SyncTransforms();
    }
}
