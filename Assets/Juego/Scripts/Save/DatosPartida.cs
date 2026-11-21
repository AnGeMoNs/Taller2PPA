using System;
using System.Collections.Generic;
using UnityEngine;

// Estructura serializable que almacena el estado de un perfil en formato JSON
[Serializable]
public class DatosPartida
{
    public int secuencia;
    public int controlTrampas;
    public int altarMask;
    public bool reliquiaRecuperada;
    public bool oleadaCompletada;
    public int versionFinal;
    public Vector3[] cajas;
    public Quaternion[] rotacionesCajas;
    public string nombre;
    public Vector3 posicion;
    public int vida = 100;
    public List<string> salasResueltas = new List<string>();
    public List<string> enemigosMuertos = new List<string>();
    public List<string> objetos = new List<string>();
}
