using UnityEngine;

// Controla el movimiento 3D, rotacion suave, bloqueo con escudo y combo de espada
[RequireComponent(typeof(Rigidbody))]
public class Movimiento3D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float turnSpeed = 14f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

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
    private Vector2 entrada;
    private bool corriendo;
    private bool bloqueando;
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

    public bool CanMove { get; set; } = true;
    public bool IsBlocking => bloqueando && !accionBloqueada && CanMove;
    public bool EstaAtacando => estaAtacando;
    public int ComboPaso => pasoCombo;
    public event System.Action AttackStarted;
    public event System.Action AttackImpact;

    private void Start()
    {
        cuerpo = GetComponent<Rigidbody>();
        cuerpo.constraints |= RigidbodyConstraints.FreezeRotation;
        cuerpo.angularVelocity = Vector3.zero;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        RevisarParametrosAnimator();
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
        LeerMovimiento();
        ActualizarAcciones();
        ActualizarAnimator();

        // Sonido de pasos segun ritmo de caminata, carrera o defensa
        if (CanMove && !accionBloqueada && entrada.sqrMagnitude > 0.01f && Time.time >= siguientePaso)
        {
            float intervalo = bloqueando ? 0.56f : (corriendo ? 0.28f : 0.44f);
            siguientePaso = Time.time + intervalo;
            AudioManager.Reproducir("Player_Footstep");
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
            return;
        }

        if (Input.GetKey(KeyCode.W)) entrada.y += 1f;
        if (Input.GetKey(KeyCode.S)) entrada.y -= 1f;
        if (Input.GetKey(KeyCode.D)) entrada.x += 1f;
        if (Input.GetKey(KeyCode.A)) entrada.x -= 1f;
        entrada = Vector2.ClampMagnitude(entrada, 1f);

        bloqueando = tieneParamBlocking && Input.GetMouseButton(1);
        corriendo = !bloqueando && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
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
                if (actual.fullPathHash != ultimoHashAtaque)
                {
                    ultimoHashAtaque = actual.fullPathHash;
                    impactoEjecutado = false;
                    comboEnCola = false;
                    if (pasoCombo > 1)
                    {
                        OrientarHaciaAtaque();
                        AttackStarted?.Invoke();
                    }
                }
            }
            else if (!enAtaque)
            {
                ultimoHashAtaque = 0;
                impactoEjecutado = false;
                comboEnCola = false;
            }

            // Limpia solicitudes viejas si una transicion se interrumpe
            if (ataqueSolicitado && Time.time - horaSolicitudAtaque > 0.32f)
            {
                ataqueSolicitado = false;
            }

            estaAtacando = enAtaque;
            accionBloqueada = enAtaque || enEspecial || ataqueSolicitado;
            if (accionBloqueada) bloqueando = false;

            // Lanza el impacto justo en el punto medio del corte de espada
            if (CanMove && !impactoEjecutado && atacandoAhora && actual.normalizedTime >= 0.36f)
            {
                impactoEjecutado = true;
                AttackImpact?.Invoke();
            }

            // Guarda el clic para encadenar golpe 1 -> 2 -> 3 de forma fluida
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

            // Inicio del primer golpe desde movimiento normal o bloqueo
            if (CanMove && cursorBloqueado && cursorBloqueadoAntes && !accionBloqueada
                && EsEstadoLocomocion(actual) && tieneParamAttack && Input.GetMouseButtonDown(0))
            {
                pasoCombo = 1;
                comboEnCola = false;
                if (tieneParamBlocking) animator.SetBool(HashBlocking, false);
                if (tieneParamComboNext) animator.ResetTrigger(HashComboNext);

                OrientarHaciaAtaque();
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

    private void OrientarHaciaAtaque()
    {
        Vector3 frente = cameraTransform != null ? cameraTransform.forward : transform.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 0.001f) return;

        frente.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, frente);
        Vector3 direccion = entrada.sqrMagnitude > 0.05f ? (frente * entrada.y + derecha * entrada.x) : frente;

        if (direccion.sqrMagnitude > 0.001f)
        {
            cuerpo.MoveRotation(Quaternion.LookRotation(direccion.normalized));
        }
    }

    private void ActualizarAnimator()
    {
        if (animator == null) return;

        if (tieneParamBlocking)
        {
            animator.SetBool(HashBlocking, bloqueando && !accionBloqueada && CanMove);
        }

        if (!tieneParamSpeed) return;

        float velocidadAnim = accionBloqueada || !CanMove ? 0f : entrada.magnitude * (bloqueando ? 0.5f : (corriendo ? 2f : 1f));
        animator.SetFloat(HashSpeed, velocidadAnim, 0.08f, Time.deltaTime);
    }

    private void AplicarMovimiento()
    {
        Vector3 frente = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        frente.y = 0f;
        frente.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, frente);

        Vector3 direccion = accionBloqueada || !CanMove ? Vector3.zero : frente * entrada.y + derecha * entrada.x;
        float velocidadActual = bloqueando ? moveSpeed * 0.42f : (corriendo ? runSpeed : moveSpeed);

        cuerpo.linearVelocity = new Vector3(direccion.x * velocidadActual, cuerpo.linearVelocity.y, direccion.z * velocidadActual);
        cuerpo.angularVelocity = Vector3.zero;

        if (direccion.sqrMagnitude > 0.001f)
        {
            Quaternion objetivo = Quaternion.LookRotation(direccion.normalized);
            cuerpo.MoveRotation(Quaternion.Slerp(cuerpo.rotation, objetivo, 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime)));
        }
        else if (bloqueando && !accionBloqueada && CanMove && frente.sqrMagnitude > 0.001f)
        {
            Quaternion objetivo = Quaternion.LookRotation(frente);
            cuerpo.MoveRotation(Quaternion.Slerp(cuerpo.rotation, objetivo, 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime)));
        }
    }

    public void ReproducirGolpe()
    {
        if (animator != null && tieneParamHit && !accionBloqueada)
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
        if (cuerpo != null) cuerpo.linearVelocity = Vector3.zero;
        if (animator != null && tieneParamDeath)
        {
            animator.SetTrigger(HashDeath);
        }
    }
}
