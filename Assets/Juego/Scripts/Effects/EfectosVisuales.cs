using UnityEngine;

// Utilidad para generar particulas, anillos y textos flotantes 3D reutilizando materiales
public static class EfectosVisuales
{
    #region CLASS_VARIABLES

    private static Material materialParticula;
    private static Material materialAnillo;
    private static Font fuenteDefecto;

    #endregion

    #region CLASS_METHODS

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstaticos()
    {
        materialParticula = null;
        materialAnillo = null;
        fuenteDefecto = null;
    }

    private static Material ObtenerMaterialParticula()
    {
        if (materialParticula == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            materialParticula = new Material(shader) { color = Color.white };
        }
        return materialParticula;
    }

    private static Material ObtenerMaterialAnillo()
    {
        if (materialAnillo == null)
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            materialAnillo = new Material(shader) { color = Color.white };
        }
        return materialAnillo;
    }

    public static void ChispasGolpe(Vector3 posicion, bool critico, bool bloqueado)
    {
        Color color = bloqueado
            ? new Color(0.45f, 0.82f, 1f)
            : (critico ? new Color(1f, 0.85f, 0.22f) : new Color(1f, 0.56f, 0.22f));

        int cantidad = critico ? 22 : (bloqueado ? 14 : 12);
        float velocidadMin = critico ? 2.6f : 1.8f;
        float velocidadMax = critico ? 5.6f : 3.8f;
        CrearEstallido("FX_Golpe", posicion, color, cantidad, velocidadMin, velocidadMax, 0.06f, critico ? 0.16f : 0.11f, 0.35f, 0.55f);
    }

    public static void OndaParry(Vector3 posicion)
    {
        Color oro = new Color(1f, 0.90f, 0.34f);
        CrearEstallido("FX_Parry", posicion, oro, 28, 3.0f, 6.2f, 0.07f, 0.16f, 0.42f, 0.2f);
        CrearAnilloExpansivo(posicion, oro, 0.3f, 2.3f, 0.28f);
    }

    public static void OndaAtaqueGiratorio(Vector3 posicion, bool potenciado)
    {
        Color color = potenciado ? new Color(1f, 0.84f, 0.24f) : new Color(0.42f, 0.88f, 1f);
        CrearEstallido("FX_Giro", posicion + Vector3.up * 0.8f, color, 34, 3.6f, 6.5f, 0.08f, 0.18f, 0.45f, 0.08f);
        CrearAnilloExpansivo(posicion + Vector3.up * 0.75f, color, 0.45f, 3.2f, 0.32f);
    }

    public static void PolvoEsquiva(Vector3 posicion, Vector3 direccion)
    {
        Color polvo = new Color(0.76f, 0.72f, 0.64f, 0.85f);
        CrearEstallido("FX_Esquiva", posicion + Vector3.up * 0.15f - direccion.normalized * 0.2f, polvo, 14, 1.0f, 2.6f, 0.09f, 0.18f, 0.40f, 0.15f);
    }

    public static void Curacion(Vector3 posicion)
    {
        Color verde = new Color(0.32f, 0.96f, 0.56f);
        CrearEstallido("FX_Curacion", posicion + Vector3.up * 0.8f, verde, 20, 1.2f, 2.8f, 0.07f, 0.14f, 0.65f, -0.45f);
        CrearAnilloExpansivo(posicion + Vector3.up * 0.12f, verde, 0.35f, 1.65f, 0.45f);
    }

    public static void RecogerObjeto(Vector3 posicion, Color color)
    {
        CrearEstallido("FX_Recoger", posicion, color, 16, 1.4f, 3.4f, 0.06f, 0.13f, 0.45f, -0.25f);
    }

    public static void EscombrosRompible(Vector3 posicion, Color color)
    {
        CrearEstallido("FX_Escombros", posicion + Vector3.up * 0.4f, color, 20, 2.0f, 4.6f, 0.09f, 0.20f, 0.65f, 1.15f);
    }

    public static void PortalInvocacion(Vector3 posicion)
    {
        Color purpura = new Color(0.78f, 0.32f, 1f);
        CrearEstallido("FX_Portal", posicion + Vector3.up * 0.4f, purpura, 26, 1.8f, 4.2f, 0.08f, 0.18f, 0.75f, -0.3f);
        CrearAnilloExpansivo(posicion + Vector3.up * 0.08f, purpura, 0.25f, 2.0f, 0.55f);
    }

    public static void EstrellasAturdimiento(Transform objetivo, float duracion)
    {
        if (objetivo == null)
            return;

        GameObject fx = new GameObject("FX_Aturdido");
        fx.transform.SetParent(objetivo, false);
        fx.transform.localPosition = Vector3.up * 1.95f;

        EstrellasOrbitantes orbita = fx.AddComponent<EstrellasOrbitantes>();
        orbita.Inicializar(duracion, ObtenerMaterialAnillo());
    }

    public static void TextoFlotante(Vector3 posicion, string texto, Color color, float escala = 1f)
    {
        if (string.IsNullOrEmpty(texto))
            return;

        if (fuenteDefecto == null)
        {
            fuenteDefecto = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        GameObject go = new GameObject("TextoFlotante");
        go.transform.position = posicion + new Vector3(Random.Range(-0.18f, 0.18f), 0.15f, Random.Range(-0.18f, 0.18f));

        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.font = fuenteDefecto;
        mesh.text = texto;
        mesh.fontSize = 44;
        mesh.characterSize = 0.036f * escala;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontStyle = FontStyle.Bold;
        mesh.color = color;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && mesh.font != null)
        {
            renderer.sharedMaterial = mesh.font.material;
        }

        TextoFlotanteComportamiento comp = go.AddComponent<TextoFlotanteComportamiento>();
        comp.Inicializar(mesh, color, escala);
    }

    private static void CrearEstallido(
        string nombre,
        Vector3 posicion,
        Color color,
        int cantidad,
        float velMin,
        float velMax,
        float tamMin,
        float tamMax,
        float vidaMax,
        float gravedad)
    {
        GameObject fx = new GameObject(nombre);
        fx.transform.position = posicion;

        ParticleSystem sistema = fx.AddComponent<ParticleSystem>();
        sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule principal = sistema.main;
        principal.duration = vidaMax;
        principal.loop = false;
        principal.startLifetime = new ParticleSystem.MinMaxCurve(vidaMax * 0.55f, vidaMax);
        principal.startSpeed = new ParticleSystem.MinMaxCurve(velMin, velMax);
        principal.startSize = new ParticleSystem.MinMaxCurve(tamMin, tamMax);
        principal.startColor = color;
        principal.gravityModifier = gravedad;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        principal.maxParticles = cantidad + 6;

        ParticleSystem.EmissionModule emision = sistema.emission;
        emision.rateOverTime = 0f;
        emision.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        ParticleSystem.ShapeModule forma = sistema.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.22f;

        ParticleSystem.SizeOverLifetimeModule tamano = sistema.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        ParticleSystemRenderer render = sistema.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = ObtenerMaterialParticula();

        sistema.Play();
        Object.Destroy(fx, vidaMax + 0.6f);
    }

    private static void CrearAnilloExpansivo(Vector3 posicion, Color color, float escalaInicial, float escalaFinal, float duracion)
    {
        GameObject anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anillo.name = "FX_Anillo";

        Collider col = anillo.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        anillo.transform.position = posicion;
        anillo.transform.localScale = new Vector3(escalaInicial, 0.02f, escalaInicial);

        Renderer render = anillo.GetComponent<Renderer>();
        render.sharedMaterial = ObtenerMaterialAnillo();
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;

        AnilloExpansivo comp = anillo.AddComponent<AnilloExpansivo>();
        comp.Inicializar(render, color, escalaInicial, escalaFinal, duracion);
    }

    #endregion

    private sealed class AnilloExpansivo : MonoBehaviour
    {
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");

        private Renderer render;
        private MaterialPropertyBlock bloque;
        private Color colorBase;
        private float inicio;
        private float fin;
        private float duracion;
        private float tiempo;

        public void Inicializar(Renderer r, Color color, float escalaInicio, float escalaFin, float dur)
        {
            render = r;
            colorBase = color;
            inicio = escalaInicio;
            fin = escalaFin;
            duracion = Mathf.Max(0.05f, dur);
            bloque = new MaterialPropertyBlock();
            AplicarColor(1f);
        }

        private void Update()
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            float curva = 1f - (1f - t) * (1f - t);
            float escala = Mathf.Lerp(inicio, fin, curva);

            transform.localScale = new Vector3(escala, Mathf.Lerp(0.035f, 0.004f, t), escala);
            AplicarColor(1f - t);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void AplicarColor(float factor)
        {
            if (render == null)
                return;

            Color c = colorBase * Mathf.Lerp(0.25f, 1.2f, factor);
            c.a = factor;
            render.GetPropertyBlock(bloque);
            bloque.SetColor(PropColor, c);
            bloque.SetColor(PropBaseColor, c);
            render.SetPropertyBlock(bloque);
        }
    }

    private sealed class EstrellasOrbitantes : MonoBehaviour
    {
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");

        private float restante;
        private readonly Transform[] estrellas = new Transform[3];

        public void Inicializar(float duracion, Material mat)
        {
            restante = duracion;
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            Color amarillo = new Color(1f, 0.90f, 0.22f);
            bloque.SetColor(PropColor, amarillo);
            bloque.SetColor(PropBaseColor, amarillo);

            for (int i = 0; i < estrellas.Length; i++)
            {
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Collider col = s.GetComponent<Collider>();
                if (col != null) Destroy(col);

                s.transform.SetParent(transform, false);
                s.transform.localScale = Vector3.one * 0.11f;
                Renderer r = s.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.SetPropertyBlock(bloque);
                estrellas[i] = s.transform;
            }
        }

        private void Update()
        {
            restante -= Time.deltaTime;
            if (restante <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float anguloBase = Time.time * 5.2f;
            for (int i = 0; i < estrellas.Length; i++)
            {
                if (estrellas[i] == null)
                    continue;

                float a = anguloBase + i * (Mathf.PI * 2f / estrellas.Length);
                estrellas[i].localPosition = new Vector3(Mathf.Cos(a) * 0.36f, Mathf.Sin(Time.time * 8f + i) * 0.04f, Mathf.Sin(a) * 0.36f);
                estrellas[i].Rotate(180f * Time.deltaTime, 220f * Time.deltaTime, 0f);
            }
        }
    }

    private sealed class TextoFlotanteComportamiento : MonoBehaviour
    {
        private TextMesh mesh;
        private Color colorInicial;
        private Camera camara;
        private float tiempo;
        private float escalaBase;
        private const float Duracion = 0.85f;

        public void Inicializar(TextMesh tm, Color color, float escala)
        {
            mesh = tm;
            colorInicial = color;
            escalaBase = escala;
            camara = Camera.main;
        }

        private void Update()
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / Duracion);
            transform.position += Vector3.up * (1.25f * (1f - t * 0.55f) * Time.unscaledDeltaTime);

            float punch = t < 0.18f ? Mathf.Lerp(0.65f, 1.25f, t / 0.18f) : Mathf.Lerp(1.25f, 0.92f, (t - 0.18f) / 0.82f);
            transform.localScale = Vector3.one * (punch * escalaBase);

            if (camara == null) camara = Camera.main;
            if (camara != null) transform.rotation = camara.transform.rotation;

            if (mesh != null)
            {
                Color c = colorInicial;
                c.a = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                mesh.color = c;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
