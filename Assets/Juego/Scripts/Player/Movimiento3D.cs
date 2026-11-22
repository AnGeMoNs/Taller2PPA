using System;
using UnityEngine;

// Controla movimiento 3D, estamina, esquiva con invulnerabilidad, bloqueo/parry, combo x3 y ataque giratorio
[RequireComponent(typeof(Rigidbody))]
public class Movimiento3D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.8f;
    [SerializeField] private float runSpeed = 6.4f;
    [SerializeField] private float turnSpeed = 14f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

    private const float EstaminaMaxValor = 100f;
    private const float CostoEsquiva = 24f;
    private const float CostoGiro = 34f;
    private const float DuracionEsquiva = 0.28f;
    private const float CooldownEsquiva = 0.50f;
    private const float VelocidadEsquiva = 11.2f;
    private const float VentanaParry = 0.28f;
    private const float TiempoCargaGiro = 0.48f;
    private const float CooldownGiro = 1.05f;

    // Hashes precalculados para evitar comparar strings en cada frame
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashComboNext = Animator.StringToHash("ComboNext");
    private static readonly int HashBlocking = Animator.StringToHash("Blocking");
    private static readonly int HashHit = Animator.StringToHash("Hit");
    private static readonly int HashPowerUp = Animator.StringToHash("PowerUp");
    private static readonly int HashDeath = Animator.StringToHash("Death");

    private static readonly int EstadoIdle = Animator.StringToHash("Idle");
    private static readonly int EstadoWalk = Animator.StringToHash("Walk");
    private static readonly int EstadoRun = Animator.StringToHash("Run");
    private static readonly int EstadoLocomotion = Animator.StringToHash("Locomotion");
    private static readonly int EstadoBlock = Animator.StringToHash("Block");
    private static readonly int EstadoAttack = Animator.StringToHash("Attack");
    private static readonly int EstadoAttack1 = Animator.StringToHash("Attack1");
    private static readonly int EstadoAttack2 = Animator.StringToHash("Attack2");
    private static readonly int EstadoAttack3 = Animator.StringToHash("Attack3");
    private static readonly int EstadoHit = Animator.StringToHash("Hit");
    private static readonly int EstadoPowerUp = Animator.StringToHash("PowerUp");
    private static readonly int EstadoDeath = Animator.StringToHash("Death");

    private Rigidbody cuerpo;
    private SistemaFijadoObjetivo fijado;
    private Vector2 entrada;
    private bool corriendo;
    private bool bloqueando;
    private bool bloqueabaAntes;
    private bool accionBloqueada;
    private bool estaAtacando;
    private bool cursorBloqueadoAntes;

    private bool tieneParamSpeed;
    private bool tieneParamAttack;
    private bool tieneParamComboNext;
    private bool tieneParamBlocking;
    private bool tieneParamHit;
    private bool tieneParamPowerUp;
    private bool tieneParamDeath;

    private float siguientePaso;
    private bool ataqueSolicitado;
    private float horaSolicitudAtaque;
    private bool comboEnCola;
    private bool impactoEjecutado;
    private int pasoCombo;
    private int ultimoHashAtaque;

    // Estamina, esquiva, parry, giro y empuje
    private float estamina = EstaminaMaxValor;
    private float bloquearRegenHasta;
    private bool estaminaAgotada;
    private float esquivaHasta;
    private float siguienteEsquiva;
    private Vector3 direccionEsquiva;
    private float horaInicioBloqueo = -10f;
    private float empujeAtaqueHasta;
    private Vector3 direccionEmpujeAtaque;
    private Vector3 velocidadEmpujeExterno;
    private float tiempoCargaBoton;
    private bool giroCargadoDisparado;
    private float siguienteGiro;
    private float giroVisualHasta;
    private float ultimoEmitidoEstamina = -1f;

    public bool CanMove { get; set; } = true;
    public bool IsBlocking => bloqueando && !accionBloqueada && !EstaEsquivando && CanMove;
    public bool EstaAtacando => estaAtacando || Time.time < giroVisualHasta;
    public bool EstaEsquivando => Time.time < esquivaHasta;
    public bool EstaCorriendo => corriendo && entrada.sqrMagnitude > 0.05f && !accionBloqueada && !EstaEsquivando && CanMove;
    public int ComboPaso => pasoCombo;
    public float Estamina => estamina;
    public float EstaminaMaxima => EstaminaMaxValor;
    public bool EstaminaAgotada => estaminaAgotada;
    public float CargaGiroProgreso => Mathf.Clamp01(tiempoCargaBoton / TiempoCargaGiro);

    public event Action AttackStarted;
    public event Action AttackImpact;
    public event Action SpinAttackExecuted;
    public event Action<float, float> EstaminaCambiada;

    private void Start()
    {
        cuerpo = GetComponent<Rigidbody>();
        cuerpo.constraints |= RigidbodyConstraints.FreezeRotation;
        cuerpo.angularVelocity = Vector3.zero;
        fijado = GetComponent<SistemaFijadoObjetivo>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        RevisarParametrosAnimator();
        NotificarEstamina(true);
    }

    private void RevisarParametrosAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        animator.applyRootMotion = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == HashSpeed && param.type == AnimatorControllerParameterType.Float) tieneParamSpeed = true;
            else if (param.nameHash == HashAttack && param.type == AnimatorControllerParameterType.Trigger) tieneParamAttack = true;
            else if (param.nameHash == HashComboNext && param.type == AnimatorControllerParameterType.Trigger) tieneParamComboNext = true;
            else if (param.nameHash == HashBlocking && param.type == AnimatorControllerParameterType.Bool) tieneParamBlocking = true;
            else if (param.nameHash == HashHit && param.type == AnimatorControllerParameterType.Trigger) tieneParamHit = true;
            else if (param.nameHash == HashPowerUp && param.type == AnimatorControllerParameterType.Trigger) tieneParamPowerUp = true;
            else if (param.nameHash == HashDeath && param.type == AnimatorControllerParameterType.Trigger) tieneParamDeath = true;
        }
    }

    private void Update()
    {
        if (fijado == null) fijado = GetComponent<SistemaFijadoObjetivo>();

        LeerMovimiento();
        ActualizarEstamina();
        ActualizarAcciones();
        ActualizarAnimator();

        // Giro visual del cuerpo durante el ataque giratorio
        if (Time.time < giroVisualHasta && cuerpo != null)
        {
            transform.Rotate(0f, 1080f * Time.deltaTime, 0f, Space.World);
        }

        // Sonido de pasos con ligera variacion de tono segun ritmo
        if (CanMove && !accionBloqueada && !EstaEsquivando && entrada.sqrMagnitude > 0.01f && Time.time >= siguientePaso)
        {
            float intervalo = bloqueando ? 0.56f : (corriendo ? 0.27f : 0.43f);
            siguientePaso = Time.time + intervalo;
            AudioManager.Reproducir("Player_Footstep", null, corriendo ? 0.72f : 0.52f, corriendo ? 1.12f : 0.96f, 0.08f);
        }
    }

    private void FixedUpdate()
    {
        AplicarMovimiento();
    }

    private void LeerMovimiento()
    {
        entrada = Vector2.zero;
        corriendo = false;
        bloqueando = false;

        if (!CanMove || Cursor.lockState != CursorLockMode.Locked)
        {
            comboEnCola = false;
            ataqueSolicitado = false;
            tiempoCargaBoton = 0f;
            bloqueabaAntes = false;
            return;
        }

        if (Input.GetKey(KeyCode.W)) entrada.y += 1f;
        if (Input.GetKey(KeyCode.S)) entrada.y -= 1f;
        if (Input.GetKey(KeyCode.D)) entrada.x += 1f;
        if (Input.GetKey(KeyCode.A)) entrada.x -= 1f;
        entrada = Vector2.ClampMagnitude(entrada, 1f);

        bool botonBloqueo = tieneParamBlocking && Input.GetMouseButton(1) && !estaminaAgotada;
        bloqueando = botonBloqueo;

        if (bloqueando && !bloqueabaAntes)
        {
            horaInicioBloqueo = Time.time;
        }
        bloqueabaAntes = bloqueando;

        bool quiereCorrer = !bloqueando && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        corriendo = quiereCorrer && !estaminaAgotada && estamina > 2f;
    }

    private void ActualizarEstamina()
    {
        if (!CanMove) return;

        if (EstaCorriendo)
        {
            estamina = Mathf.Max(0f, estamina - 16f * Time.deltaTime);
            bloquearRegenHasta = Time.time + 0.55f;
            if (estamina <= 0.1f) estaminaAgotada = true;
        }
        else if (Time.time >= bloquearRegenHasta && !EstaEsquivando)
        {
            float tasa = bloqueando ? 11f : 28f;
            estamina = Mathf.Min(EstaminaMaxValor, estamina + tasa * Time.deltaTime);
            if (estaminaAgotada && estamina >= 25f)
            {
                estaminaAgotada = false;
            }
        }

        NotificarEstamina(false);
    }

    public bool ConsumirEstamina(float cantidad)
    {
        if (cantidad <= 0f) return true;
        if (estamina < cantidad * 0.55f) return false;
        estamina = Mathf.Max(0f, estamina - cantidad);
        bloquearRegenHasta = Time.time + 0.72f;
        if (estamina <= 0.1f) estaminaAgotada = true;
        NotificarEstamina(true);
        return true;
    }

    public void RecuperarEstamina(float cantidad)
    {
        if (cantidad <= 0f) return;
        estamina = Mathf.Min(EstaminaMaxValor, estamina + cantidad);
        if (estamina >= 20f) estaminaAgotada = false;
        NotificarEstamina(true);
    }

    public void RestaurarEstaminaCompleta()
    {
        estamina = EstaminaMaxValor;
        estaminaAgotada = false;
        NotificarEstamina(true);
    }

    private void NotificarEstamina(bool forzar)
    {
        if (forzar || Mathf.Abs(estamina - ultimoEmitidoEstamina) >= 0.5f)
        {
            ultimoEmitidoEstamina = estamina;
            EstaminaCambiada?.Invoke(estamina, EstaminaMaxValor);
        }
    }

    public bool EsParryPerfecto(Vector3? origenDano)
    {
        if (!IsBlocking || Time.time - horaInicioBloqueo > VentanaParry) return false;

        if (origenDano.HasValue)
        {
            Vector3 haciaAtacante = origenDano.Value - transform.position;
            haciaAtacante.y = 0f;
            if (haciaAtacante.sqrMagnitude > 0.01f)
            {
                // Cono generoso frontal de ~125 grados para que el parry se sienta justo y preciso
                float dot = Vector3.Dot(transform.forward, haciaAtacante.normalized);
                if (dot < -0.15f) return false;
            }
        }

        return true;
    }

    public void AplicarEmpuje(Vector3 direccion, float fuerza)
    {
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.001f) return;
        velocidadEmpujeExterno = direccion.normalized * fuerza;
    }

    private static bool EsEstadoAtaque(AnimatorStateInfo estado)
    {
        int hash = estado.shortNameHash;
        return hash == EstadoAttack || hash == EstadoAttack1 || hash == EstadoAttack2 || hash == EstadoAttack3;
    }

    private static bool EsEstadoLocomocion(AnimatorStateInfo estado)
    {
        int hash = estado.shortNameHash;
        return hash == EstadoIdle || hash == EstadoWalk || hash == EstadoRun || hash == EstadoLocomotion || hash == EstadoBlock;
    }

    private static bool EsEstadoEspecial(AnimatorStateInfo estado)
    {
        int hash = estado.shortNameHash;
        return hash == EstadoPowerUp || hash == EstadoHit || hash == EstadoDeath;
    }

    private static int ObtenerPasoCombo(AnimatorStateInfo estado)
    {
        if (estado.shortNameHash == EstadoAttack3) return 3;
        if (estado.shortNameHash == EstadoAttack2) return 2;
        return 1;
    }

    private void ActualizarAcciones()
    {
        bool cursorBloqueado = Cursor.lockState == CursorLockMode.Locked;
        accionBloqueada = false;
        estaAtacando = false;

        bool puedeCancelarConEsquiva = false;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimatorStateInfo actual = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo siguiente = animator.GetNextAnimatorStateInfo(0);
            bool enTransicion = animator.IsInTransition(0);

            bool atacandoAhora = EsEstadoAtaque(actual);
            bool atacandoSiguiente = enTransicion && EsEstadoAtaque(siguiente);
            bool enAtaque = atacandoAhora || atacandoSiguiente;
            bool enEspecial = EsEstadoEspecial(actual) || (enTransicion && EsEstadoEspecial(siguiente));

            if (atacandoAhora)
            {
                ataqueSolicitado = false;
                pasoCombo = ObtenerPasoCombo(actual);
                puedeCancelarConEsquiva = actual.normalizedTime >= 0.52f;

                if (actual.fullPathHash != ultimoHashAtaque)
                {
                    ultimoHashAtaque = actual.fullPathHash;
                    impactoEjecutado = false;
                    comboEnCola = false;
                    if (pasoCombo > 1)
                    {
                        OrientarHaciaAtaque();
                        IniciarEmpujeAtaque(pasoCombo == 3 ? 0.20f : 0.14f);
                        AttackStarted?.Invoke();
                    }
                }
            }
            else if (!enAtaque)
            {
                ultimoHashAtaque = 0;
                impactoEjecutado = false;
                comboEnCola = false;
                puedeCancelarConEsquiva = !enEspecial;
            }

            if (ataqueSolicitado && Time.time - horaSolicitudAtaque > 0.32f)
            {
                ataqueSolicitado = false;
            }

            estaAtacando = enAtaque;
            accionBloqueada = enAtaque || enEspecial || ataqueSolicitado || Time.time < giroVisualHasta;
            if (accionBloqueada) bloqueando = false;

            // Impacto en el punto medio del corte de espada
            if (CanMove && !impactoEjecutado && atacandoAhora && actual.normalizedTime >= 0.36f)
            {
                impactoEjecutado = true;
                AttackImpact?.Invoke();
            }

            // Esquiva rapida con Espacio (con i-frames y cancelacion tardia de ataque)
            if (CanMove && cursorBloqueado && Input.GetKeyDown(KeyCode.Space)
                && Time.time >= siguienteEsquiva && !enEspecial && puedeCancelarConEsquiva)
            {
                IntentarEsquivar();
            }

            // Ataque Giratorio con tecla R o manteniendo clic izquierdo
            if (CanMove && cursorBloqueado && cursorBloqueadoAntes && !enEspecial && !EstaEsquivando)
            {
                if (Input.GetKeyDown(KeyCode.R) && !accionBloqueada)
                {
                    IntentarAtaqueGiratorio();
                }

                if (Input.GetMouseButton(0) && !accionBloqueada)
                {
                    tiempoCargaBoton += Time.deltaTime;
                    if (tiempoCargaBoton >= TiempoCargaGiro && !giroCargadoDisparado)
                    {
                        giroCargadoDisparado = true;
                        IntentarAtaqueGiratorio();
                    }
                }
                else if (!Input.GetMouseButton(0))
                {
                    tiempoCargaBoton = 0f;
                    giroCargadoDisparado = false;
                }
            }

            // Encadena combo 1 -> 2 -> 3
            if (CanMove && cursorBloqueado && cursorBloqueadoAntes && atacandoAhora && !atacandoSiguiente
                && tieneParamComboNext && (actual.shortNameHash == EstadoAttack1 || actual.shortNameHash == EstadoAttack2)
                && actual.normalizedTime >= 0.22f && Input.GetMouseButtonDown(0))
            {
                comboEnCola = true;
            }

            if (comboEnCola && atacandoAhora && !enTransicion && actual.normalizedTime >= 0.50f)
            {
                comboEnCola = false;
                animator.SetTrigger(HashComboNext);
            }

            // Primer golpe del combo
            if (CanMove && cursorBloqueado && cursorBloqueadoAntes && !accionBloqueada && !EstaEsquivando
                && EsEstadoLocomocion(actual) && tieneParamAttack && Input.GetMouseButtonDown(0))
            {
                pasoCombo = 1;
                comboEnCola = false;
                if (tieneParamBlocking) animator.SetBool(HashBlocking, false);
                if (tieneParamComboNext) animator.ResetTrigger(HashComboNext);

                OrientarHaciaAtaque();
                IniciarEmpujeAtaque(0.14f);
                animator.SetTrigger(HashAttack);
                ataqueSolicitado = true;
                horaSolicitudAtaque = Time.time;
                accionBloqueada = true;
                estaAtacando = true;
                bloqueando = false;
                impactoEjecutado = false;
                AttackStarted?.Invoke();
            }
        }

        cursorBloqueadoAntes = cursorBloqueado;
    }

    private void IntentarEsquivar()
    {
        if (!ConsumirEstamina(CostoEsquiva))
        {
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 1.9f, "SIN ESTAMINA", new Color(1f, 0.65f, 0.25f), 0.85f);
            return;
        }

        Vector3 dir = CalcularDireccionEntrada();
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.y = 0f;
        direccionEsquiva = dir.normalized;

        esquivaHasta = Time.time + DuracionEsquiva;
        siguienteEsquiva = Time.time + CooldownEsquiva;
        comboEnCola = false;
        ataqueSolicitado = false;
        bloqueando = false;

        if (cuerpo != null)
        {
            cuerpo.MoveRotation(Quaternion.LookRotation(direccionEsquiva));
        }

        EfectosVisuales.PolvoEsquiva(transform.position, direccionEsquiva);
        AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.Esquiva, 0.72f);
    }

    private void IntentarAtaqueGiratorio()
    {
        if (Time.time < siguienteGiro) return;
        if (!ConsumirEstamina(CostoGiro))
        {
            EfectosVisuales.TextoFlotante(transform.position + Vector3.up * 1.9f, "SIN ESTAMINA", new Color(1f, 0.65f, 0.25f), 0.85f);
            return;
        }

        siguienteGiro = Time.time + CooldownGiro;
        giroVisualHasta = Time.time + 0.34f;
        accionBloqueada = true;
        estaAtacando = true;
        bloqueando = false;

        if (animator != null && tieneParamAttack)
        {
            animator.SetTrigger(HashAttack);
            impactoEjecutado = true; // Evita doble golpe normal durante el giro
        }

        SpinAttackExecuted?.Invoke();
    }

    private void IniciarEmpujeAtaque(float duracion)
    {
        empujeAtaqueHasta = Time.time + duracion;
        direccionEmpujeAtaque = ObtenerDireccionObjetivoAtaque();
    }

    private Vector3 CalcularDireccionEntrada()
    {
        Vector3 frente = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 0.001f) frente = Vector3.forward;
        frente.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, frente);
        return frente * entrada.y + derecha * entrada.x;
    }

    private Vector3 ObtenerDireccionObjetivoAtaque()
    {
        // Si hay objetivo fijado con Z-Targeting, orienta el ataque hacia el enemigo
        if (fijado != null && fijado.TieneObjetivo)
        {
            Vector3 haciaObjetivo = fijado.ObjetivoActual.transform.position - transform.position;
            haciaObjetivo.y = 0f;
            if (haciaObjetivo.sqrMagnitude > 0.01f)
            {
                return haciaObjetivo.normalized;
            }
        }

        Vector3 dirEntrada = CalcularDireccionEntrada();
        if (dirEntrada.sqrMagnitude > 0.05f) return dirEntrada.normalized;

        Vector3 frente = cameraTransform != null ? cameraTransform.forward : transform.forward;
        frente.y = 0f;
        return frente.sqrMagnitude > 0.001f ? frente.normalized : transform.forward;
    }

    private void OrientarHaciaAtaque()
    {
        Vector3 direccion = ObtenerDireccionObjetivoAtaque();
        if (direccion.sqrMagnitude > 0.001f && cuerpo != null)
        {
            cuerpo.MoveRotation(Quaternion.LookRotation(direccion));
        }
    }

    private void ActualizarAnimator()
    {
        if (animator == null) return;

        if (tieneParamBlocking)
        {
            animator.SetBool(HashBlocking, bloqueando && !accionBloqueada && !EstaEsquivando && CanMove);
        }

        if (!tieneParamSpeed) return;

        float velocidadAnim;
        if (EstaEsquivando)
        {
            velocidadAnim = 2.0f;
        }
        else if (accionBloqueada || !CanMove)
        {
            velocidadAnim = 0f;
        }
        else
        {
            velocidadAnim = entrada.magnitude * (bloqueando ? 0.5f : (corriendo ? 2f : 1f));
        }
        animator.SetFloat(HashSpeed, velocidadAnim, 0.08f, Time.deltaTime);
    }

    private void AplicarMovimiento()
    {
        Vector3 velHorizontal;

        if (EstaEsquivando && CanMove)
        {
            float factorCurva = Mathf.Clamp01((esquivaHasta - Time.time) / DuracionEsquiva);
            velHorizontal = direccionEsquiva * (VelocidadEsquiva * Mathf.Lerp(0.55f, 1.15f, factorCurva));
        }
        else if (Time.time < empujeAtaqueHasta && CanMove)
        {
            // Sutil avance durante el golpe de espada para conectar mejor los combos
            velHorizontal = direccionEmpujeAtaque * 2.1f;
        }
        else
        {
            Vector3 direccion = accionBloqueada || !CanMove ? Vector3.zero : CalcularDireccionEntrada();
            float velocidadActual = bloqueando ? moveSpeed * 0.45f : (corriendo ? runSpeed : moveSpeed);
            velHorizontal = direccion * velocidadActual;

            if (direccion.sqrMagnitude > 0.001f)
            {
                Quaternion objetivo = Quaternion.LookRotation(direccion.normalized);
                cuerpo.MoveRotation(Quaternion.Slerp(cuerpo.rotation, objetivo, 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime)));
            }
            else if ((bloqueando || (fijado != null && fijado.TieneObjetivo)) && !accionBloqueada && CanMove)
            {
                Vector3 mirar = ObtenerDireccionObjetivoAtaque();
                if (mirar.sqrMagnitude > 0.001f)
                {
                    Quaternion objetivo = Quaternion.LookRotation(mirar);
                    cuerpo.MoveRotation(Quaternion.Slerp(cuerpo.rotation, objetivo, 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime)));
                }
            }
        }

        if (velocidadEmpujeExterno.sqrMagnitude > 0.01f)
        {
            velHorizontal += velocidadEmpujeExterno;
            velocidadEmpujeExterno = Vector3.Lerp(velocidadEmpujeExterno, Vector3.zero, 1f - Mathf.Exp(-14f * Time.fixedDeltaTime));
        }

        cuerpo.linearVelocity = new Vector3(velHorizontal.x, cuerpo.linearVelocity.y, velHorizontal.z);
        cuerpo.angularVelocity = Vector3.zero;
    }

    public void ReproducirGolpe()
    {
        if (animator != null && tieneParamHit && !accionBloqueada && !EstaEsquivando)
        {
            animator.SetTrigger(HashHit);
        }
    }

    public void ReproducirPowerUp()
    {
        if (animator != null && tieneParamPowerUp)
        {
            animator.SetTrigger(HashPowerUp);
        }
    }

    public void ReproducirMuerte()
    {
        CanMove = false;
        bloqueando = false;
        esquivaHasta = 0f;
        if (cuerpo != null) cuerpo.linearVelocity = Vector3.zero;
        if (animator != null && tieneParamDeath)
        {
            animator.SetTrigger(HashDeath);
        }
    }
}
