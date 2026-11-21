using System.Collections.Generic;
using UnityEngine;

// Gestiona el ataque cuerpo a cuerpo del jugador y la deteccion de impactos
[RequireComponent(typeof(Movimiento3D), typeof(VidaJugador))]
public class CombateJugador : MonoBehaviour
{
    [SerializeField] private int dano = 25;
    [SerializeField] private float alcance = 1.65f;
    [SerializeField] private float radio = 0.95f;

    private Movimiento3D movimiento;
    private VidaJugador vida;
    private MazmorraPuzzles puzzles;
    private readonly HashSet<Enemy> golpeados = new HashSet<Enemy>();
    private readonly Collider[] bufferColisiones = new Collider[16];
    private readonly RaycastHit[] bufferObstaculos = new RaycastHit[16];

    private void Awake()
    {
        movimiento = GetComponent<Movimiento3D>();
        vida = GetComponent<VidaJugador>();
    }

    private void Start()
    {
        puzzles = FindFirstObjectByType<MazmorraPuzzles>();
    }

    private void OnEnable()
    {
        movimiento.AttackStarted += AlIniciarAtaque;
        movimiento.AttackImpact += EjecutarGolpe;
    }

    private void OnDisable()
    {
        movimiento.AttackStarted -= AlIniciarAtaque;
        movimiento.AttackImpact -= EjecutarGolpe;
    }

    private void AlIniciarAtaque()
    {
        AudioManager.Reproducir("Player_Kick");
    }

    private void EjecutarGolpe()
    {
        if (vida.Muerto) return;

        golpeados.Clear();
        bool potenciado = puzzles != null && puzzles.Recuperado;
        int danoFinal = potenciado ? dano * 2 : dano;

        // El tercer golpe del combo aplica dano extra
        if (movimiento.ComboPaso == 3)
        {
            danoFinal = Mathf.RoundToInt(danoFinal * 1.35f);
        }

        Vector3 centroGolpe = transform.position + transform.forward * alcance + Vector3.up * 0.75f;
        int totalColisiones = Physics.OverlapSphereNonAlloc(centroGolpe, radio, bufferColisiones, ~0, QueryTriggerInteraction.Ignore);
        bool impactoAlguno = false;

        for (int i = 0; i < totalColisiones; i++)
        {
            Collider col = bufferColisiones[i];
            Enemy enemigo = col.GetComponentInParent<Enemy>();
            if (enemigo == null || enemigo.Muerto || golpeados.Contains(enemigo)) continue;

            // Verifica que no haya un muro entre el jugador y el enemigo
            Vector3 origenRayo = transform.position + Vector3.up * 0.75f;
            Vector3 puntoCercano = col.ClosestPoint(origenRayo);
            Vector3 direccion = puntoCercano - origenRayo;
            bool bloqueadoPorMuro = false;

            if (direccion.sqrMagnitude > 0.001f)
            {
                int impactos = Physics.RaycastNonAlloc(origenRayo, direccion.normalized, bufferObstaculos, direccion.magnitude, ~0, QueryTriggerInteraction.Ignore);
                for (int j = 0; j < impactos; j++)
                {
                    Collider obstaculo = bufferObstaculos[j].collider;
                    if (!obstaculo.transform.IsChildOf(transform) && obstaculo.GetComponentInParent<Enemy>() != enemigo)
                    {
                        bloqueadoPorMuro = true;
                        break;
                    }
                }
            }

            if (!bloqueadoPorMuro && golpeados.Add(enemigo))
            {
                enemigo.RecibirDano(danoFinal, transform.position);
                impactoAlguno = true;
            }
        }

        if (impactoAlguno)
        {
            CamaraTerceraPersona.Sacudir(movimiento.ComboPaso == 3 ? 0.11f : 0.06f, 0.1f);
        }
    }
}
