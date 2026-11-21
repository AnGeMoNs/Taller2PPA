using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Coordina los cuatro puzzles de la mazmorra, el altar de sellos y la reliquia sagrada
public class MazmorraPuzzles : MonoBehaviour
{
    public static readonly string[] Salas = { "Combate", "Placas", "Secuencia", "Trampas" };
    public static readonly string[] Objetos = { "LlaveRoja", "LlaveVerde", "LlaveAmarilla", "Gema" };
    private static readonly int[] OrdenPalancas = { 2, 1, 3 };
    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Enemy[] guardianes;
    [SerializeField] private Rigidbody[] cajas;
    [SerializeField] private Renderer[] placas;
    [SerializeField] private PlacaPresion[] placasPresion;
    [SerializeField] private Renderer[] palancas;
    [SerializeField] private PalancaAnimada[] palancasAnimadas;
    [SerializeField] private Renderer[] cristalesSecuencia;
    [SerializeField] private Renderer[] controlesTrampa;
    [SerializeField] private Renderer[] antorchasCombate;
    [SerializeField] private GameObject[] recompensas;
    [SerializeField] private Renderer[] pedestales;
    [SerializeField] private GameObject[] sellosAltar;
    [SerializeField] private Renderer[] conductosAltar;
    [SerializeField] private GameObject hazReliquia;
    [SerializeField] private GameObject puertaFinal;
    [FormerlySerializedAs("bate")]
    [SerializeField] private GameObject reliquia;
    [SerializeField] private TrampaPulso[] trampas;
    [SerializeField] private InteraccionMazmorra[] interacciones;

    private ProgresoPartida progreso;
    private GameManager game;
    private VidaJugador jugador;
    private HUDManager hud;
    private PuertaAnimada puertaAnimada;
    private EquipoLinkVisual equipoLink;
    private Vector3[] origenCajas;
    private Quaternion[] rotacionCajas;
    private int pasoSecuencia;
    private int controlesActivados;
    private int mascaraAltar;
    private int guardianesMuertosPrevios = -1;
    private bool listo;
    private bool recuperado;
    private bool finalCompletado;
    private OleadaFinal oleada;
    private float tiempoEnPlacas;
    private MaterialPropertyBlock bloqueColor;
    private readonly Dictionary<Renderer, bool> cacheColores = new Dictionary<Renderer, bool>();
    private readonly RaycastHit[] bufferVisibilidad = new RaycastHit[16];
    private string ultimoObjetivo = "";
    private string ultimoPrompt = "";
    private string ultimaSala = "";

    public bool FinalCompletado => finalCompletado;
    public bool Recuperado => recuperado;
    public bool EntradaFinalAbierta => listo && TieneTodosLosSellos();

    public void Inicializar(GameManager gm, VidaJugador player, ProgresoPartida prog, HUDManager ui)
    {
        bloqueColor = new MaterialPropertyBlock();
        cacheColores.Clear();
        game = gm;
        jugador = player;
        progreso = prog;
        hud = ui;

        if (puertaFinal != null) puertaAnimada = puertaFinal.GetComponent<PuertaAnimada>();
        if (jugador != null) equipoLink = jugador.GetComponent<EquipoLinkVisual>();

        oleada = new OleadaFinal(transform, game);
        origenCajas = new Vector3[cajas.Length];
        rotacionCajas = new Quaternion[cajas.Length];
        for (int i = 0; i < cajas.Length; i++)
        {
            if (cajas[i] == null) continue;
            origenCajas[i] = cajas[i].position;
            rotacionCajas[i] = cajas[i].rotation;
        }

        listo = true;
        progreso.Cambio += ActualizarMundo;
        ActualizarMundo(true);
    }

    private void OnDestroy()
    {
        if (progreso != null) progreso.Cambio -= ActualizarMundo;
    }

    private void Update()
    {
        if (!listo || !game.EnJuego) return;

        if (recuperado && !finalCompletado && oleada.Completa)
        {
            finalCompletado = true;
            game.Guardar(false);
            game.Ganar();
            return;
        }

        ActualizarCombate();
        if (!progreso.SalaResuelta(Salas[1])) ComprobarPlacas();

        InteraccionMazmorra cercana = BuscarInteraccionCercana();
        string sala = NombreSalaActual();
        string objetivo = ObjetivoActual();
        string prompt = cercana == null ? "" : "[E] " + cercana.Texto;

        if (objetivo != ultimoObjetivo || prompt != ultimoPrompt || sala != ultimaSala)
        {
            hud.MostrarObjetivo(objetivo, prompt);
            hud.MostrarSala(sala, recuperado);
            ultimoObjetivo = objetivo;
            ultimoPrompt = prompt;
            ultimaSala = sala;
        }

        if (cercana != null && Input.GetKeyDown(KeyCode.E))
        {
            ActivarInteraccion(cercana);
        }
    }

    private InteraccionMazmorra BuscarInteraccionCercana()
    {
        InteraccionMazmorra mejorObjeto = null;
        float mejorDistancia = 2.35f;
        Vector3 referenciaJugador = jugador.transform.position + Vector3.up * 0.7f;

        for (int i = 0; i < interacciones.Length; i++)
        {
            InteraccionMazmorra item = interacciones[i];
            if (item == null || !item.gameObject.activeInHierarchy || !EstaDisponible(item)) continue;

            float distancia = Vector3.Distance(referenciaJugador, item.transform.position);
            if (distancia < mejorDistancia && EsVisible(item))
            {
                mejorDistancia = distancia;
                mejorObjeto = item;
            }
        }
        return mejorObjeto;
    }

    private void ActualizarCombate()
    {
        int muertos = 0;
        for (int i = 0; i < guardianes.Length; i++)
        {
            bool muerto = guardianes[i] == null || guardianes[i].Muerto;
            if (muerto) muertos++;
            if (antorchasCombate != null && i < antorchasCombate.Length)
            {
                Pintar(antorchasCombate[i], muerto || progreso.SalaResuelta(Salas[0]));
            }
        }

        if (muertos != guardianesMuertosPrevios)
        {
            guardianesMuertosPrevios = muertos;
            if (!progreso.SalaResuelta(Salas[0]) && guardianes.Length > 0 && muertos >= guardianes.Length)
            {
                ResolverSala(0);
            }
        }
    }

    private bool EsVisible(InteraccionMazmorra interaccion)
    {
        Vector3 origen = jugador.transform.position + Vector3.up * 1.25f;
        Vector3 delta = interaccion.transform.position - origen;
        if (delta.sqrMagnitude < 0.001f) return true;

        int cantidad = Physics.RaycastNonAlloc(origen, delta.normalized, bufferVisibilidad, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
        {
            Transform colTransform = bufferVisibilidad[i].collider.transform;
            bool esJugador = colTransform.IsChildOf(jugador.transform);
            bool esObjetivo = colTransform.IsChildOf(interaccion.transform);
            bool esPedestalPropio = interaccion.Tipo == TipoInteraccion.Pedestal && colTransform == interaccion.transform.parent;

            if (!esJugador && !esObjetivo && !esPedestalPropio) return false;
        }
        return true;
    }

    public string NombreSalaActual()
    {
        Vector3 p = jugador.transform.position;
        if (p.z > 18f) return "CÁMARA DEL ALTAR";
        if (p.x < -6f) return "SALA OESTE · ARENA";
        if (p.x > 6f) return "SALA ESTE · PLACAS";
        if (p.z < -6f) return "SALA SUR · PALANCAS";
        if (p.z > 6f) return "SALA NORTE · TRAMPAS";
        return "VESTÍBULO CENTRAL";
    }

    private string ObjetivoActual()
    {
        Vector3 p = jugador.transform.position;
        if (recuperado) return "RELIQUIA ACTIVA · Derrota la oleada final. Restantes: " + oleada.Restantes;
        if (p.z > 18f) return mascaraAltar == 15 ? "ALTAR: reclama la Reliquia Sagrada del centro." : "ALTAR: coloca los cuatro sellos en sus pedestales (" + ContarSellos() + "/4).";
        if (p.x < -6f) return progreso.TieneObjeto(Objetos[0]) ? "ARENA: completada." : progreso.SalaResuelta(Salas[0]) ? "ARENA: recoge la llave roja." : "ARENA: derrota a los guardianes (Clic Izq. combo · Clic Der. escudo).";
        if (p.x > 6f) return progreso.TieneObjeto(Objetos[1]) ? "PLACAS: completado." : progreso.SalaResuelta(Salas[1]) ? "PLACAS: recoge la llave verde." : "PLACAS: empuja una caja sobre cada placa luminosa. Usa RESET si la atascas.";
        if (p.z < -6f) return progreso.TieneObjeto(Objetos[2]) ? "PALANCAS: completado." : progreso.SalaResuelta(Salas[2]) ? "PALANCAS: recoge la llave amarilla." : "PALANCAS: lee el mural (II - I - III) y activa en orden. " + pasoSecuencia + "/3";
        if (p.z > 6f) return progreso.TieneObjeto(Objetos[3]) ? (TieneTodosLosSellos() ? "TRAMPAS: completado. La puerta del altar está abierta." : "TRAMPAS: completado.") : progreso.SalaResuelta(Salas[3]) ? "TRAMPAS: recoge la gema." : "TRAMPAS: espera la luz verde (ámbar = aviso) y activa los controles en orden. " + controlesActivados + "/2";
        return TieneTodosLosSellos() ? "La reja norte se ha levantado. Entra a la Cámara del Altar." : "Reúne los 4 sellos. Oeste: arena · Este: placas · Sur: palancas · Norte: trampas.";
    }

    private int ContarSellos()
    {
        int total = 0;
        for (int i = 0; i < 4; i++)
        {
            if ((mascaraAltar & (1 << i)) != 0) total++;
        }
        return total;
    }

    private bool EstaDisponible(InteraccionMazmorra item)
    {
        int idx = item.Indice;
        switch (item.Tipo)
        {
            case TipoInteraccion.Recompensa: return progreso.SalaResuelta(Salas[idx]) && !progreso.TieneObjeto(Objetos[idx]);
            case TipoInteraccion.Palanca: return !progreso.SalaResuelta(Salas[2]);
            case TipoInteraccion.ResetCajas: return !progreso.SalaResuelta(Salas[1]);
            case TipoInteraccion.ControlTrampa: return !progreso.SalaResuelta(Salas[3]);
            case TipoInteraccion.Pedestal: return TieneTodosLosSellos() && (mascaraAltar & (1 << idx)) == 0;
            case TipoInteraccion.Reliquia: return mascaraAltar == 15 && !recuperado;
            default: return false;
        }
    }

    private void ActivarInteraccion(InteraccionMazmorra item)
    {
        int idx = item.Indice;
        switch (item.Tipo)
        {
            case TipoInteraccion.Recompensa:
                progreso.Recoger(Objetos[idx]);
                hud.Aviso("Sello obtenido: " + Objetos[idx]);
                game.Guardar(false);
                break;

            case TipoInteraccion.Palanca:
                if (idx == OrdenPalancas[pasoSecuencia])
                {
                    pasoSecuencia++;
                    AudioManager.Reproducir("UI_Click");
                    if (pasoSecuencia == OrdenPalancas.Length) ResolverSala(2);
                    else hud.Aviso("Palanca correcta (" + pasoSecuencia + "/3)");
                }
                else
                {
                    pasoSecuencia = 0;
                    AudioManager.Reproducir("Player_Hurt");
                    if (palancasAnimadas != null)
                    {
                        for (int i = 0; i < palancasAnimadas.Length; i++)
                        {
                            if (palancasAnimadas[i] != null) palancasAnimadas[i].DestelloError();
                        }
                    }
                    hud.Aviso("Orden incorrecto. Sigue el mural: II — I — III.");
                }
                ActualizarMundo();
                break;

            case TipoInteraccion.ResetCajas:
                ReiniciarCajas();
                AudioManager.Reproducir("UI_Click");
                hud.Aviso("Cajas devueltas a su posición inicial");
                break;

            case TipoInteraccion.ControlTrampa:
                if (idx == controlesActivados + 1)
                {
                    controlesActivados++;
                    AudioManager.Reproducir("UI_Click");
                    if (controlesActivados == 2) ResolverSala(3);
                    else hud.Aviso("Control 1 desbloqueado. Avanza al Control 2.");
                }
                else
                {
                    AudioManager.Reproducir("Player_Hurt");
                    hud.Aviso("Activa primero el Control 1.");
                }
                ActualizarMundo();
                break;

            case TipoInteraccion.Pedestal:
                mascaraAltar |= 1 << idx;
                AudioManager.Reproducir("Puzzle_Complete");
                ActualizarMundo();
                CamaraTerceraPersona.Sacudir(0.08f, 0.16f);
                hud.Aviso(mascaraAltar == 15 ? "¡Los 4 sellos resuenan! La Reliquia Sagrada ha aparecido." : "Sello colocado en el altar (" + ContarSellos() + "/4)");
                game.Guardar(false);
                break;

            case TipoInteraccion.Reliquia:
                if (!oleada.Disponible)
                {
                    hud.Aviso("Faltan los guardianes de la oleada. Ejecuta Taller 2 > 6. Reparar fisicas y enemigos.");
                    break;
                }
                recuperado = true;
                jugador.PotenciarConReliquia();
                jugador.GetComponent<Movimiento3D>()?.ReproducirPowerUp();
                equipoLink?.EstablecerPotenciado(true);
                ActualizarMundo();
                oleada.Activar();
                AudioManager.Reproducir("Item_Pickup");
                CamaraTerceraPersona.Sacudir(0.16f, 0.3f);
                hud.Aviso("¡Espada Maestra potenciada! Vida 300 y daño x2. Derrota la oleada final.");
                game.Guardar(false);
                break;
        }
    }

    private void ComprobarPlacas()
    {
        if (placas == null || placas.Length < 2 || placas[0] == null || placas[1] == null) return;

        int cajaA = BuscarCajaSobre(placas[0].transform.position);
        int cajaB = BuscarCajaSobre(placas[1].transform.position);
        ActualizarVisualPlaca(0, cajaA >= 0);
        ActualizarVisualPlaca(1, cajaB >= 0);

        if (cajaA >= 0) CentrarSuave(cajas[cajaA], placas[0].transform.position);
        if (cajaB >= 0) CentrarSuave(cajas[cajaB], placas[1].transform.position);

        if (cajaA >= 0 && cajaB >= 0 && cajaA != cajaB)
        {
            tiempoEnPlacas += Time.deltaTime;
            if (tiempoEnPlacas >= 0.35f) ResolverSala(1);
        }
        else
        {
            tiempoEnPlacas = 0f;
        }
    }

    private void CentrarSuave(Rigidbody caja, Vector3 centroPlaca)
    {
        if (caja == null) return;
        Vector3 diferencia = new Vector3(centroPlaca.x - caja.position.x, 0f, centroPlaca.z - caja.position.z);
        if (diferencia.sqrMagnitude < 0.0004f || caja.linearVelocity.sqrMagnitude > 1.2f) return;
        caja.MovePosition(caja.position + diferencia * Mathf.Min(1f, Time.deltaTime * 4.5f));
    }

    private void ActualizarVisualPlaca(int indice, bool activa)
    {
        if (placasPresion != null && indice < placasPresion.Length && placasPresion[indice] != null)
        {
            placasPresion[indice].EstablecerActiva(activa);
        }
        else if (indice < placas.Length)
        {
            Pintar(placas[indice], activa);
        }
    }

    private int BuscarCajaSobre(Vector3 centro)
    {
        for (int i = 0; i < cajas.Length; i++)
        {
            if (cajas[i] == null) continue;
            Vector3 diff = cajas[i].position - centro;
            if (Mathf.Abs(diff.x) < 0.78f && Mathf.Abs(diff.z) < 0.78f && diff.y > 0.15f && diff.y < 1.1f)
            {
                return i;
            }
        }
        return -1;
    }

    private void ReiniciarCajas()
    {
        for (int i = 0; i < cajas.Length; i++)
        {
            if (cajas[i] == null) continue;
            cajas[i].position = origenCajas[i];
            cajas[i].rotation = rotacionCajas[i];
            cajas[i].linearVelocity = Vector3.zero;
            cajas[i].angularVelocity = Vector3.zero;
        }
        tiempoEnPlacas = 0f;
    }

    private void ResolverSala(int indiceSala)
    {
        progreso.ResolverSala(Salas[indiceSala]);
        CamaraTerceraPersona.Sacudir(0.09f, 0.2f);
        hud.Aviso("¡Prueba superada! Recoge tu sello en el pedestal.");
        game.Guardar(false);
    }

    private bool TieneTodosLosSellos()
    {
        for (int i = 0; i < Salas.Length; i++)
        {
            if (!progreso.SalaResuelta(Salas[i]) || !progreso.TieneObjeto(Objetos[i])) return false;
        }
        return true;
    }

    private void ActualizarMundo() => ActualizarMundo(false);

    private void ActualizarMundo(bool instantaneo)
    {
        if (!listo) return;

        for (int i = 0; i < 4; i++)
        {
            if (i < recompensas.Length && recompensas[i] != null)
            {
                recompensas[i].SetActive(progreso.SalaResuelta(Salas[i]) && !progreso.TieneObjeto(Objetos[i]));
            }
        }

        bool abrirPuerta = TieneTodosLosSellos();
        if (puertaAnimada != null)
        {
            if (!puertaAnimada.Abierta && abrirPuerta && game.EnJuego) AudioManager.Reproducir("Door_Open");
            puertaAnimada.EstablecerAbierta(abrirPuerta, instantaneo);
        }
        else if (puertaFinal != null)
        {
            if (puertaFinal.activeSelf && abrirPuerta && game.EnJuego) AudioManager.Reproducir("Door_Open");
            puertaFinal.SetActive(!abrirPuerta);
        }

        bool secuenciaResuelta = progreso.SalaResuelta(Salas[2]);
        for (int i = 0; i < palancas.Length; i++)
        {
            bool activa = secuenciaResuelta || Array.IndexOf(OrdenPalancas, i + 1) < pasoSecuencia;
            if (palancasAnimadas != null && i < palancasAnimadas.Length && palancasAnimadas[i] != null)
            {
                palancasAnimadas[i].EstablecerEstado(activa);
            }
            else
            {
                Pintar(palancas[i], activa);
            }
        }

        if (cristalesSecuencia != null)
        {
            for (int i = 0; i < cristalesSecuencia.Length; i++)
            {
                Pintar(cristalesSecuencia[i], secuenciaResuelta || i < pasoSecuencia);
            }
        }

        if (progreso.SalaResuelta(Salas[1]))
        {
            for (int i = 0; i < placas.Length; i++) ActualizarVisualPlaca(i, true);
        }

        bool trampasResueltas = progreso.SalaResuelta(Salas[3]);
        for (int i = 0; i < trampas.Length; i++)
        {
            if (trampas[i] != null) trampas[i].Desactivada = trampasResueltas;
        }
        if (controlesTrampa != null)
        {
            for (int i = 0; i < controlesTrampa.Length; i++)
            {
                Pintar(controlesTrampa[i], trampasResueltas || i < controlesActivados);
            }
        }

        for (int i = 0; i < pedestales.Length; i++)
        {
            bool colocado = (mascaraAltar & (1 << i)) != 0;
            Pintar(pedestales[i], colocado);
            if (sellosAltar != null && i < sellosAltar.Length && sellosAltar[i] != null)
            {
                sellosAltar[i].SetActive(colocado);
            }
            if (conductosAltar != null && i < conductosAltar.Length)
            {
                PintarDorado(conductosAltar[i], colocado);
            }
        }

        bool mostrarReliquia = abrirPuerta && mascaraAltar == 15 && !recuperado;
        if (reliquia != null) reliquia.SetActive(mostrarReliquia);
        if (hazReliquia != null) hazReliquia.SetActive(mostrarReliquia);
        equipoLink?.EstablecerPotenciado(recuperado);
    }

    private void Pintar(Renderer r, bool activo)
    {
        if (r == null) return;
        if (cacheColores.TryGetValue(r, out bool anterior) && anterior == activo) return;
        if (bloqueColor == null) bloqueColor = new MaterialPropertyBlock();

        Color color = activo ? new Color(0.19f, 0.86f, 0.38f) : new Color(0.55f, 0.29f, 0.13f);
        r.GetPropertyBlock(bloqueColor);
        bloqueColor.SetColor(PropColor, color);
        bloqueColor.SetColor(PropBaseColor, color);
        r.SetPropertyBlock(bloqueColor);
        cacheColores[r] = activo;
    }

    private void PintarDorado(Renderer r, bool activo)
    {
        if (r == null) return;
        if (cacheColores.TryGetValue(r, out bool anterior) && anterior == activo) return;
        if (bloqueColor == null) bloqueColor = new MaterialPropertyBlock();

        Color color = activo ? new Color(1f, 0.80f, 0.22f) : new Color(0.24f, 0.21f, 0.18f);
        r.GetPropertyBlock(bloqueColor);
        bloqueColor.SetColor(PropColor, color);
        bloqueColor.SetColor(PropBaseColor, color);
        r.SetPropertyBlock(bloqueColor);
        cacheColores[r] = activo;
    }

    public void Escribir(DatosPartida datos)
    {
        datos.versionFinal = 1;
        datos.oleadaCompletada = finalCompletado;
        datos.secuencia = pasoSecuencia;
        datos.controlTrampas = controlesActivados;
        datos.altarMask = mascaraAltar;
        datos.reliquiaRecuperada = recuperado;

        datos.cajas = new Vector3[cajas.Length];
        datos.rotacionesCajas = new Quaternion[cajas.Length];
        for (int i = 0; i < cajas.Length; i++)
        {
            if (cajas[i] == null) continue;
            datos.cajas[i] = cajas[i].position;
            datos.rotacionesCajas[i] = cajas[i].rotation;
        }
    }

    public void Restaurar(DatosPartida datos)
    {
        pasoSecuencia = progreso.SalaResuelta(Salas[2]) ? 3 : Mathf.Clamp(datos.secuencia, 0, 2);
        controlesActivados = progreso.SalaResuelta(Salas[3]) ? 2 : Mathf.Clamp(datos.controlTrampas, 0, 1);
        mascaraAltar = TieneTodosLosSellos() ? datos.altarMask & 15 : 0;
        recuperado = mascaraAltar == 15 && datos.reliquiaRecuperada;
        finalCompletado = recuperado && datos.oleadaCompletada;

        if (recuperado && !finalCompletado)
        {
            if (oleada.Disponible) oleada.Activar();
            else
            {
                recuperado = false;
                Debug.LogWarning("Repara la escena antes de recuperar la Reliquia Sagrada.");
            }
        }

        ReiniciarCajas();
        if (datos.cajas != null && datos.cajas.Length == cajas.Length)
        {
            for (int i = 0; i < cajas.Length; i++)
            {
                if (cajas[i] == null) continue;
                cajas[i].position = datos.cajas[i];
                if (datos.rotacionesCajas != null && datos.rotacionesCajas.Length == cajas.Length)
                {
                    cajas[i].rotation = datos.rotacionesCajas[i];
                }
            }
        }

        ActualizarMundo(true);
    }
}
