using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Guarda, carga, lista y elimina archivos JSON por perfil de jugador
public static class SaveManager
{
    #region CLASS_VARIABLES

    private static string Carpeta => Path.Combine(Application.persistentDataPath, "Partidas");

    #endregion

    #region CLASS_METHODS

    public static bool NombreValido(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 32)
            return false;

        foreach (char c in nombre)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                return false;
        }

        return true;
    }

    private static string Ruta(string nombre)
    {
        if (!NombreValido(nombre))
        {
            throw new ArgumentException("Nombre invalido");
        }

        return Path.Combine(Carpeta, nombre.ToLowerInvariant() + ".json");
    }

    public static bool Existe(string nombre)
    {
        return NombreValido(nombre) && File.Exists(Ruta(nombre));
    }

    public static void Guardar(DatosPartida partida)
    {
        Directory.CreateDirectory(Carpeta);

        string rutaFinal = Ruta(partida.nombre);
        string rutaTemporal = rutaFinal + ".tmp";
        string dataAsJson = JsonUtility.ToJson(partida, true);

        // Escribe en un temporal primero para no danar el archivo si se cierra el juego
        using (StreamWriter writer = new StreamWriter(rutaTemporal))
        {
            writer.Write(dataAsJson);
        }

        if (File.Exists(rutaFinal))
        {
            File.Replace(rutaTemporal, rutaFinal, null);
        }
        else
        {
            File.Move(rutaTemporal, rutaFinal);
        }
    }

    public static DatosPartida Cargar(string nombre)
    {
        string json = File.ReadAllText(Ruta(nombre));
        DatosPartida partida = JsonUtility.FromJson<DatosPartida>(json);

        if (partida == null || !NombreValido(partida.nombre) || !string.Equals(partida.nombre, nombre, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Archivo de partida invalido");
        }

        return partida;
    }

    public static bool Eliminar(string nombre)
    {
        if (!Existe(nombre))
            return false;

        File.Delete(Ruta(nombre));
        return true;
    }

    public static List<DatosPartida> Listar()
    {
        List<DatosPartida> partidas = new List<DatosPartida>();

        if (!Directory.Exists(Carpeta))
            return partidas;

        string[] archivos = Directory.GetFiles(Carpeta, "*.json");
        for (int i = 0; i < archivos.Length; i++)
        {
            try
            {
                string nombreArchivo = Path.GetFileNameWithoutExtension(archivos[i]);
                partidas.Add(Cargar(nombreArchivo));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Guardado omitido: " + ex.Message);
            }
        }

        partidas.Sort((a, b) => string.Compare(a.nombre, b.nombre, StringComparison.OrdinalIgnoreCase));
        return partidas;
    }

    #endregion
}
