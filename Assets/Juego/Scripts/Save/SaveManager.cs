using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Maneja la lectura, escritura, eliminacion y listado de archivos JSON por perfil de jugador
public static class SaveManager
{
    private static string Carpeta => Path.Combine(Application.persistentDataPath, "Partidas");

    public static bool NombreValido(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 32) return false;
        foreach (char c in nombre)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') return false;
        }
        return true;
    }

    private static string Ruta(string nombre)
    {
        if (!NombreValido(nombre)) throw new ArgumentException("Nombre inválido");
        return Path.Combine(Carpeta, nombre.ToLowerInvariant() + ".json");
    }

    public static bool Existe(string nombre) => NombreValido(nombre) && File.Exists(Ruta(nombre));

    // Escribe primero a un archivo temporal para evitar corrupcion si se cierra de golpe
    public static void Guardar(DatosPartida partida)
    {
        Directory.CreateDirectory(Carpeta);
        string rutaFinal = Ruta(partida.nombre);
        string rutaTemporal = rutaFinal + ".tmp";
        File.WriteAllText(rutaTemporal, JsonUtility.ToJson(partida, true));

        if (File.Exists(rutaFinal)) File.Replace(rutaTemporal, rutaFinal, null);
        else File.Move(rutaTemporal, rutaFinal);
    }

    public static DatosPartida Cargar(string nombre)
    {
        DatosPartida partida = JsonUtility.FromJson<DatosPartida>(File.ReadAllText(Ruta(nombre)));
        if (partida == null || !NombreValido(partida.nombre) || !string.Equals(partida.nombre, nombre, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Archivo de partida inválido");
        }
        return partida;
    }

    public static bool Eliminar(string nombre)
    {
        if (!Existe(nombre)) return false;
        File.Delete(Ruta(nombre));
        return true;
    }

    public static List<DatosPartida> Listar()
    {
        List<DatosPartida> partidas = new List<DatosPartida>();
        if (!Directory.Exists(Carpeta)) return partidas;

        foreach (string ruta in Directory.GetFiles(Carpeta, "*.json"))
        {
            try
            {
                partidas.Add(Cargar(Path.GetFileNameWithoutExtension(ruta)));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Guardado omitido: " + ex.Message);
            }
        }

        partidas.Sort((a, b) => string.Compare(a.nombre, b.nombre, StringComparison.OrdinalIgnoreCase));
        return partidas;
    }
}
