using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Behavior;

// Clase base abstracta de los enemigos con atributos comunes y control de parametros del Animator
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected int vidaMaxima = 50;
    [SerializeField] protected int dano = 10;
    [SerializeField] protected float velocidad = 1.4f;
    [SerializeField] protected float rangoVista = 6f;
    [SerializeField] protected float rangoAtaque = 1.2f;
    [SerializeField] private bool usarUnityBehavior;
    [SerializeField] private float radioTerritorio = 4.4f;

    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    private static Material materialParticulasCompartido;

    private BehaviorGraphAgent agente;
    private float ultimoNodo = -100f;
    private float bloqueoRuta;
    private int vida;
    private Animator animator;
    private Rigidbody cuerpo;
    private Renderer[] mallas;
    private MaterialPropertyBlock bloqueFlash;
    private BarraVidaEnemigo barraVida;
    private float flashHasta;
    private bool flashActivo;
    private float tiempoMuerte = -1f;
    private Vector3 escalaInicial;
    private Vector3 origen;
    private int puntoActual;
    private Vector3 direccionMovimiento;
    private readonly RaycastHit[] bufferRayos = new RaycastHit[16];
    private readonly HashSet<string> parametrosAnimator = new HashSet<string>();

    protected VidaJugador jugador;
    protected float siguienteAtaque;
    protected bool observado;

    protected Vector3 Origen => origen;
    protected float RadioTerritorio => radioTerritorio;
    protected float BloqueoRuta => bloqueoRuta;
    protected bool DentroTerritorio => jugador != null && Vector3.Distance(jugador.transform.position, origen) <= radioTerritorio + 1f;
    protected virtual Color ColorParticulasMuerte => new Color(0.85f, 0.82f, 0.72f);
    protected virtual float TiempoDesaparicion => 1.55f;

    public bool PuedeActuar => EnJuego;
    public void NotificarNodo() { ultimoNodo = Time.time; }
    public bool Muerto { get; private set; }
    public int VidaActual => vida;
    public int VidaMaxima => vidaMaxima;
    public string Id => gameObject.name;
    public event Action<Enemy> Murio;
    protected bool EnJuego => GameManager.Instance != null && GameManager.Instance.EnJuego && !Muerto && jugador != null && !jugador.Muerto;

    protected virtual void Awake()
    {
        vida = vidaMaxima;
        escalaInicial = transform.localScale;
        animator = GetComponentInChildren<Animator>();
        cuerpo = GetComponent<Rigidbody>();
        bloqueFlash = new MaterialPropertyBlock();
        origen = transform.position;
        agente = GetComponent<BehaviorGraphAgent>();

        List<Renderer> listaRenderers = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.transform.name == "Fondo" || r.transform.name == "Relleno") continue;
            listaRenderers.Add(r);
        }
        mallas = listaRenderers.ToArray();

        barraVida = GetComponent<BarraVidaEnemigo>();
        if (barraVida == null) barraVida = gameObject.AddComponent<BarraVidaEnemigo>();
        CapsuleCollider capsula = GetComponent<CapsuleCollider>();
        barraVida.Inicializar(capsula != null ? capsula.height : 1.5f);

        foreach (SkinnedMeshRenderer skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skin.updateWhenOffscreen = true;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            CachearParametrosAnimator();
        }
    }

    private void CachearParametrosAnimator()
    {
        parametrosAnimator.Clear();
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            parametrosAnimator.Add(param.name);
        }
    }

    private bool TieneParametro(string nombre)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        if (parametrosAnimator.Count == 0) CachearParametrosAnimator();
        return parametrosAnimator.Contains(nombre);
    }

    protected void SetCaminando(bool caminando)
    {
        if (TieneParametro("Caminando"))
        {
            animator.SetBool("Caminando", caminando);
        }
    }

    protected void SetDefendiendo(bool defendiendo)
    {
        if (TieneParametro("Defendiendo"))
        {
            animator.SetBool("Defendiendo", defendiendo);
        }
    }

    protected void ActivarAtaque()
    {
        if (TieneParametro("Atacar"))
        {
            animator.SetTrigger("Atacar");
        }
    }

    protected void ActivarGolpe()
    {
        if (TieneParametro("Golpe"))
        {
            animator.SetTrigger("Golpe");
        }
    }

    protected void ActivarMuerte()
    {
        if (TieneParametro("Atacar")) animator.ResetTrigger("Atacar");
        if (TieneParametro("Golpe")) animator.ResetTrigger("Golpe");
        if (TieneParametro("Morir"))
        {
            animator.SetTrigger("Morir");
        }
    }

    private void Start()
    {
        jugador = FindFirstObjectByType<VidaJugador>();
        if (agente != null && agente.Graph != null && usarUnityBehavior)
        {
            if (agente.GetVariable<GameObject>("Agent", out _))
            {
                agente.SetVariableValue("Agent", gameObject);
            }
        }
    }

    protected virtual void Update()
    {
        ActualizarFlash();
        if (Muerto)
        {
            ActualizarDesaparicion();
            return;
        }

        if (!EnJuego)
        {
            Detener();
            return;
        }

        if (usarUnityBehavior && agente != null && agente.isActiveAndEnabled && agente.Graph != null && Time.time - ultimoNodo < 0.25f)
        {
            return;
        }

        if (!IntentarAtacar() && !IntentarPerseguir())
        {
            Patrullar();
        }
    }

    private void ActualizarFlash()
    {
        if (!flashActivo || Time.time < flashHasta) return;
        flashActivo = false;
        for (int i = 0; i < mallas.Length; i++)
        {
            if (mallas[i] != null) mallas[i].SetPropertyBlock(null);
        }
    }

    private void ActualizarDesaparicion()
    {
        if (tiempoMuerte < 0f) return;
        float transcurrido = Time.time - tiempoMuerte;
        float inicioEncoger = Mathf.Max(0.4f, TiempoDesaparicion - 0.45f);
        if (transcurrido > inicioEncoger)
        {
            float progreso = Mathf.Clamp01((transcurrido - inicioEncoger) / 0.45f);
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, progreso * progreso);
        }
    }

    private void FixedUpdate()
    {
        if (!EnJuego) return;

        Vector3 distancia = jugador.transform.position - transform.position;
        if (Mathf.Abs(distancia.y) < 0.45f && (distancia.x * distancia.x + distancia.z * distancia.z) <= 0.82f * 0.82f)
        {
            Contacto();
        }

        if (direccionMovimiento.sqrMagnitude < 0.001f) return;

        float factorVel = FactorVelocidadActual();
        Vector3 paso = direccionMovimiento * (velocidad * factorVel) * Time.fixedDeltaTime;
        Quaternion rotObjetivo = Quaternion.LookRotation(direccionMovimiento);
        cuerpo.MoveRotation(Quaternion.Slerp(cuerpo.rotation, rotObjetivo, 1f - Mathf.Exp(-10f * Time.fixedDeltaTime)));

        if (cuerpo.SweepTest(direccionMovimiento, out RaycastHit hit, paso.magnitude + 0.05f, QueryTriggerInteraction.Ignore))
        {
            paso = direccionMovimiento * Mathf.Max(0f, Mathf.Min(paso.magnitude, hit.distance - 0.05f));
        }

        bloqueoRuta = paso.sqrMagnitude < 0.000001f ? bloqueoRuta + Time.fixedDeltaTime : 0f;
        cuerpo.MovePosition(cuerpo.position + paso);
    }

    protected virtual float FactorVelocidadActual() => 1f;

    public void Detener()
    {
        direccionMovimiento = Vector3.zero;
        SetCaminando(false);
    }

    protected void ReiniciarBloqueo() { bloqueoRuta = 0f; }
    protected float Distancia => jugador == null ? float.PositiveInfinity : Vector3.Distance(transform.position, jugador.transform.position);

    protected bool VeJugador()
    {
        if (!EnJuego || !DentroTerritorio || Distancia > rangoVista) return false;

        Vector3 origenRayo = transform.position + Vector3.up * 0.55f;
        Vector3 destino = jugador.transform.position + Vector3.up * 0.55f;
        Vector3 dir = destino - origenRayo;

        int cantidad = Physics.RaycastNonAlloc(origenRayo, dir.normalized, bufferRayos, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
        {
            Collider col = bufferRayos[i].collider;
            if (col.GetComponentInParent<Enemy>() != this && col.GetComponentInParent<VidaJugador>() != jugador)
            {
                return false;
            }
        }
        return true;
    }

    protected void MoverEnDireccion(Vector3 dir, bool actualizarAnimacion = true)
    {
        dir.y = 0f;
        direccionMovimiento = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.zero;

        if (direccionMovimiento != Vector3.zero && bloqueoRuta > 0.35f)
        {
            Vector3 desvio = Quaternion.Euler(0f, 60f, 0f) * direccionMovimiento;
            if (cuerpo.SweepTest(desvio, out _, 0.48f, QueryTriggerInteraction.Ignore))
            {
                desvio = Quaternion.Euler(0f, -60f, 0f) * direccionMovimiento;
            }
            if (!cuerpo.SweepTest(desvio, out _, 0.48f, QueryTriggerInteraction.Ignore))
            {
                direccionMovimiento = desvio;
            }
        }

        if (actualizarAnimacion)
        {
            SetCaminando(direccionMovimiento.sqrMagnitude > 0f);
        }
    }

    protected void MoverHacia(Vector3 destino, bool actualizarAnimacion = true)
    {
        Vector3 dir = destino - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.02f)
        {
            Detener();
            return;
        }
        MoverEnDireccion(dir, actualizarAnimacion);
    }

    protected void MirarJugador()
    {
        Vector3 dir = jugador.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            cuerpo.MoveRotation(Quaternion.Slerp(cuerpo.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-12f * Time.deltaTime)));
        }
    }

    protected void Ruta(float distancia)
    {
        Vector3 destino = CalcularPuntoRuta(distancia);
        if (bloqueoRuta > 1f || Vector3.Distance(new Vector3(transform.position.x, origen.y, transform.position.z), destino) < 0.28f)
        {
            puntoActual = (puntoActual + 1) % 4;
            destino = CalcularPuntoRuta(distancia);
            bloqueoRuta = 0f;
        }
        MoverHacia(destino);
    }

    private Vector3 CalcularPuntoRuta(float distancia)
    {
        float x = (puntoActual == 0 || puntoActual == 3) ? -distancia : distancia;
        float z = puntoActual < 2 ? distancia : -distancia;
        return origen + new Vector3(x, 0f, z);
    }

    public virtual bool IntentarAtacar() => false;
    public virtual bool IntentarPerseguir() => false;
    public virtual bool Patrullar() { if (!EnJuego) return false; Ruta(1.8f); return true; }

    protected bool GolpeDeContacto(string nombreAudio)
    {
        if (!EnJuego || Time.time < siguienteAtaque) return false;
        siguienteAtaque = Time.time + 1.1f;
        jugador.RecibirDano(dano);
        ActivarAtaque();
        AudioManager.Reproducir(nombreAudio, transform.position);
        return true;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (jugador != null && collision.collider.GetComponentInParent<VidaJugador>() == jugador)
        {
            Contacto();
        }
    }

    protected virtual void Contacto() { }

    protected virtual int ModificarDanoRecibido(int cantidad) => cantidad;

    protected virtual void AlRecibirDano(int danoAplicado)
    {
        ActivarGolpe();
    }

    public void RecibirDano(int cantidad) => RecibirDano(cantidad, null);

    public void RecibirDano(int cantidad, Vector3? origenGolpe)
    {
        if (Muerto || cantidad <= 0) return;

        int danoFinal = Mathf.Max(1, ModificarDanoRecibido(cantidad));
        vida = Mathf.Max(0, vida - danoFinal);
        if (barraVida != null) barraVida.Actualizar(vida, vidaMaxima);
        AudioManager.Reproducir("Enemy_Hit", transform.position);
        ActivarFlashGolpe();

        if (vida > 0)
        {
            AlRecibirDano(danoFinal);
            AplicarRetroceso(origenGolpe);
            return;
        }

        Muerto = true;
        tiempoMuerte = Time.time;
        Detener();
        ActivarMuerte();
        if (agente != null) agente.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        AudioManager.Reproducir("Enemy_Death", transform.position);
        GenerarParticulasMuerte();
        Murio?.Invoke(this);
        Destroy(gameObject, TiempoDesaparicion);
    }

    private void AplicarRetroceso(Vector3? origenGolpe)
    {
        if (!origenGolpe.HasValue || cuerpo == null) return;
        Vector3 empuje = transform.position - origenGolpe.Value;
        empuje.y = 0f;
        if (empuje.sqrMagnitude <= 0.001f) return;

        Vector3 dir = empuje.normalized;
        float dist = 0.35f;
        if (cuerpo.SweepTest(dir, out RaycastHit hit, dist + 0.05f, QueryTriggerInteraction.Ignore))
        {
            dist = Mathf.Max(0f, hit.distance - 0.05f);
        }
        if (dist > 0.01f) cuerpo.MovePosition(cuerpo.position + dir * dist);
    }

    private void ActivarFlashGolpe()
    {
        if (mallas == null || mallas.Length == 0) return;
        flashActivo = true;
        flashHasta = Time.time + 0.14f;
        Color flash = new Color(1f, 0.35f, 0.25f, 1f);

        for (int i = 0; i < mallas.Length; i++)
        {
            if (mallas[i] == null) continue;
            mallas[i].GetPropertyBlock(bloqueFlash);
            bloqueFlash.SetColor(PropColor, flash);
            bloqueFlash.SetColor(PropBaseColor, flash);
            mallas[i].SetPropertyBlock(bloqueFlash);
        }
    }

    private void GenerarParticulasMuerte()
    {
        GameObject fx = new GameObject("FX_Muerte_" + name);
        fx.transform.position = transform.position + Vector3.up * 0.65f;
        ParticleSystem sistema = fx.AddComponent<ParticleSystem>();
        sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule principal = sistema.main;
        principal.duration = 0.6f;
        principal.loop = false;
        principal.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        principal.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 4.0f);
        principal.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
        principal.startColor = ColorParticulasMuerte;
        principal.gravityModifier = 0.8f;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        principal.maxParticles = 28;

        ParticleSystem.EmissionModule emision = sistema.emission;
        emision.rateOverTime = 0f;
        emision.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });

        ParticleSystem.ShapeModule forma = sistema.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.35f;

        ParticleSystem.SizeOverLifetimeModule tamano = sistema.sizeOverLifetime;
        tamano.enabled = true;
        AnimationCurve curva = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        tamano.size = new ParticleSystem.MinMaxCurve(1f, curva);

        if (materialParticulasCompartido == null)
        {
            Shader shaderParticula = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            materialParticulasCompartido = new Material(shaderParticula) { color = Color.white };
        }

        ParticleSystemRenderer renderParticulas = sistema.GetComponent<ParticleSystemRenderer>();
        renderParticulas.sharedMaterial = materialParticulasCompartido;

        sistema.Play();
        Destroy(fx, 1.5f);
    }

    public void EliminarAlCargar()
    {
        Muerto = true;
        Detener();
        gameObject.SetActive(false);
    }
}