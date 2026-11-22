using UnityEngine;

// Permite destruir barriles y anforas de la mazmorra con la espada para obtener botin
public class ObjetoRompible : MonoBehaviour
{
    [SerializeField] private int golpesParaRomper = 1;
    [SerializeField] private Color colorEscombros = new Color(0.62f, 0.42f, 0.24f);

    private int golpesRestantes;
    private bool roto;
    private float tiempoSacudida;
    private Vector3 posicionInicial;

    public bool Roto => roto;

    public void Configurar(int resistencia, Color colorParticulas)
    {
        golpesParaRomper = Mathf.Max(1, resistencia);
        golpesRestantes = golpesParaRomper;
        colorEscombros = colorParticulas;
    }

    private void Awake()
    {
        golpesRestantes = Mathf.Max(1, golpesParaRomper);
        posicionInicial = transform.position;
        AsegurarColision();
    }

    private void AsegurarColision()
    {
        bool tieneColliderActivo = false;
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
        {
            if (c.enabled && !c.isTrigger)
            {
                tieneColliderActivo = true;
                break;
            }
        }

        if (!tieneColliderActivo)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.45f, 0f);
            box.size = new Vector3(0.68f, 0.90f, 0.68f);
        }
    }

    private void Update()
    {
        if (tiempoSacudida > 0f)
        {
            tiempoSacudida = Mathf.Max(0f, tiempoSacudida - Time.deltaTime);
            transform.position = posicionInicial + Random.insideUnitSphere * (0.045f * (tiempoSacudida / 0.14f));
            if (tiempoSacudida <= 0f)
            {
                transform.position = posicionInicial;
            }
        }
    }

    public void RecibirGolpe(Vector3 origenGolpe, InventarioJugador inventario)
    {
        if (roto) return;

        golpesRestantes--;
        if (golpesRestantes > 0)
        {
            tiempoSacudida = 0.14f;
            EfectosVisuales.ChispasGolpe(transform.position + Vector3.up * 0.45f, false, false);
            AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.RomperObjeto, 0.45f, transform.position);
            return;
        }

        roto = true;
        EfectosVisuales.EscombrosRompible(transform.position, colorEscombros);
        AudioManager.ReproducirSintetico(AudioManager.SonidoSintetico.RomperObjeto, 0.72f, transform.position);
        inventario?.RegistrarObjetoRoto();

        // Suelta corazones, rupias o una pocion rara
        float azar = Random.value;
        if (azar < 0.38f)
        {
            ObjetoRecolectable.Crear(TipoRecolectable.Corazon, transform.position + Vector3.up * 0.4f);
        }
        else if (azar < 0.88f)
        {
            TipoRecolectable rupia = Random.value < 0.25f ? TipoRecolectable.RupiaAzul : TipoRecolectable.RupiaVerde;
            ObjetoRecolectable.Crear(rupia, transform.position + Vector3.up * 0.4f);
        }
        else
        {
            ObjetoRecolectable.Crear(TipoRecolectable.Pocion, transform.position + Vector3.up * 0.45f);
        }

        gameObject.SetActive(false);
    }
}
