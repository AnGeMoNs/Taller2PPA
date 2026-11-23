using System;
using System.Collections.Generic;
using UnityEngine;

// Agrega cofres, objetos rompibles y el santuario al iniciar la escena
public class MejorasMazmorra : MonoBehaviour
{
    #region CLASS_VARIABLES

    private readonly List<IInteractuable> interactuables = new List<IInteractuable>();
    private readonly List<CofreTesoro> cofres = new List<CofreTesoro>();
    private SantuarioCuracion santuario;
    private bool construido;

    public IReadOnlyList<IInteractuable> Interactuables => interactuables;
    public IReadOnlyList<CofreTesoro> Cofres => cofres;
    public SantuarioCuracion Santuario => santuario;

    #endregion

    #region CLASS_METHODS

    public void Construir()
    {
        if (construido)
            return;

        construido = true;

        Transform contenedor = new GameObject("Contenido_Extra_Mazmorra").transform;
        contenedor.SetParent(transform, false);

        ConvertirDecoracionesExistentes();
        CrearCofresAdicionales(contenedor);
        CrearSantuario(contenedor);
    }

    private void ConvertirDecoracionesExistentes()
    {
        Transform[] todos = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < todos.Length; i++)
        {
            Transform t = todos[i];
            if (t == null)
                continue;

            string n = t.name;

            if (n.StartsWith("Barril_", StringComparison.OrdinalIgnoreCase))
            {
                if (t.GetComponent<ObjetoRompible>() == null)
                {
                    ObjetoRompible rom = t.gameObject.AddComponent<ObjetoRompible>();
                    rom.Configurar(1, new Color(0.58f, 0.36f, 0.18f));
                }
            }
            else if (n.StartsWith("Anfora_", StringComparison.OrdinalIgnoreCase))
            {
                if (t.GetComponent<ObjetoRompible>() == null)
                {
                    ObjetoRompible rom = t.gameObject.AddComponent<ObjetoRompible>();
                    rom.Configurar(1, new Color(0.74f, 0.44f, 0.28f));
                }
            }
            else if (n.StartsWith("Cofre_Altar_", StringComparison.OrdinalIgnoreCase))
            {
                CofreTesoro cofre = t.GetComponent<CofreTesoro>();
                if (cofre == null)
                {
                    cofre = t.gameObject.AddComponent<CofreTesoro>();
                    cofre.ConfigurarExistente(n, 40, 1);
                }
                RegistrarCofre(cofre);
            }
        }
    }

    private void CrearCofresAdicionales(Transform contenedor)
    {
        RegistrarCofre(CofreTesoro.CrearProcedural("Cofre_Entrada", new Vector3(4.3f, 0f, 3.6f), Quaternion.Euler(0f, -135f, 0f), contenedor, 20, 1));
        RegistrarCofre(CofreTesoro.CrearProcedural("Cofre_Arena", new Vector3(-8.0f, 0f, -4.4f), Quaternion.Euler(0f, -35f, 0f), contenedor, 30, 1));
        RegistrarCofre(CofreTesoro.CrearProcedural("Cofre_Placas", new Vector3(16.6f, 0f, -4.6f), Quaternion.Euler(0f, -45f, 0f), contenedor, 25, 1));
        RegistrarCofre(CofreTesoro.CrearProcedural("Cofre_Secuencia", new Vector3(-4.5f, 0f, -8.3f), Quaternion.Euler(0f, 135f, 0f), contenedor, 30, 0));
        RegistrarCofre(CofreTesoro.CrearProcedural("Cofre_Trampas", new Vector3(-4.4f, 0f, 16.4f), Quaternion.Euler(0f, 140f, 0f), contenedor, 45, 1));
    }

    private void CrearSantuario(Transform contenedor)
    {
        santuario = SantuarioCuracion.Crear(new Vector3(-4.1f, 0f, 3.3f), contenedor);
        interactuables.Add(santuario);
    }

    private void RegistrarCofre(CofreTesoro cofre)
    {
        if (cofre == null || cofres.Contains(cofre))
            return;

        cofres.Add(cofre);
        interactuables.Add(cofre);
    }

    public void SincronizarConInventario(InventarioJugador inventario)
    {
        if (inventario == null)
            return;

        for (int i = 0; i < cofres.Count; i++)
        {
            if (cofres[i] != null)
            {
                cofres[i].EstablecerEstadoCargado(inventario.CofreAbierto(cofres[i].Id));
            }
        }
    }

    public void NotificarSalaResuelta()
    {
        if (santuario != null)
        {
            santuario.RecargarBendicion();
        }
    }

    #endregion
}
