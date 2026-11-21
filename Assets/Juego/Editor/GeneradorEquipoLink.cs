using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Genera los modelos 3D de la espada y el escudo y los equipa en los huesos de Link
public static class GeneradorEquipoLink
{
    private const string DirModelos = "Assets/Juego/Models/EquipoLink";
    private const string DirMats = "Assets/Juego/Materials/EquipoLink";
    private const string RutaEspada = DirModelos + "/EspadaLink.asset";
    private const string RutaEscudo = DirModelos + "/EscudoLink.asset";

    public static void Equipar(Movimiento3D movimiento)
    {
        if (movimiento == null) return;
        Transform modelo = movimiento.transform.Find("Modelo");
        if (modelo == null || modelo.childCount == 0) return;

        Transform raizVisual = modelo.GetChild(0);
        Transform manoDerecha = BuscarHueso(raizVisual, "mixamorig:RightHand");
        Transform antebrazoIzq = BuscarHueso(raizVisual, "mixamorig:LeftForeArm");
        if (manoDerecha == null || antebrazoIzq == null)
        {
            Debug.LogWarning("No se encontraron los huesos mixamorig:RightHand o LeftForeArm en Link.");
            return;
        }

        PrepararMazmorra.AsegurarCarpeta(DirModelos);
        PrepararMazmorra.AsegurarCarpeta(DirMats);

        Material matAcero = ObtenerMaterial("Acero", new Color(0.84f, 0.88f, 0.94f), 0.88f, 0.78f);
        Material matAzul = ObtenerMaterial("AzulReal", new Color(0.11f, 0.24f, 0.64f), 0.35f, 0.55f);
        Material matOro = ObtenerMaterial("Oro", new Color(0.95f, 0.76f, 0.22f), 0.85f, 0.72f);
        Material matEmpunadura = ObtenerMaterial("Empunadura", new Color(0.10f, 0.38f, 0.30f), 0.08f, 0.28f);
        Material matCarmesi = ObtenerMaterial("Carmesi", new Color(0.76f, 0.14f, 0.16f), 0.25f, 0.45f);
        Material matCuero = ObtenerMaterial("CueroOscuro", new Color(0.22f, 0.15f, 0.11f), 0.15f, 0.22f);
        Material matRastro = ObtenerMaterialRastro();

        Mesh meshEspada = ObtenerOCrearMesh(RutaEspada, ConstruirMeshEspada);
        Mesh meshEscudo = ObtenerOCrearMesh(RutaEscudo, ConstruirMeshEscudo);

        // Espada en mano derecha, reutilizando el nodo si ya existe dentro del prefab
        Transform espadaTr = manoDerecha.Find("Espada_Link");
        GameObject espada = espadaTr != null ? espadaTr.gameObject : new GameObject("Espada_Link");
        if (espadaTr == null) espada.transform.SetParent(manoDerecha, false);
        espada.transform.localPosition = new Vector3(0.0f, 0.095f, 0.032f);
        espada.transform.localRotation = Quaternion.Euler(90f, -8f, 84f);
        espada.transform.localScale = Vector3.one;

        MeshFilter mfEspada = GetOrAdd<MeshFilter>(espada);
        mfEspada.sharedMesh = meshEspada;
        MeshRenderer mrEspada = GetOrAdd<MeshRenderer>(espada);
        mrEspada.sharedMaterials = new[] { matAcero, matAzul, matOro, matEmpunadura };
        mrEspada.shadowCastingMode = ShadowCastingMode.On;

        // Rastro luminoso cerca de la punta de la hoja
        Transform rastroTr = espada.transform.Find("RastroEspada");
        GameObject rastroObj = rastroTr != null ? rastroTr.gameObject : new GameObject("RastroEspada");
        if (rastroTr == null) rastroObj.transform.SetParent(espada.transform, false);
        rastroObj.transform.localPosition = new Vector3(0f, 0.72f, 0f);

        TrailRenderer trail = GetOrAdd<TrailRenderer>(rastroObj);
        trail.time = 0.18f;
        trail.startWidth = 0.18f;
        trail.endWidth = 0.01f;
        trail.minVertexDistance = 0.04f;
        trail.sharedMaterial = matRastro;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.68f, 0.92f, 1f), 0f), new GradientColorKey(new Color(0.22f, 0.62f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = grad;

        // Luz dorada en la hoja para cuando se activa la reliquia
        Transform luzTr = espada.transform.Find("Luz_Espada");
        GameObject luzObj = luzTr != null ? luzTr.gameObject : new GameObject("Luz_Espada");
        if (luzTr == null) luzObj.transform.SetParent(espada.transform, false);
        luzObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);

        Light luzEspada = GetOrAdd<Light>(luzObj);
        luzEspada.type = LightType.Point;
        luzEspada.color = new Color(1f, 0.84f, 0.30f);
        luzEspada.intensity = 2.0f;
        luzEspada.range = 4.0f;
        luzEspada.shadows = LightShadows.None;
        luzEspada.enabled = false;

        // Escudo en antebrazo izquierdo mirando hacia el frente al bloquear
        Transform escudoTr = antebrazoIzq.Find("Escudo_Link");
        GameObject escudo = escudoTr != null ? escudoTr.gameObject : new GameObject("Escudo_Link");
        if (escudoTr == null) escudo.transform.SetParent(antebrazoIzq, false);
        escudo.transform.localPosition = new Vector3(0.0f, 0.22f, -0.075f);
        escudo.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);
        escudo.transform.localScale = Vector3.one;

        MeshFilter mfEscudo = GetOrAdd<MeshFilter>(escudo);
        mfEscudo.sharedMesh = meshEscudo;
        MeshRenderer mrEscudo = GetOrAdd<MeshRenderer>(escudo);
        mrEscudo.sharedMaterials = new[] { matAzul, matAcero, matOro, matCarmesi, matCuero };
        mrEscudo.shadowCastingMode = ShadowCastingMode.On;

        EquipoLinkVisual visual = GetOrAdd<EquipoLinkVisual>(movimiento.gameObject);
        visual.Configurar(trail, luzEspada);

        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(espada);
        EditorUtility.SetDirty(escudo);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        return comp != null ? comp : go.AddComponent<T>();
    }

    private static Transform BuscarHueso(Transform raiz, string nombre)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == nombre) return t;
        }
        return null;
    }

    private static Material ObtenerMaterial(string nombre, Color color, float metallic, float smoothness)
    {
        string path = DirMats + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        Shader shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = shader;
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material ObtenerMaterialRastro()
    {
        string path = DirMats + "/RastroEspada.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = shader;
        m.color = Color.white;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Mesh ObtenerOCrearMesh(string path, Func<Mesh> constructor)
    {
        Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        Mesh nueva = constructor();
        if (existente != null)
        {
            EditorUtility.CopySerialized(nueva, existente);
            UnityEngine.Object.DestroyImmediate(nueva);
            EditorUtility.SetDirty(existente);
            AssetDatabase.SaveAssets();
            return existente;
        }
        AssetDatabase.CreateAsset(nueva, path);
        AssetDatabase.SaveAssets();
        return nueva;
    }

    // Construye la malla de la espada con 4 submeshes (acero, azul, oro, mango)
    private static Mesh ConstruirMeshEspada()
    {
        MeshBuilder mb = new MeshBuilder(4);

        // Mango y aros de agarre
        mb.AgregarPrisma(3, new Vector3(0f, -0.105f, 0f), new Vector3(0f, 0.085f, 0f), 0.017f, 0.019f, 8);
        mb.AgregarPrisma(2, new Vector3(0f, -0.025f, 0f), new Vector3(0f, -0.008f, 0f), 0.021f, 0.021f, 8);
        mb.AgregarPrisma(2, new Vector3(0f, 0.055f, 0f), new Vector3(0f, 0.072f, 0f), 0.022f, 0.022f, 8);

        // Pomo dorado inferior
        mb.AgregarPrisma(2, new Vector3(0f, -0.130f, 0f), new Vector3(0f, -0.105f, 0f), 0.028f, 0.020f, 8);
        mb.AgregarPrisma(2, new Vector3(0f, -0.160f, 0f), new Vector3(0f, -0.130f, 0f), 0.010f, 0.028f, 8);

        // Guarda con alas inclinadas y gema central
        mb.AgregarCaja(1, new Vector3(0f, 0.115f, 0f), new Vector3(0.095f, 0.055f, 0.044f));
        for (int lado = -1; lado <= 1; lado += 2)
        {
            mb.AgregarCajaRotada(1, new Vector3(lado * 0.088f, 0.132f, 0f), new Vector3(0.115f, 0.036f, 0.030f), Quaternion.Euler(0f, 0f, lado * 22f));
            mb.AgregarCajaRotada(1, new Vector3(lado * 0.148f, 0.168f, 0f), new Vector3(0.068f, 0.024f, 0.022f), Quaternion.Euler(0f, 0f, lado * 42f));
            mb.AgregarCajaRotada(2, new Vector3(lado * 0.175f, 0.194f, 0f), new Vector3(0.024f, 0.020f, 0.024f), Quaternion.Euler(0f, 0f, lado * 45f));
        }
        mb.AgregarCajaRotada(2, new Vector3(0f, 0.118f, 0f), new Vector3(0.032f, 0.032f, 0.052f), Quaternion.Euler(0f, 0f, 45f));

        // Hoja de acero de doble filo y punta afilada
        mb.AgregarSeccionHoja(0, 0.145f, 0.046f, 0.016f, 0.240f, 0.036f, 0.014f);
        mb.AgregarSeccionHoja(0, 0.240f, 0.036f, 0.014f, 0.800f, 0.027f, 0.011f);
        mb.AgregarPuntaHoja(0, 0.800f, 0.027f, 0.011f, 0.960f);

        return mb.Construir("EspadaLink");
    }

    // Construye la malla curva del escudo con 5 submeshes (azul, acero, oro, carmesi, cuero)
    private static Mesh ConstruirMeshEscudo()
    {
        MeshBuilder mb = new MeshBuilder(5);

        float[] alturas = { 0.34f, 0.28f, 0.08f, -0.10f, -0.24f, -0.35f };
        float[] anchos = { 0.21f, 0.255f, 0.245f, 0.195f, 0.115f, 0.008f };
        const float curva = 0.055f;
        const float grosor = 0.026f;
        const int columnas = 8;

        for (int r = 0; r < alturas.Length - 1; r++)
        {
            for (int c = 0; c < columnas; c++)
            {
                float u0 = -1f + 2f * c / columnas;
                float u1 = -1f + 2f * (c + 1) / columnas;

                Vector3 f00 = PuntoEscudo(u0 * anchos[r], alturas[r], curva, 0f);
                Vector3 f10 = PuntoEscudo(u1 * anchos[r], alturas[r], curva, 0f);
                Vector3 f01 = PuntoEscudo(u0 * anchos[r + 1], alturas[r + 1], curva, 0f);
                Vector3 f11 = PuntoEscudo(u1 * anchos[r + 1], alturas[r + 1], curva, 0f);

                mb.AgregarQuad(0, f00, f10, f11, f01);

                Vector3 b00 = f00 + Vector3.back * grosor;
                Vector3 b10 = f10 + Vector3.back * grosor;
                Vector3 b01 = f01 + Vector3.back * grosor;
                Vector3 b11 = f11 + Vector3.back * grosor;
                mb.AgregarQuad(4, b10, b00, b01, b11);
            }
        }

        // Borde metalico plateado alrededor del escudo
        const float borde = 0.032f;
        const float relieveBorde = 0.014f;
        for (int lado = -1; lado <= 1; lado += 2)
        {
            for (int r = 0; r < alturas.Length - 1; r++)
            {
                float xExt0 = lado * (anchos[r] + 0.012f);
                float xExt1 = lado * (anchos[r + 1] + 0.012f);
                float xInt0 = lado * Mathf.Max(0f, anchos[r] - borde);
                float xInt1 = lado * Mathf.Max(0f, anchos[r + 1] - borde);

                Vector3 pExt0 = PuntoEscudo(xExt0, alturas[r], curva, relieveBorde);
                Vector3 pExt1 = PuntoEscudo(xExt1, alturas[r + 1], curva, relieveBorde);
                Vector3 pInt0 = PuntoEscudo(xInt0, alturas[r], curva, relieveBorde);
                Vector3 pInt1 = PuntoEscudo(xInt1, alturas[r + 1], curva, relieveBorde);
                Vector3 kExt0 = pExt0 + Vector3.back * (grosor + relieveBorde);
                Vector3 kExt1 = pExt1 + Vector3.back * (grosor + relieveBorde);

                if (lado > 0)
                {
                    mb.AgregarQuad(1, pInt0, pExt0, pExt1, pInt1);
                    mb.AgregarQuad(1, pExt0, kExt0, kExt1, pExt1);
                }
                else
                {
                    mb.AgregarQuad(1, pExt0, pInt0, pInt1, pExt1);
                    mb.AgregarQuad(1, kExt0, pExt0, pExt1, kExt1);
                }
            }
        }

        // Borde superior
        for (int c = 0; c < columnas; c++)
        {
            float u0 = -1f + 2f * c / columnas;
            float u1 = -1f + 2f * (c + 1) / columnas;
            Vector3 t0 = PuntoEscudo(u0 * (anchos[0] + 0.012f), alturas[0] + 0.014f, curva, relieveBorde);
            Vector3 t1 = PuntoEscudo(u1 * (anchos[0] + 0.012f), alturas[0] + 0.014f, curva, relieveBorde);
            Vector3 m0 = PuntoEscudo(u0 * anchos[0], alturas[0] - 0.026f, curva, relieveBorde);
            Vector3 m1 = PuntoEscudo(u1 * anchos[0], alturas[0] - 0.026f, curva, relieveBorde);
            mb.AgregarQuad(1, t0, t1, m1, m0);
            mb.AgregarQuad(1, t0 + Vector3.back * (grosor + relieveBorde), t1 + Vector3.back * (grosor + relieveBorde), t1, t0);
        }

        // Emblema dorado superior con tres triangulos en relieve
        AgregarTrianguloRelieve(mb, 2, 0.0f, 0.175f, 0.068f, 0.060f, curva, 0.014f);
        AgregarTrianguloRelieve(mb, 2, -0.036f, 0.112f, 0.068f, 0.060f, curva, 0.014f);
        AgregarTrianguloRelieve(mb, 2, 0.036f, 0.112f, 0.068f, 0.060f, curva, 0.014f);
        mb.AgregarCaja(2, PuntoEscudo(0f, 0.275f, curva, 0.010f), new Vector3(0.28f, 0.020f, 0.014f));
        mb.AgregarCajaRotada(2, PuntoEscudo(0f, -0.26f, curva, 0.010f), new Vector3(0.055f, 0.055f, 0.016f), Quaternion.Euler(0f, 0f, 45f));

        // Cresta roja central
        mb.AgregarCajaRotada(3, PuntoEscudo(0f, -0.045f, curva, 0.012f), new Vector3(0.082f, 0.105f, 0.014f), Quaternion.Euler(0f, 0f, 45f));
        for (int lado = -1; lado <= 1; lado += 2)
        {
            mb.AgregarCajaRotada(3, PuntoEscudo(lado * 0.095f, -0.010f, curva, 0.010f), new Vector3(0.115f, 0.036f, 0.012f), Quaternion.Euler(0f, 0f, lado * 18f));
            mb.AgregarCajaRotada(3, PuntoEscudo(lado * 0.082f, -0.062f, curva, 0.010f), new Vector3(0.088f, 0.026f, 0.012f), Quaternion.Euler(0f, 0f, lado * -16f));
        }

        // Correas traseras para el antebrazo
        mb.AgregarCaja(4, new Vector3(-0.07f, 0.0f, -grosor - 0.018f), new Vector3(0.045f, 0.16f, 0.032f));
        mb.AgregarCaja(4, new Vector3(0.08f, 0.0f, -grosor - 0.018f), new Vector3(0.045f, 0.16f, 0.032f));

        return mb.Construir("EscudoLink");
    }

    private static Vector3 PuntoEscudo(float x, float y, float curva, float extraZ)
    {
        float nx = x / 0.26f;
        float z = curva * (1f - nx * nx * 0.72f) + extraZ;
        return new Vector3(x, y, z);
    }

    private static void AgregarTrianguloRelieve(MeshBuilder mb, int sub, float cx, float cy, float ancho, float alto, float curva, float rel)
    {
        Vector3 pTop = PuntoEscudo(cx, cy + alto * 0.55f, curva, rel);
        Vector3 pIzq = PuntoEscudo(cx - ancho * 0.5f, cy - alto * 0.45f, curva, rel);
        Vector3 pDer = PuntoEscudo(cx + ancho * 0.5f, cy - alto * 0.45f, curva, rel);
        Vector3 bTop = pTop + Vector3.back * (rel + 0.006f);
        Vector3 bIzq = pIzq + Vector3.back * (rel + 0.006f);
        Vector3 bDer = pDer + Vector3.back * (rel + 0.006f);

        mb.AgregarTriangulo(sub, pTop, pDer, pIzq);
        mb.AgregarQuad(sub, pTop, bTop, bDer, pDer);
        mb.AgregarQuad(sub, pDer, bDer, bIzq, pIzq);
        mb.AgregarQuad(sub, pIzq, bIzq, bTop, pTop);
    }

    // Auxiliar simple para juntar vertices y triangulos en varios submeshes
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> verts = new List<Vector3>();
        private readonly List<int>[] subTris;

        public MeshBuilder(int submeshes)
        {
            subTris = new List<int>[submeshes];
            for (int i = 0; i < submeshes; i++) subTris[i] = new List<int>();
        }

        public void AgregarTriangulo(int sub, Vector3 a, Vector3 b, Vector3 c)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            subTris[sub].Add(i); subTris[sub].Add(i + 1); subTris[sub].Add(i + 2);
        }

        public void AgregarQuad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            subTris[sub].Add(i); subTris[sub].Add(i + 1); subTris[sub].Add(i + 2);
            subTris[sub].Add(i); subTris[sub].Add(i + 2); subTris[sub].Add(i + 3);
        }

        public void AgregarCaja(int sub, Vector3 centro, Vector3 tam)
            => AgregarCajaRotada(sub, centro, tam, Quaternion.identity);

        public void AgregarCajaRotada(int sub, Vector3 centro, Vector3 tam, Quaternion rot)
        {
            Vector3 h = tam * 0.5f;
            Vector3[] p =
            {
                centro + rot * new Vector3(-h.x, -h.y, -h.z),
                centro + rot * new Vector3( h.x, -h.y, -h.z),
                centro + rot * new Vector3( h.x,  h.y, -h.z),
                centro + rot * new Vector3(-h.x,  h.y, -h.z),
                centro + rot * new Vector3(-h.x, -h.y,  h.z),
                centro + rot * new Vector3( h.x, -h.y,  h.z),
                centro + rot * new Vector3( h.x,  h.y,  h.z),
                centro + rot * new Vector3(-h.x,  h.y,  h.z)
            };
            AgregarQuad(sub, p[0], p[3], p[2], p[1]);
            AgregarQuad(sub, p[5], p[6], p[7], p[4]);
            AgregarQuad(sub, p[4], p[7], p[3], p[0]);
            AgregarQuad(sub, p[1], p[2], p[6], p[5]);
            AgregarQuad(sub, p[3], p[7], p[6], p[2]);
            AgregarQuad(sub, p[4], p[0], p[1], p[5]);
        }

        public void AgregarPrisma(int sub, Vector3 a, Vector3 b, float rA, float rB, int lados)
        {
            for (int i = 0; i < lados; i++)
            {
                float t0 = (float)i / lados * Mathf.PI * 2f;
                float t1 = (float)(i + 1) / lados * Mathf.PI * 2f;
                Vector3 d0 = new Vector3(Mathf.Cos(t0), 0f, Mathf.Sin(t0));
                Vector3 d1 = new Vector3(Mathf.Cos(t1), 0f, Mathf.Sin(t1));
                Vector3 a0 = a + d0 * rA, a1 = a + d1 * rA;
                Vector3 b0 = b + d0 * rB, b1 = b + d1 * rB;
                AgregarQuad(sub, a0, b0, b1, a1);
                AgregarTriangulo(sub, a, a1, a0);
                AgregarTriangulo(sub, b, b0, b1);
            }
        }

        public void AgregarSeccionHoja(int sub, float y0, float w0, float t0, float y1, float w1, float t1)
        {
            Vector3[] r0 = { new Vector3(-w0, y0, 0f), new Vector3(0f, y0, t0), new Vector3(w0, y0, 0f), new Vector3(0f, y0, -t0) };
            Vector3[] r1 = { new Vector3(-w1, y1, 0f), new Vector3(0f, y1, t1), new Vector3(w1, y1, 0f), new Vector3(0f, y1, -t1) };
            for (int i = 0; i < 4; i++)
            {
                int n = (i + 1) % 4;
                AgregarQuad(sub, r0[i], r1[i], r1[n], r0[n]);
            }
        }

        public void AgregarPuntaHoja(int sub, float y0, float w0, float t0, float yTip)
        {
            Vector3 tip = new Vector3(0f, yTip, 0f);
            Vector3[] r0 = { new Vector3(-w0, y0, 0f), new Vector3(0f, y0, t0), new Vector3(w0, y0, 0f), new Vector3(0f, y0, -t0) };
            for (int i = 0; i < 4; i++)
            {
                int n = (i + 1) % 4;
                AgregarTriangulo(sub, r0[i], tip, r0[n]);
            }
        }

        public Mesh Construir(string nombre)
        {
            Mesh mesh = new Mesh { name = nombre };
            mesh.SetVertices(verts);
            mesh.subMeshCount = subTris.Length;
            for (int i = 0; i < subTris.Length; i++)
            {
                mesh.SetTriangles(subTris[i], i);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
