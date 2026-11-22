using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Gestiona el combate cuerpo a cuerpo, racha de combo, contraataque de parry, ataque giratorio y hit-stop
[RequireComponent(typeof(Movimiento3D), typeof(VidaJugador))]
public class CombateJugador : MonoBehaviour
{
    [SerializeField] private int dano = 25;
    [SerializeField] private float alcance = 1.75f;
    [SerializeField] private float radio = 1.05f;

    private const float DuracionVentanaCombo = 4.2f;
    private const float DuracionContraataque = 3.5f;
    private const float RadioAtaqueGiratorio = 2.75f;

    private Movimiento3D movimiento;
    private VidaJugador vida;
    private InventarioJugador inventario;
    private MazmorraPuzzles puzzles;
    private Coroutine corrutinaHitStop;

    private int rachaCombo;
    private float comboExpiraEn;
    private float contraataqueHasta;

    private readonly HashSet<Enemy> golpeados = new HashSet<Enemy>();
    private readonly HashSet<ObjetoRompible> rompiblesGolpeados = new HashSet<ObjetoRompible>();
    private readonly Collider[] bufferColisiones = new Collider[24];
    private readonly RaycastHit[] bufferObstaculos = new RaycastHit[16];

    public int RachaCombo => rachaCombo;
    public float ProgresoTiempoCombo => rachaCombo > 0 ? Mathf.Clamp01((comboExpiraEn - Time.time) / DuracionVentanaCombo) : 0f;
    public bool ContraataqueListo => Time.time < contraataqueHasta;

    public event Action<int, float> ComboCambiado;

    private void Awake()
    {
        movimiento = GetComponent<Movimiento3D>();
        vida = GetComponent<VidaJugador>();
        inventario = GetComponent<InventarioJugador>();
    }

    private void Start()
    {
        puzzles = FindFirstObjectByType<MazmorraPuzzles>();
    }

    private void OnEnable()
    {
        movimiento.AttackStarted += AlIniciarAtaque;
        movimiento.AttackImpact += EjecutarGolpe;
        movimiento.SpinAttackExecuted += EjecutarAtaqueGiratorio;
    }

    private void OnDisable()
    {
        movimiento.AttackStarted -= AlIniciarAtaque;
        movimiento.AttackImpact -= EjecutarGolpe;
        movimiento.SpinAttackExecuted -= EjecutarAtaqueGiratorio;
    }

    private void Update()
    {
        if (rachaCombo > 0 && Time.time >= comboExpiraEn)
        {
            rachaCombo = 0;
            ComboCambiado?.Invoke(0, 0f);
        }
    }

    public void ActivarContraataque()
    {
        contraataqueHasta = Time.time + DuracionContraataque;
    }

    public void SolicitarHitStop(float duracion)
    {
        if (GameManager.Instance == null || !GameManager.Instance.EnJuego) return;
        if (corrutinaHitStop != null) StopCoroutine(corrutinaHitStop);
        corrutinaHitStop = StartCoroutine(AplicarHitStop(duracion));
    }

    private IEnumerator AplicarHitStop(float duracion)
    {
        Time.timeScale = 0.08f;
        yield return new WaitForSecondsRealtime(duracion);
        if (GameManager.Instance == null || GameManager.Instance.EnJuego)
        {
            Time.timeScale = 1f;
        }
        corrutinaHitStop = null;
    }

    private void AlIniciarAtaque()
    {
        float pitch = movimiento.ComboPaso == 3 ? 0.90f : (movimiento.ComboPaso == 2 ? 1.08f : 1.0f);
        AudioManager.Reproducir("Player_Kick", null, 0.88f, pitch, 0.05f);
    }

    private void EjecutarGolpe()
    {
        if (vida.Muerto) return;
        if (inventario == null) inventario = GetComponent<InventarioJugador>();

        bool potenciado = puzzles != null && puzzles.Recuperado;
        bool esContraataque = ContraataqueListo;
        bool esFinisher = movimiento.ComboPaso == 3;
        bool critico = esContraataque || esFinisher;

        float multiplicadorRacha = 1f + Mathf.Min(0.30f, rachaCombo * 0.05f);
        int danoFinal = potenciado ? dano * 2 : dano;
        if (esFinisher) danoFinal = Mathf.RoundToInt(danoFinal * 1.35f);
        if (esContraataque)
        {
            danoFinal = Mathf.RoundToInt(danoFinal * 1.75f);
            contraataqueHasta = 0f;
        }
        danoFinal = Mathf.RoundToInt(danoFinal * multiplicadorRacha);

        Vector3 centroGolpe = transform.position + transform.forward * alcance + Vector3.up * 0.78f;
        float empuje = esFinisher || esContraataque ? 0.65f : 0.35f;
        int impactos = AplicarAreaDeDano(centroGolpe, radio, danoFinal, critico, empuje, 0f);

        if (impactos > 0)
        {
            CamaraTerceraPersona.Sacudir(critico ? 0.13f : 0.065f, critico ? 0.12f : 0.09f);
            SolicitarHitStop(critico ? 0.062f : 0.032f);
        }
    }

    private void EjecutarAtaqueGiratorio()
    {
        if (vida.Muerto) return;
        if (inventario == null) inventario = GetComponent<InventarioJugador>();

        bool potenciado = puzzles != null && puzzles.Recuperado;
        bool esContraataque = ContraataqueListo;
        int danoGiro = Mathf.RoundToInt((potenciado ? dano * 2 : dano) * (esContraataque ? 2.1f : 1.55f));
        if (esContraataque) contraataqueHasta = 0f;

        AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.AtaqueGiratorio, 0.85f);
        AudioManager.Reproducir("Player_Kick", null, 0.85f, 0.82f);
        EfectosVisuales.OndaAtaqueGiratorio(transform.position, potenciado || esContraataque);
        CamaraTerceraPersona.Sacudir(0.14f, 0.18f);

        Vector3 centro = transform.position + Vector3.up * 0.78f;
        int impactos = AplicarAreaDeDano(centro, RadioAtaqueGiratorio, danoGiro, true, 0.95f, 0.85f);

        if (impactos > 0)
        {
            SolicitarHitStop(0.072f);
        }
    }

    private int AplicarAreaDeDano(Vector3 centro, float radioGolpe, int danoAplicado, bool critico, float retroceso, float duracionAturdimiento)
    {
        golpeados.Clear();
        rompiblesGolpeados.Clear();

        int totalColisiones = Physics.OverlapSphereNonAlloc(centro, radioGolpe, bufferColisiones, ~0, QueryTriggerInteraction.Ignore);
        int impactos = 0;

        for (int i = 0; i < totalColisiones; i++)
        {
            Collider col = bufferColisiones[i];
            if (col == null) continue;

            // 1. Revisar objetos rompibles (barriles, anforas)
            ObjetoRompible rompible = col.GetComponentInParent<ObjetoRompible>();
            if (rompible != null && !rompible.Roto && rompiblesGolpeados.Add(rompible))
            {
                rompible.RecibirGolpe(transform.position, inventario);
                impactos++;
                continue;
            }

            // 2. Revisar enemigos con verificacion de linea de vista contra muros
            Enemy enemigo = col.GetComponentInParent<Enemy>();
            if (enemigo == null || enemigo.Muerto || golpeados.Contains(enemigo)) continue;

            if (!EstaBloqueadoPorMuro(col, enemigo) && golpeados.Add(enemigo))
            {
                enemigo.RecibirDano(danoAplicado, transform.position, critico, retroceso);
                if (duracionAturdimiento > 0f && !enemigo.Muerto)
                {
                    enemigo.Aturdir(duracionAturdimiento, transform.position);
                }
                RegistrarImpactoCombo();
                impactos++;
            }
        }

        return impactos;
    }

    private bool EstaBloqueadoPorMuro(Collider colEnemigo, Enemy enemigo)
    {
        Vector3 origenRayo = transform.position + Vector3.up * 0.78f;
        Vector3 puntoCercano = colEnemigo.ClosestPoint(origenRayo);
        Vector3 direccion = puntoCercano - origenRayo;
        if (direccion.sqrMagnitude <= 0.001f) return false;

        int impactos = Physics.RaycastNonAlloc(origenRayo, direccion.normalized, bufferObstaculos, direccion.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int j = 0; j < impactos; j++)
        {
            Collider obstaculo = bufferObstaculos[j].collider;
            if (!obstaculo.transform.IsChildOf(transform)
                && obstaculo.GetComponentInParent<Enemy>() != enemigo
                && obstaculo.GetComponentInParent<ObjetoRompible>() == null)
            {
                return true;
            }
        }
        return false;
    }

    private void RegistrarImpactoCombo()
    {
        rachaCombo++;
        comboExpiraEn = Time.time + DuracionVentanaCombo;
        inventario?.RegistrarCombo(rachaCombo);
        ComboCambiado?.Invoke(rachaCombo, 1f);
    }
}
