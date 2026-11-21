using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Construye los mecanismos de los cuatro puzzles, las recompensas y el altar final
public static partial class PrepararMazmorra
{
    private static void PrepararPuzzlesV4(Transform root, GameObject reliquia)
    {
        List<InteraccionMazmorra> interacciones = new List<InteraccionMazmorra>();
        Rigidbody[] cajas = new Rigidbody[2];
        Renderer[] placas = new Renderer[2];
        PlacaPresion[] placasPresion = new PlacaPresion[2];
        Renderer[] palancas = new Renderer[3];
        PalancaAnimada[] palancasAnimadas = new PalancaAnimada[3];
        Renderer[] cristalesSecuencia = new Renderer[3];
        Renderer[] controlesTrampa = new Renderer[2];
        Renderer[] antorchasCombate = new Renderer[2];
        GameObject[] recompensas = new GameObject[4];
        Renderer[] pedestales = new Renderer[4];
        GameObject[] sellosAltar = new GameObject[4];
        Renderer[] conductosAltar = new Renderer[4];
        TrampaPulso[] trampas = new TrampaPulso[3];

        // Sala Oeste: arena de combate con braseros de progreso
        ColocarDecoracion("Armero_Arena", Props + "03_Blacksmith_01_WoodenRack.prefab", new Vector3(-16.8f, 0f, -4.4f), 1.4f, Quaternion.Euler(0, 90, 0), root);
        ColocarDecoracion("Diana_Arena", Props + "97_Othert_PracticeTarget.prefab", new Vector3(-16.5f, 0f, 3.6f), 1.55f, Quaternion.Euler(0, 125, 0), root);
        for (int i = 0; i < 2; i++)
        {
            float z = i == 0 ? -1.4f : 1.4f;
            Pieza("Base_Brasero_Combate_" + i, Modulos + "Other/Podest_1.prefab", new Vector3(-15.2f, 0.35f, z), new Vector3(0.55f, 0.7f, 0.55f), Quaternion.identity, root, true);
            GameObject orbe = Esfera("Orbe_Combate_" + i, new Vector3(-15.2f, 0.92f, z), 0.28f, root, new Color(0.55f, 0.29f, 0.13f), false);
            antorchasCombate[i] = orbe.GetComponent<Renderer>();
        }

        // Sala Este: puzzle de empuje de cajas sobre placas de presion
        PhysicsMaterial matCaja = ObtenerFisicaDeslizante();
        Pieza("Separador_Placas", Modulos + "Pillars/Pillar_1.prefab", new Vector3(12.5f, 0.65f, 0f), new Vector3(1.1f, 1.3f, 1.1f), Quaternion.identity, root, true);

        for (int i = 0; i < 2; i++)
        {
            float z = i == 0 ? -2.7f : 2.7f;
            GameObject caja = Pieza("Caja_" + (i + 1), Pack + "Construccion de mazmorra/props 2/Prefabs/Box_01.prefab", new Vector3(9.8f, 0.50f, z), new Vector3(0.92f, 0.92f, 0.92f), Quaternion.identity, root, true);
            BoxCollider bc = caja.GetComponent<BoxCollider>();
            if (bc != null) bc.sharedMaterial = matCaja;

            Rigidbody rb = caja.AddComponent<Rigidbody>();
            rb.mass = 0.85f;
            rb.linearDamping = 2.6f;
            rb.angularDamping = 4f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            cajas[i] = rb;

            GameObject placaRoot = new GameObject("Placa_" + (i + 1));
            placaRoot.transform.SetParent(root, false);
            placaRoot.transform.position = new Vector3(14.8f, 0.0f, z);

            Bloque("Marco_Piedra", new Vector3(14.8f, 0.03f, z), new Vector3(1.75f, 0.06f, 1.75f), placaRoot.transform, new Color(0.24f, 0.23f, 0.22f), false);
            GameObject pad = Bloque("Plataforma", new Vector3(14.8f, 0.08f, z), new Vector3(1.38f, 0.07f, 1.38f), placaRoot.transform, new Color(0.68f, 0.38f, 0.16f), false);
            List<Renderer> rendsPlaca = new List<Renderer> { pad.GetComponent<Renderer>() };

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    GameObject baliza = Bloque("Runa", new Vector3(14.8f + sx * 0.82f, 0.14f, z + sz * 0.82f), new Vector3(0.14f, 0.22f, 0.14f), placaRoot.transform, new Color(0.68f, 0.38f, 0.16f), false);
                    rendsPlaca.Add(baliza.GetComponent<Renderer>());
                }
            }

            Light luzPlaca = CrearLuzPunto("Luz_Placa", new Vector3(14.8f, 0.65f, z), new Color(0.68f, 0.38f, 0.16f), 0.7f, 3.8f, placaRoot.transform);
            PlacaPresion pp = placaRoot.AddComponent<PlacaPresion>();
            pp.ConfigurarVisual(pad.transform, rendsPlaca.ToArray(), luzPlaca);

            placas[i] = pad.GetComponent<Renderer>();
            placasPresion[i] = pp;
        }

        // Pedestal de reinicio de cajas
        Pieza("Pedestal_Reset", Modulos + "Other/Podest_1.prefab", new Vector3(8.2f, 0.42f, 3.8f), new Vector3(0.65f, 0.84f, 0.65f), Quaternion.identity, root, true);
        GameObject reset = Esfera("RESET", new Vector3(8.2f, 1.08f, 3.8f), 0.32f, root, new Color(0.24f, 0.75f, 0.92f), true);
        AgregarAuraColeccionable(reset, new Color(0.24f, 0.78f, 0.96f), 1.4f, 3.2f);
        interacciones.Add(Marcar(reset, TipoInteraccion.ResetCajas, 0, "Reiniciar cajas"));
        Rotulo("REINICIAR CAJAS", new Vector3(8.2f, 1.72f, 3.8f), root);

        // Sala Sur: puzzle de palancas animadas en secuencia
        string[] romanos = { "I", "II", "III" };
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 2.9f;
            Vector3 basePos = new Vector3(x, 0f, -14.6f);
            GameObject palancaRoot = new GameObject("Palanca_" + (i + 1));
            palancaRoot.transform.SetParent(root, false);
            palancaRoot.transform.position = basePos + Vector3.up * 0.9f;

            Pieza("Pedestal_Palanca", Modulos + "Other/Podest_1.prefab", basePos + Vector3.up * 0.40f, new Vector3(0.68f, 0.80f, 0.68f), Quaternion.identity, palancaRoot.transform, true);
            Bloque("Mecanismo", basePos + Vector3.up * 0.84f, new Vector3(0.32f, 0.10f, 0.46f), palancaRoot.transform, new Color(0.20f, 0.21f, 0.23f), false);

            GameObject pivote = new GameObject("Brazo_Pivote");
            pivote.transform.SetParent(palancaRoot.transform, false);
            pivote.transform.position = basePos + Vector3.up * 0.86f;
            Bloque("Mango", pivote.transform.position + Vector3.up * 0.34f, new Vector3(0.08f, 0.68f, 0.08f), pivote.transform, new Color(0.34f, 0.24f, 0.16f), false);
            GameObject cristal = Esfera("Cristal_Palanca", pivote.transform.position + Vector3.up * 0.72f, 0.22f, pivote.transform, new Color(0.75f, 0.52f, 0.18f), false);
            Light luzPal = CrearLuzPunto("Luz_Palanca", basePos + Vector3.up * 1.55f, new Color(0.75f, 0.52f, 0.18f), 0.9f, 3.6f, palancaRoot.transform);

            PalancaAnimada pa = palancaRoot.AddComponent<PalancaAnimada>();
            pa.ConfigurarVisual(pivote.transform, cristal.GetComponent<Renderer>(), luzPal);

            palancas[i] = cristal.GetComponent<Renderer>();
            palancasAnimadas[i] = pa;
            interacciones.Add(Marcar(palancaRoot, TipoInteraccion.Palanca, i + 1, "Accionar palanca " + romanos[i]));
            Rotulo("PALANCA " + romanos[i], basePos + Vector3.up * 1.92f, root);

            GameObject cProg = Esfera("Cristal_Progreso_" + i, new Vector3((i - 1) * 0.95f, 2.75f, -17.45f), 0.26f, root, new Color(0.55f, 0.29f, 0.13f), false);
            cristalesSecuencia[i] = cProg.GetComponent<Renderer>();
        }
        Bloque("Mural_Secuencia", new Vector3(0, 2.15f, -17.7f), new Vector3(4.2f, 1.0f, 0.18f), root, new Color(0.22f, 0.21f, 0.20f), false);
        Rotulo("ORDEN:  II  —  I  —  III", new Vector3(0, 2.15f, -17.55f), root);

        // Sala Norte: corredor de trampas de puas y pilares de control
        for (int i = 0; i < 3; i++)
        {
            float z = 8.5f + 2.6f * i;
            GameObject banda = Bloque("Trampa_" + (i + 1), new Vector3(0, 0.05f, z), new Vector3(11.2f, 0.10f, 1.15f), root, new Color(0.8f, 0.16f, 0.1f), false);
            BoxCollider c = banda.GetComponent<BoxCollider>();
            c.isTrigger = true;
            c.size = new Vector3(1f, 4.5f, 1f);

            GameObject grupoPinchos = new GameObject("Pinchos");
            grupoPinchos.transform.SetParent(banda.transform, false);
            grupoPinchos.transform.localScale = new Vector3(1f / 11.2f, 1f / 0.10f, 1f / 1.15f);
            for (int px = -5; px <= 5; px++)
            {
                for (int pz = -1; pz <= 1; pz += 2)
                {
                    GameObject pincho = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pincho.name = "Pua";
                    UnityEngine.Object.DestroyImmediate(pincho.GetComponent<Collider>());
                    pincho.transform.SetParent(grupoPinchos.transform, false);
                    pincho.transform.localPosition = new Vector3(px * 0.96f, 0.18f, pz * 0.28f);
                    pincho.transform.localScale = new Vector3(0.09f, 0.24f, 0.09f);
                    pincho.GetComponent<Renderer>().sharedMaterial = MaterialMecanismo(new Color(0.76f, 0.78f, 0.82f));
                }
            }

            trampas[i] = banda.AddComponent<TrampaPulso>();
            trampas[i].Configurar(i * 0.85f, grupoPinchos.transform);
        }

        for (int i = 0; i < 2; i++)
        {
            Vector3 cPos = new Vector3(i == 0 ? -3.8f : 3.8f, 0f, i == 0 ? 10.0f : 16.0f);
            Pieza("Pedestal_Control_" + (i + 1), Modulos + "Other/Podest_1.prefab", cPos + Vector3.up * 0.45f, new Vector3(0.72f, 0.90f, 0.72f), Quaternion.identity, root, true);
            GameObject cristalCtrl = Esfera("Control_" + (i + 1), cPos + Vector3.up * 1.18f, 0.34f, root, new Color(0.55f, 0.29f, 0.13f), true);
            cristalCtrl.AddComponent<EfectoReliquia>();
            CrearLuzPunto("Luz_Control", cPos + Vector3.up * 1.35f, new Color(0.95f, 0.75f, 0.28f), 1.3f, 3.8f, cristalCtrl.transform);
            controlesTrampa[i] = cristalCtrl.GetComponent<Renderer>();
            interacciones.Add(Marcar(cristalCtrl, TipoInteraccion.ControlTrampa, i + 1, "Desactivar trampa · Control " + (i + 1)));
            Rotulo("CONTROL " + (i + 1), cPos + Vector3.up * 1.82f, root);
        }

        // Recompensas de las 4 salas y altares finales con sellos y conductos
        string[] paths = { "97_Othert_Key_Red.prefab", "97_Othert_Key_Green.prefab", "97_Othert_Key_Yellow.prefab", "05_Vault_01_Emerald#1.prefab" };
        string[] labels = { "Llave roja", "Llave verde", "Llave amarilla", "Gema" };
        Color[] coloresLuz = { new Color(1f, 0.25f, 0.2f), new Color(0.25f, 1f, 0.4f), new Color(1f, 0.85f, 0.25f), new Color(0.2f, 0.95f, 0.7f) };
        Vector3[] positions = { new Vector3(-15.2f, 0.35f, 0), new Vector3(16.8f, 0.35f, 0), new Vector3(0, 0.35f, -16.2f), new Vector3(0, 0.35f, 16.5f) };
        Vector3 centroAltar = new Vector3(0f, 0.02f, 24f);

        for (int i = 0; i < 4; i++)
        {
            Pieza("Base_recompensa_" + i, Modulos + "Other/Podest_1.prefab", positions[i] + Vector3.up * 0.05f, new Vector3(0.8f, 0.72f, 0.8f), Quaternion.identity, root, true);
            GameObject item = ColocarActor("Recompensa_" + MazmorraPuzzles.Objetos[i], Props + paths[i], positions[i] + Vector3.up * 0.92f, 0.50f, root);
            AgregarAuraColeccionable(item, coloresLuz[i], 1.9f, 4.8f);
            recompensas[i] = item;
            interacciones.Add(Marcar(item, TipoInteraccion.Recompensa, i, "Recoger " + labels[i]));
            item.SetActive(false);

            Vector3 p = new Vector3((i % 2 == 0 ? -1 : 1) * 3.1f, 0.65f, i < 2 ? 21.7f : 26.7f);
            GameObject altar = Pieza("Altar_" + i, Modulos + "Other/Podest_1.prefab", p, new Vector3(1.15f, 1.3f, 1.15f), Quaternion.identity, root, true);
            GameObject ranura = Bloque("Ranura_" + i, p + Vector3.up * 0.74f, new Vector3(0.52f, 0.12f, 0.52f), altar.transform, new Color(0.55f, 0.29f, 0.13f), false);
            ranura.GetComponent<BoxCollider>().enabled = false;
            pedestales[i] = ranura.GetComponent<Renderer>();
            interacciones.Add(Marcar(ranura, TipoInteraccion.Pedestal, i, "Colocar " + labels[i]));
            Rotulo(labels[i], p + Vector3.up * 1.48f, root);

            GameObject selloVisual = ColocarActor("Sello_Altar_" + i, Props + paths[i], p + Vector3.up * 0.95f, 0.50f, root);
            AgregarAuraColeccionable(selloVisual, coloresLuz[i], 2.0f, 4.2f);
            selloVisual.SetActive(false);
            sellosAltar[i] = selloVisual;

            Vector3 pSuelo = new Vector3(p.x, 0.02f, p.z);
            Vector3 diff = centroAltar - pSuelo;
            GameObject conducto = Bloque("Conducto_Altar_" + i, (pSuelo + centroAltar) * 0.5f, new Vector3(0.22f, 0.03f, diff.magnitude), root, new Color(0.24f, 0.21f, 0.18f), false);
            conducto.transform.rotation = Quaternion.LookRotation(diff.normalized);
            conducto.GetComponent<BoxCollider>().enabled = false;
            conductosAltar[i] = conducto.GetComponent<Renderer>();
        }

        // Haz luminoso sobre el altar central
        GameObject haz = new GameObject("Haz_Reliquia");
        haz.transform.SetParent(root, false);
        haz.transform.position = new Vector3(0f, 2.3f, 24f);
        Light luzHaz = haz.AddComponent<Light>();
        luzHaz.type = LightType.Point;
        luzHaz.color = new Color(1f, 0.86f, 0.35f);
        luzHaz.intensity = 4.2f;
        luzHaz.range = 10f;
        luzHaz.shadows = LightShadows.None;
        haz.SetActive(false);

        reliquia.name = "Reliquia_Sagrada";
        interacciones.Add(Marcar(reliquia, TipoInteraccion.Reliquia, 0, "Reclamar la Reliquia Sagrada"));
        reliquia.SetActive(false);

        GameObject puerta = GameObject.Find("Puerta_final_bloqueada");
        if (puerta != null && puerta.GetComponent<PuertaAnimada>() == null)
        {
            puerta.AddComponent<PuertaAnimada>();
        }

        MazmorraPuzzles puzzles = root.gameObject.AddComponent<MazmorraPuzzles>();
        SerializedObject so = new SerializedObject(puzzles);
        Referencias(so, "guardianes", new Enemy[] { GameObject.Find("Esqueleto_muestra").GetComponent<Enemy>(), GameObject.Find("Slime_muestra").GetComponent<Enemy>() });
        Referencias(so, "cajas", cajas);
        Referencias(so, "placas", placas);
        Referencias(so, "placasPresion", placasPresion);
        Referencias(so, "palancas", palancas);
        Referencias(so, "palancasAnimadas", palancasAnimadas);
        Referencias(so, "cristalesSecuencia", cristalesSecuencia);
        Referencias(so, "controlesTrampa", controlesTrampa);
        Referencias(so, "antorchasCombate", antorchasCombate);
        Referencias(so, "recompensas", recompensas);
        Referencias(so, "pedestales", pedestales);
        Referencias(so, "sellosAltar", sellosAltar);
        Referencias(so, "conductosAltar", conductosAltar);
        Referencias(so, "trampas", trampas);
        Referencias(so, "interacciones", interacciones.ToArray());
        so.FindProperty("puertaFinal").objectReferenceValue = puerta;
        so.FindProperty("reliquia").objectReferenceValue = reliquia;
        so.FindProperty("hazReliquia").objectReferenceValue = haz;
        so.ApplyModifiedPropertiesWithoutUndo();

        Rotulo("OESTE · ARENA", new Vector3(-4, 2.7f, 0), root);
        Rotulo("ESTE · PLACAS", new Vector3(4, 2.7f, 0), root);
        Rotulo("SUR · PALANCAS", new Vector3(0, 2.7f, -4.2f), root);
        Rotulo("NORTE · TRAMPAS", new Vector3(0, 2.7f, 4.2f), root);
        Rotulo("RELIQUIA SAGRADA", new Vector3(0, 3.05f, 24), root);
    }

    private static PhysicsMaterial ObtenerFisicaDeslizante()
    {
        AsegurarCarpeta(Juego + "Materials");
        string path = Juego + "Materials/CajaDeslizante.physicMaterial";
        PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial("CajaDeslizante")
            {
                dynamicFriction = 0.08f,
                staticFriction = 0.08f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(pm, path);
        }
        return pm;
    }

    private static Material MaterialMecanismo(Color color)
    {
        AsegurarCarpeta(Juego + "Materials");
        string path = Juego + "Materials/Mecanismo_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static Light CrearLuzPunto(string nombre, Vector3 pos, Color color, float intensidad, float rango, Transform parent)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = rango;
        l.shadows = LightShadows.None;
        return l;
    }

    private static void Referencias<T>(SerializedObject so, string campo, T[] objetos) where T : UnityEngine.Object
    {
        SerializedProperty sp = so.FindProperty(campo);
        if (sp == null) return;
        sp.arraySize = objetos.Length;
        for (int i = 0; i < objetos.Length; i++)
        {
            sp.GetArrayElementAtIndex(i).objectReferenceValue = objetos[i];
        }
    }

    private static InteraccionMazmorra Marcar(GameObject obj, TipoInteraccion tipo, int indice, string texto)
    {
        InteraccionMazmorra inter = obj.AddComponent<InteraccionMazmorra>();
        inter.Configurar(tipo, indice, texto);
        return inter;
    }

    private static GameObject Bloque(string nombre, Vector3 pos, Vector3 size, Transform parent, Color color, bool solido)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = nombre;
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = MaterialMecanismo(color);
        obj.GetComponent<BoxCollider>().isTrigger = !solido;
        return obj;
    }

    private static GameObject Esfera(string nombre, Vector3 pos, float diametro, Transform parent, Color color, bool conCollider)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = nombre;
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        obj.transform.localScale = Vector3.one * diametro;
        obj.GetComponent<Renderer>().sharedMaterial = MaterialMecanismo(color);
        Collider col = obj.GetComponent<Collider>();
        if (!conCollider && col != null) UnityEngine.Object.DestroyImmediate(col);
        else if (col != null) col.isTrigger = true;
        return obj;
    }

    private static void Rotulo(string text, Vector3 position, Transform root)
    {
        GameObject go = new GameObject("Rotulo_" + text);
        go.transform.SetParent(root, false);
        go.transform.position = position;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.text = text;
        tm.fontSize = 48;
        tm.characterSize = 0.035f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.94f, 0.84f, 0.58f);
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        go.AddComponent<RotuloOrientado>();
    }
}
