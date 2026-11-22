using System;
using System.Collections.Generic;
using UnityEngine;

// Estructura serializable que almacena el estado completo de un perfil en formato JSON
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

    // Campos adicionales de inventario, exploracion y estadisticas (compatibles con guardados previos)
    public bool pocionesInicializadas;
    public int pociones = 1;
    public int rupias;
    public float tiempoJugado;
    public int enemigosDerrotados;
    public int parriesPerfectos;
    public int comboMaximo;
    public int danoRecibido;
    public int objetosRotos;
    public string ultimaFechaGuardado;
    public List<string> cofresAbiertos = new List<string>();
    public List<string> salasDescubiertas = new List<string>();
}
