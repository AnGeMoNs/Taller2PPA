using UnityEngine;

// Trampa de puas por ciclos: verde segura, ambar aviso y roja con dano
public class TrampaPulso : MonoBehaviour
{
    #region CLASS_VARIABLES

    [Header("Configuracion")]
    [SerializeField] private float desfase;
    [SerializeField] private int dano = 14;
    [SerializeField] private Transform grupoPinchos;
    [SerializeField] private float yPinchosOcultos = -0.34f;
    [SerializeField] private float yPinchosActivos = 0.08f;

    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");

    private Renderer visual;
    private BoxCollider zona;
    private float siguienteGolpe;
    private MaterialPropertyBlock bloque;
    private int ultimoEstado = -1;
    private float yPinchosActual;
    private readonly Collider[] bufferZona = new Collider[8];

    public bool Desactivada { get; set; }

    #endregion

    #region UNITY_METHODS

    private void Awake()
    {
        visual = GetComponent<Renderer>();
        zona = GetComponent<BoxCollider>();
        bloque = new MaterialPropertyBlock();

        if (grupoPinchos == null)
        {
            grupoPinchos = transform.Find("Pinchos");
        }
        yPinchosActual = yPinchosOcultos;

        if (grupoPinchos != null)
        {
            Vector3 pos = grupoPinchos.localPosition;
            pos.y = yPinchosOcultos;
            grupoPinchos.localPosition = pos;
        }
    }

    private void Update()
    {
        // Ciclo de 3.4s: peligro 0..1.25, segura 1.25..2.85, aviso 2.85..3.4
        float ciclo = Mathf.Repeat(Time.time + desfase, 3.4f);
        int estado = Desactivada ? 0 : (ciclo < 1.25f ? 2 : (ciclo >= 2.85f ? 1 : 0));

        if (estado != ultimoEstado)
        {
            ultimoEstado = estado;
            Color color = estado == 2
                ? new Color(0.88f, 0.14f, 0.10f)
                : (estado == 1 ? new Color(0.95f, 0.68f, 0.12f) : new Color(0.12f, 0.48f, 0.28f));

            if (visual != null)
            {
                visual.GetPropertyBlock(bloque);
                bloque.SetColor(PropColor, color);
                bloque.SetColor(PropBaseColor, color);
                visual.SetPropertyBlock(bloque);
            }
        }

        if (grupoPinchos != null)
        {
            float yObjetivo = estado == 2 ? yPinchosActivos : (estado == 1 ? Mathf.Lerp(yPinchosOcultos, yPinchosActivos, 0.28f) : yPinchosOcultos);
            float velocidadSubida = estado == 2 ? 24f : 10f;
            yPinchosActual = Mathf.Lerp(yPinchosActual, yObjetivo, 1f - Mathf.Exp(-velocidadSubida * Time.deltaTime));

            Vector3 pos = grupoPinchos.localPosition;
            pos.y = yPinchosActual;
            grupoPinchos.localPosition = pos;
        }
    }

    private void FixedUpdate()
    {
        if (Desactivada || ultimoEstado != 2 || Time.time < siguienteGolpe || GameManager.Instance == null || !GameManager.Instance.EnJuego)
            return;

        Bounds limites = zona.bounds;
        int encontrados = Physics.OverlapBoxNonAlloc(
            limites.center + Vector3.up * 0.25f,
            limites.extents + new Vector3(0f, 0.2f, 0f),
            bufferZona,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < encontrados; i++)
        {
            VidaJugador vida = bufferZona[i].GetComponentInParent<VidaJugador>();
            if (vida == null)
                continue;

            vida.RecibirDano(dano, null, transform.position);
            siguienteGolpe = Time.time + 0.85f;
            break;
        }
    }

    #endregion

    #region CLASS_METHODS

    public void Configurar(float offset, Transform pinchos = null)
    {
        desfase = offset;
        if (pinchos != null)
        {
            grupoPinchos = pinchos;
        }
    }

    #endregion
}
