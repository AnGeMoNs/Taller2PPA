using System;
using System.Collections.Generic;
using UnityEngine;

// Sistema de fijado de objetivo estilo Z-Targeting con indicador 3D sobre el enemigo
public class SistemaFijadoObjetivo : MonoBehaviour
{
    [SerializeField] private float rangoFijado = 11.5f;
    [SerializeField] private float rangoPerdida = 15.0f;

    private Enemy objetivoActual;
    private GameObject indicador;
    private Transform romboInterno;
    private readonly List<Enemy> candidatos = new List<Enemy>();
    private readonly RaycastHit[] bufferLineaVista = new RaycastHit[12];

    public Enemy ObjetivoActual => objetivoActual != null && !objetivoActual.Muerto && objetivoActual.gameObject.activeInHierarchy ? objetivoActual : null;
    public bool TieneObjetivo => ObjetivoActual != null;
    public event Action<Enemy> ObjetivoCambiado;

    private void Awake()
    {
        CrearIndicador3D();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.EnJuego)
        {
            if (indicador != null && indicador.activeSelf) indicador.SetActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (TieneObjetivo) LiberarObjetivo();
            else BuscarObjetivoMasCercano();
        }
        else if (TieneObjetivo && (Input.GetKeyDown(KeyCode.V) || Input.GetMouseButtonDown(2)))
        {
            CambiarSiguienteObjetivo();
        }

        ValidarObjetivoActual();
        ActualizarIndicador();
    }

    private void ValidarObjetivoActual()
    {
        if (objetivoActual == null) return;
        float dist = Vector3.Distance(transform.position, objetivoActual.transform.position);
        if (objetivoActual.Muerto || !objetivoActual.gameObject.activeInHierarchy || dist > rangoPerdida)
        {
            Enemy anterior = objetivoActual;
            objetivoActual = null;
            // Si murio el objetivo actual, intenta fijar automaticamente al siguiente enemigo cercano
            if (anterior != null && anterior.Muerto && BuscarObjetivoMasCercano(false))
            {
                return;
            }
            ObjetivoCambiado?.Invoke(null);
        }
    }

    private bool BuscarObjetivoMasCercano(bool reproducirSonido = true)
    {
        RecolectarCandidatos();
        if (candidatos.Count == 0) return false;

        EstablecerObjetivo(candidatos[0], reproducirSonido);
        return true;
    }

    private void CambiarSiguienteObjetivo()
    {
        RecolectarCandidatos();
        if (candidatos.Count <= 1) return;

        int indice = candidatos.IndexOf(objetivoActual);
        int siguiente = (indice + 1) % candidatos.Count;
        EstablecerObjetivo(candidatos[siguiente], true);
    }

    private void RecolectarCandidatos()
    {
        candidatos.Clear();
        Camera cam = Camera.main;
        Vector3 frenteCam = cam != null ? cam.transform.forward : transform.forward;
        frenteCam.y = 0f;
        if (frenteCam.sqrMagnitude > 0.001f) frenteCam.Normalize();

        Enemy[] todos = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in todos)
        {
            if (e == null || e.Muerto || !e.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d > rangoFijado) continue;
            if (!TieneLineaDeVista(e)) continue;
            candidatos.Add(e);
        }

        candidatos.Sort((a, b) =>
        {
            float scoreA = CalcularPuntuacion(a, frenteCam);
            float scoreB = CalcularPuntuacion(b, frenteCam);
            return scoreA.CompareTo(scoreB);
        });
    }

    private float CalcularPuntuacion(Enemy enemigo, Vector3 frenteCam)
    {
        Vector3 delta = enemigo.transform.position - transform.position;
        float dist = delta.magnitude;
        delta.y = 0f;
        float alineacion = delta.sqrMagnitude > 0.001f ? Vector3.Dot(frenteCam, delta.normalized) : 1f;
        return dist - alineacion * 3.0f;
    }

    private bool TieneLineaDeVista(Enemy enemigo)
    {
        Vector3 origen = transform.position + Vector3.up * 1.1f;
        Vector3 destino = enemigo.transform.position + Vector3.up * 0.8f;
        Vector3 dir = destino - origen;
        if (dir.sqrMagnitude < 0.01f) return true;

        int hits = Physics.RaycastNonAlloc(origen, dir.normalized, bufferLineaVista, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits; i++)
        {
            Transform t = bufferLineaVista[i].collider.transform;
            if (!t.IsChildOf(transform) && t.GetComponentInParent<Enemy>() != enemigo)
            {
                return false;
            }
        }
        return true;
    }

    private void EstablecerObjetivo(Enemy nuevo, bool reproducirSonido)
    {
        objetivoActual = nuevo;
        if (reproducirSonido)
        {
            AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.FijarObjetivo, 0.55f);
        }
        ObjetivoCambiado?.Invoke(objetivoActual);
    }

    public void LiberarObjetivo()
    {
        if (objetivoActual == null) return;
        objetivoActual = null;
        ObjetivoCambiado?.Invoke(null);
    }

    private void CrearIndicador3D()
    {
        indicador = new GameObject("Indicador_Fijado");
        romboInterno = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        romboInterno.name = "Rombo";
        Collider col = romboInterno.GetComponent<Collider>();
        if (col != null) Destroy(col);
        romboInterno.SetParent(indicador.transform, false);
        romboInterno.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        romboInterno.localRotation = Quaternion.Euler(45f, 0f, 45f);

        Renderer render = romboInterno.GetComponent<Renderer>();
        Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { color = new Color(1f, 0.86f, 0.22f) };
        render.sharedMaterial = mat;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Light luz = indicador.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.86f, 0.22f);
        luz.intensity = 1.6f;
        luz.range = 2.6f;
        luz.shadows = LightShadows.None;

        indicador.SetActive(false);
    }

    private void ActualizarIndicador()
    {
        if (indicador == null) return;
        Enemy obj = ObjetivoActual;
        if (obj == null)
        {
            if (indicador.activeSelf) indicador.SetActive(false);
            return;
        }

        if (!indicador.activeSelf) indicador.SetActive(true);
        CapsuleCollider cap = obj.GetComponent<CapsuleCollider>();
        float altura = (cap != null ? cap.height : 1.5f) + 0.72f + Mathf.Sin(Time.time * 5f) * 0.08f;
        indicador.transform.position = obj.transform.position + Vector3.up * altura;
        if (romboInterno != null)
        {
            romboInterno.Rotate(0f, 160f * Time.deltaTime, 0f, Space.World);
        }
    }

    private void OnDestroy()
    {
        if (indicador != null) Destroy(indicador);
    }
}
