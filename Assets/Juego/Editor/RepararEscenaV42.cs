using System;
using System.Linq;
using Unity.Behavior;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Ajusta colliders de la mazmorra, alinea personaje y enemigos, y asigna Behavior Graph
public static class RepararEscenaV42
{
    private const string Pack = "Assets/Taller 2 Assets/Enemigos/";

    [MenuItem("Taller 2/6. Reparar fisicas y enemigos")]
    public static void RepararAbierta()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().name != "MazmorraV4")
        {
            EditorUtility.DisplayDialog("Reparar escena", "Abre MazmorraV4 antes de aplicar el ajuste.", "Aceptar");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string ruta = SceneManager.GetActiveScene().path;
        if (!string.IsNullOrEmpty(ruta))
        {
            string copia = AssetDatabase.GenerateUniqueAssetPath("Assets/Juego/Scenes/MazmorraV4_Backup.unity");
            if (!AssetDatabase.CopyAsset(ruta, copia))
            {
                Debug.LogError("No se pudo crear copia de seguridad.");
                return;
            }
            Debug.Log("Respaldo de escena: " + copia);
        }

        Aplicar();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static void Aplicar()
    {
        Transform mapa = GameObject.Find("Mazmorra")?.transform;
        Transform personajes = GameObject.Find("Personajes")?.transform;
        Movimiento3D jugador = Object.FindFirstObjectByType<Movimiento3D>();
        if (mapa == null || personajes == null || jugador == null)
        {
            throw new InvalidOperationException("Faltan Mazmorra, Personajes o el jugador (Player_Link).");
        }

        int pisos = 0, muros = 0, pilares = 0;
        foreach (Transform t in mapa.GetComponentsInChildren<Transform>(true))
        {
            Vector3 tam;
            switch (t.name)
            {
                case "Piso": tam = new Vector3(4, 0.3f, 4); pisos++; break;
                case "Muro": tam = new Vector3(4, 3.4f, 0.4f); muros++; break;
                case "Pilar": tam = new Vector3(0.65f, 3, 0.65f); pilares++; break;
                default: continue;
            }
            AsegurarCaja(t.gameObject, tam);
        }

        GameObject puerta = GameObject.Find("Puerta_final_bloqueada");
        if (puerta != null) AsegurarCaja(puerta, new Vector3(4, 3.3f, 0.4f));

        foreach (Rigidbody caja in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            if (caja.name.StartsWith("Caja_", StringComparison.Ordinal))
            {
                AsegurarCaja(caja.gameObject, new Vector3(0.92f, 0.92f, 0.92f));
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(jugador.gameObject, "Alinear personaje Link");
        PrepararMazmorra.PrepararMaterialesLink();
        PrepararAnimacionesSahur.Asignar(jugador, PrepararAnimacionesSahur.ObtenerControlador());

        Transform visual = jugador.transform.Find("Modelo");
        if (visual != null) AjustarVisual(visual, jugador.transform, 1.85f);

        CapsuleCollider capsule = jugador.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = Undo.AddComponent<CapsuleCollider>(jugador.gameObject);
        capsule.center = new Vector3(0, 0.925f, 0);
        capsule.height = 1.85f;
        capsule.radius = 0.38f;
        capsule.direction = 1;
        capsule.enabled = true;
        capsule.isTrigger = false;

        Rigidbody rb = jugador.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 pos = jugador.transform.position;
        pos.y = 0.04f;
        jugador.transform.position = pos;

        Animator animJugador = jugador.GetComponentInChildren<Animator>();
        if (animJugador == null || !animJugador.isHuman)
        {
            if (jugador.GetComponent<AjusteVisualSuelo>() == null) Undo.AddComponent<AjusteVisualSuelo>(jugador.gameObject);
        }
        else
        {
            AjusteVisualSuelo ajuste = jugador.GetComponent<AjusteVisualSuelo>();
            if (ajuste != null) Undo.DestroyObjectImmediate(ajuste);
        }

        Registrar(jugador.transform);
        Registrar(capsule);
        Registrar(rb);

        // Enemigos distribuidos en las salas y guardianes de la camara final
        CrearEsqueleto("Esqueleto_muestra", new Vector3(-14, 0, 1), personajes, false);
        CrearEsqueleto("Esqueleto_trampas", new Vector3(-2.7f, 0, 14.5f), personajes, true);
        CrearEsqueleto("Esqueleto_guardian_final", new Vector3(-2.0f, 0, 24.5f), personajes, true);
        CrearSlime("Slime_muestra", new Vector3(-11, 0, -2), personajes, false);
        CrearSlime("Slime_guardian_final", new Vector3(2, 0, 24.5f), personajes, true);

        GameObject tortuga = Buscar(personajes, "Tortuga_muestra");
        if (tortuga == null)
        {
            tortuga = PrepararMazmorra.ColocarActor("Tortuga_muestra", Pack + "RPG monsters/Prefabs/PolyartDefault/TurtleShellPolyart.prefab", new Vector3(2.2f, 0, 12.4f), 1.0f, personajes);
        }
        tortuga.transform.position = new Vector3(2.2f, 0.025f, 12.4f);
        tortuga.SetActive(true);
        PrepararSistemasV3.ConfigurarEnemigo<TurtleEnemy>(tortuga.name, 1f, 80, 14, 0.90f, 0, 0.55f, "Tortuga", PrepararSistemasV3.RpgClips("TurtleShell", true));
        PrepararActor(tortuga, 1f, 0.37f, 3.6f);

        TurtleEnemy turtleComp = tortuga.GetComponent<TurtleEnemy>();
        if (turtleComp != null)
        {
            turtleComp.ConfigurarRuta(new[]
            {
                new Vector3(0.0f, 0f, 0.0f),
                new Vector3(-4.0f, 0f, 0.0f),
                new Vector3(-4.0f, 0f, 2.2f),
                new Vector3(0.0f, 0f, 2.2f)
            });
            EditorUtility.SetDirty(turtleComp);
        }

        // Asegura las referencias de los guardianes del primer puzzle
        MazmorraPuzzles puzzles = Object.FindFirstObjectByType<MazmorraPuzzles>();
        if (puzzles != null)
        {
            SerializedObject so = new SerializedObject(puzzles);
            SerializedProperty g = so.FindProperty("guardianes");
            g.arraySize = 2;
            g.GetArrayElementAtIndex(0).objectReferenceValue = Buscar(personajes, "Esqueleto_muestra").GetComponent<Enemy>();
            g.GetArrayElementAtIndex(1).objectReferenceValue = Buscar(personajes, "Slime_muestra").GetComponent<Enemy>();
            so.ApplyModifiedProperties();
        }

        // Luz extra en la camara final para resaltar el altar
        Transform foco = personajes.Find("Luz_refuerzo_final_V42");
        if (foco == null)
        {
            foco = new GameObject("Luz_refuerzo_final_V42").transform;
            foco.SetParent(personajes, false);
            foco.gameObject.AddComponent<Light>();
        }
        foco.position = new Vector3(0, 2.9f, 24);
        Light l = foco.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.8f, 0.6f);
        l.intensity = 2.2f;
        l.range = 10f;
        l.shadows = LightShadows.None;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Ajuste aplicado: {pisos} pisos, {muros} muros, {pilares} pilares y {personajes.GetComponentsInChildren<Enemy>(true).Length} enemigos configurados.");
    }

    private static void AsegurarCaja(GameObject go, Vector3 size)
    {
        Undo.RecordObject(go.transform, "Reparar collider");
        BoxCollider b = go.GetComponent<BoxCollider>();
        if (b == null) b = Undo.AddComponent<BoxCollider>(go);
        Undo.RecordObject(b, "Reparar collider");
        b.center = Vector3.zero;
        b.size = size;
        b.enabled = true;
        b.isTrigger = false;

        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
        {
            if (c != b)
            {
                Undo.RecordObject(c, "Desactivar collider duplicado");
                c.enabled = false;
                Registrar(c);
            }
        }
        Registrar(b);
    }

    private static GameObject Buscar(Transform parent, string name) => parent.Find(name)?.gameObject;

    private static void CrearEsqueleto(string name, Vector3 pos, Transform parent, bool nuevo)
    {
        GameObject obj = Buscar(parent, name);
        if (obj == null)
        {
            obj = PrepararMazmorra.ColocarActor(name, Pack + "Esqueleto/prefab/UD_demo_character.prefab", pos, 1.75f, parent);
        }
        obj.SetActive(true);
        PrepararSistemasV3.ConfigurarEnemigo<SkeletonEnemy>(name, 1.75f, 65, 15, 1.9f, 7.5f, 1.55f, "Esqueleto", PrepararSistemasV3.ClipsEsqueleto());
        PrepararActor(obj, 1.75f, 0.34f, nuevo ? 3.8f : 4.4f);
    }

    private static void CrearSlime(string name, Vector3 pos, Transform parent, bool nuevo)
    {
        GameObject obj = Buscar(parent, name);
        if (obj == null)
        {
            obj = PrepararMazmorra.ColocarActor(name, Pack + "RPG monsters/Prefabs/PolyartDefault/SlimePolyart.prefab", pos, 0.85f, parent);
        }
        obj.SetActive(true);
        PrepararSistemasV3.ConfigurarEnemigo<SlimeEnemy>(name, 0.85f, 40, 10, 1.25f, 6.5f, 0.55f, "Slime", PrepararSistemasV3.RpgClips("Slime", false));
        PrepararActor(obj, 0.85f, 0.35f, nuevo ? 3.5f : 4.4f);
    }

    private static void PrepararActor(GameObject obj, float altura, float radio, float territorio)
    {
        Undo.RegisterFullObjectHierarchyUndo(obj, "Alinear enemigo");
        if (obj.transform.childCount > 0) AjustarVisual(obj.transform.GetChild(0), obj.transform, altura);

        Vector3 p = obj.transform.position;
        p.y = 0.025f;
        obj.transform.position = p;

        CapsuleCollider c = obj.GetComponent<CapsuleCollider>();
        c.center = Vector3.up * (altura * 0.5f);
        c.height = altura;
        c.radius = radio;
        c.direction = 1;
        c.enabled = true;
        c.isTrigger = false;

        foreach (Collider otro in obj.GetComponentsInChildren<Collider>(true))
        {
            if (otro != c)
            {
                otro.enabled = false;
                Registrar(otro);
            }
        }
        foreach (SkinnedMeshRenderer r in obj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            r.enabled = true;
            r.updateWhenOffscreen = true;
            Registrar(r);
        }

        Enemy enemy = obj.GetComponent<Enemy>();
        SerializedObject so = new SerializedObject(enemy);
        so.FindProperty("radioTerritorio").floatValue = territorio;
        so.ApplyModifiedProperties();

        Registrar(c);
        Registrar(obj.transform);
    }

    private static void Registrar(Object obj)
    {
        EditorUtility.SetDirty(obj);
        if (PrefabUtility.IsPartOfPrefabInstance(obj))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }
    }

    private static void AjustarVisual(Transform visual, Transform raiz, float altura)
    {
        Bounds b = LimitesVisuales(visual.gameObject);
        if (b.size.y > 0.001f) visual.localScale *= altura / b.size.y;
        b = LimitesVisuales(visual.gameObject);
        visual.position += raiz.position - new Vector3(b.center.x, b.min.y, b.center.z);
        Registrar(visual);
    }

    // Mide la malla real ignorando espada, escudo y rastros para alinear bien los pies
    internal static Bounds LimitesVisuales(GameObject obj)
    {
        bool tiene = false;
        Bounds bounds = new Bounds();
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            if (r is TrailRenderer || r.gameObject.name == "Espada_Link" || r.gameObject.name == "Escudo_Link") continue;

            if (r is SkinnedMeshRenderer skin && skin.sharedMesh != null)
            {
                Mesh mesh = new Mesh();
                try
                {
                    skin.BakeMesh(mesh);
                    Vector3[] vertices = mesh.vertices;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        Vector3 pos = skin.transform.TransformPoint(vertices[i]);
                        if (!tiene)
                        {
                            bounds = new Bounds(pos, Vector3.zero);
                            tiene = true;
                        }
                        else
                        {
                            bounds.Encapsulate(pos);
                        }
                    }
                    if (vertices.Length > 0) continue;
                }
                finally
                {
                    Object.DestroyImmediate(mesh);
                }
            }

            if (!tiene)
            {
                bounds = r.bounds;
                tiene = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (!tiene) throw new InvalidOperationException("Modelo sin renderer: " + obj.name);
        return bounds;
    }

    [MenuItem("Taller 2/7. Asignar Behavior Graph seleccionado")]
    public static void AsignarGrafo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        BehaviorGraph graph = Selection.activeObject as BehaviorGraph;
        if (graph == null && !string.IsNullOrEmpty(path))
        {
            graph = AssetDatabase.LoadAllAssetsAtPath(path).OfType<BehaviorGraph>().FirstOrDefault();
        }
        if (graph == null)
        {
            EditorUtility.DisplayDialog("Unity Behavior", "Crea y abre BG_Enemigo, guarda el grafo y seleccionalo en Project antes de usar este menu.", "Aceptar");
            return;
        }

        Enemy[] enemigos = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemigos)
        {
            BehaviorGraphAgent agent = enemy.GetComponent<BehaviorGraphAgent>();
            if (agent == null) agent = Undo.AddComponent<BehaviorGraphAgent>(enemy.gameObject);
            Undo.RecordObject(agent, "Asignar Behavior");
            agent.Graph = graph;
            agent.enabled = true;
            Registrar(agent);

            SerializedObject so = new SerializedObject(enemy);
            so.FindProperty("usarUnityBehavior").boolValue = true;
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Behavior Graph asignado a {enemigos.Length} enemigos.");
    }
}
