using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Construye la escena base de la mazmorra con salas, iluminacion, jugador y reliquia
public static partial class PrepararMazmorra
{
    private const string Juego = "Assets/Juego/";
    private const string Pack = "Assets/Taller 2 Assets/";
    private const string Escena = Juego + "Scenes/MazmorraV4.unity";
    private const string Modulos = Pack + "Construccion de mazmorra/Mazmorra assets/Prefabs/";
    private const string Props = Pack + "Construccion de mazmorra/Props/3D Dungeon Lowpoly Pack/Prefabs/";
    private const float TamanoSala = 12f;

    private static readonly Dictionary<Vector2Int, string> Salas = new Dictionary<Vector2Int, string>
    {
        { new Vector2Int(0, 0), "00_Entrada" },
        { new Vector2Int(-1, 0), "01_Combate" },
        { new Vector2Int(1, 0), "02_Placas" },
        { new Vector2Int(0, -1), "03_Secuencia" },
        { new Vector2Int(0, 1), "04_Trampas" },
        { new Vector2Int(0, 2), "05_Camara_del_altar" }
    };

    [MenuItem("Taller 2/1. Preparar materiales")]
    public static void PrepararMateriales()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        AsegurarCarpeta(Juego + "Materials");

        bool urp = GraphicsSettings.currentRenderPipeline != null;
        Shader shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        if (shader == null) throw new InvalidOperationException("No se encontro el shader esperado.");

        if (!urp)
        {
            ConvertirMaterialesURP(Pack.TrimEnd('/'), shader);
        }

        PrepararMaterialesLink();
        AssetDatabase.SaveAssets();
        Debug.Log("Materiales preparados para mazmorra, enemigos y Link.");
    }

    internal static void PrepararMaterialesLink()
    {
        if (GraphicsSettings.currentRenderPipeline != null) return;
        Shader shader = Shader.Find("Standard");
        if (shader == null || !AssetDatabase.IsValidFolder("Assets/CharacterAssets")) return;

        ConvertirMaterialesURP("Assets/CharacterAssets", shader);
        AssetDatabase.SaveAssets();
    }

    internal static void AsegurarCarpeta(string assetFolder)
    {
        string limpia = assetFolder.TrimEnd('/');
        if (AssetDatabase.IsValidFolder(limpia)) return;
        Directory.CreateDirectory(limpia);
        AssetDatabase.Refresh();
    }

    private static void ConvertirMaterialesURP(string folder, Shader standardShader)
    {
        foreach (string id in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) continue;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || path.EndsWith("Skybox_Mat.mat", StringComparison.OrdinalIgnoreCase)) continue;

            bool yaStandard = mat.shader != null && mat.shader.name == "Standard";
            SerializedObject source = new SerializedObject(mat);
            bool transparente =
                string.Equals(mat.GetTag("RenderType", false), "Transparent", StringComparison.OrdinalIgnoreCase) ||
                SavedFloat(source, "_Surface", 0f) > 0.5f ||
                mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");

            if (yaStandard && (!transparente || mat.renderQueue >= (int)RenderQueue.Transparent)) continue;

            Texture albedo = SavedTexture(source, "_BaseMap") ?? SavedTexture(source, "_MainTex");
            Texture normal = SavedTexture(source, "_BumpMap");
            Color color = SavedColor(source, "_BaseColor", SavedColor(source, "_Color", Color.white));

            mat.shader = standardShader;
            mat.mainTexture = albedo;
            mat.color = color;
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.18f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

            if (transparente)
            {
                mat.SetFloat("_Mode", 2f);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(mat);
        }
    }

    private static float SavedFloat(SerializedObject source, string name, float fallback)
    {
        SerializedProperty list = source.FindProperty("m_SavedProperties.m_Floats");
        if (list == null) return fallback;
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty pair = list.GetArrayElementAtIndex(i);
            if (pair.FindPropertyRelative("first").stringValue == name)
            {
                return pair.FindPropertyRelative("second").floatValue;
            }
        }
        return fallback;
    }

    private static Texture SavedTexture(SerializedObject source, string name)
    {
        SerializedProperty list = source.FindProperty("m_SavedProperties.m_TexEnvs");
        if (list == null) return null;
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty pair = list.GetArrayElementAtIndex(i);
            if (pair.FindPropertyRelative("first").stringValue == name)
            {
                return pair.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
            }
        }
        return null;
    }

    private static Color SavedColor(SerializedObject source, string name, Color fallback)
    {
        SerializedProperty list = source.FindProperty("m_SavedProperties.m_Colors");
        if (list == null) return fallback;
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty pair = list.GetArrayElementAtIndex(i);
            if (pair.FindPropertyRelative("first").stringValue == name)
            {
                return pair.FindPropertyRelative("second").colorValue;
            }
        }
        return fallback;
    }

    [MenuItem("Taller 2/2. Crear mazmorra completa (Link)")]
    public static void CrearEscena()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (File.Exists(Escena) && !Application.isBatchMode)
        {
            int opcion = EditorUtility.DisplayDialogComplex(
                "Mazmorra de Link",
                "Ya existe MazmorraV4.unity. ¿Quieres regenerarla desde cero con Link, decoración mejorada y la Reliquia Sagrada, o abrir la existente?",
                "Regenerar nueva", "Cancelar", "Abrir existente");
            if (opcion == 1) return;
            if (opcion == 2)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Escena);
                return;
            }
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        PrepararMateriales();
        PrepararAnimacionesLink.ObtenerControlador(forzarReconstruccion: true);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.32f, 0.30f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.08f, 0.08f, 0.09f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 44f;

        Transform mapa = new GameObject("Mazmorra").transform;
        foreach (KeyValuePair<Vector2Int, string> sala in Salas)
        {
            ConstruirSala(mapa, sala.Key, sala.Value);
        }

        Transform personajes = new GameObject("Personajes").transform;
        GameObject player = CrearJugador(personajes);
        CrearCamara(player);

        Transform props = new GameObject("Puzzles y recompensas").transform;
        ColocarActor("Esqueleto_muestra", Pack + "Enemigos/Esqueleto/prefab/UD_demo_character.prefab", new Vector3(-14, 0, 1), 1.75f, personajes);
        ColocarActor("Slime_muestra", Pack + "Enemigos/RPG monsters/Prefabs/PolyartDefault/SlimePolyart.prefab", new Vector3(-11, 0, -2), 0.85f, personajes);
        ColocarActor("Tortuga_muestra", Pack + "Enemigos/RPG monsters/Prefabs/PolyartDefault/TurtleShellPolyart.prefab", new Vector3(2.2f, 0, 12.4f), 1.0f, personajes);

        // Altar central y reliquia sagrada
        Pieza("Pedestal_Reliquia", Modulos + "Other/Podest_1.prefab", new Vector3(0, 0.5f, 24), new Vector3(1.35f, 1, 1.35f), Quaternion.identity, props, true);
        GameObject reliquia = ColocarActor("Reliquia_Sagrada", Props + "97_Othert_Star.prefab", new Vector3(0, 1.35f, 24), 0.85f, props);
        AgregarAuraColeccionable(reliquia, new Color(1f, 0.82f, 0.28f), 2.8f, 6.5f);

        Pieza("Puerta_final_bloqueada", Modulos + "Doors&Gates/GateSquareClosed.prefab", new Vector3(0, 1.65f, 18), new Vector3(4, 3.3f, 0.4f), Quaternion.identity, props, true);
        DecorarMazmorra(mapa);

        GameObject sol = new GameObject("Luz_general");
        Light luz = sol.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 0.38f;
        luz.color = new Color(0.95f, 0.92f, 0.86f);
        sol.transform.rotation = Quaternion.Euler(55, -35, 0);

        PrepararSistemasV3.Instalar();
        PrepararPuzzlesV4(props, reliquia);
        RepararEscenaV42.Aplicar();

        EditorSceneManager.MarkSceneDirty(scene);
        AsegurarCarpeta(Juego + "Scenes");
        if (!EditorSceneManager.SaveScene(scene, Escena)) throw new IOException("No se pudo guardar la escena.");

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Escena, true) };
        Selection.activeGameObject = player;
        InicioV4.Validar();
        Debug.Log("Mazmorra de Link creada y guardada. Presiona Play para jugar.");
    }

    private static void ConstruirSala(Transform root, Vector2Int celda, string nombre)
    {
        Transform room = new GameObject(nombre).transform;
        room.SetParent(root, false);
        Vector3 centro = new Vector3(celda.x * TamanoSala, 0, celda.y * TamanoSala);

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Pieza("Piso", Modulos + "Ground/Ground_Simple1.prefab", centro + new Vector3(x * 4, -0.15f, z * 4), new Vector3(4, 0.3f, 4), Quaternion.identity, room, true);
            }
        }

        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        foreach (Vector2Int dir in dirs)
        {
            bool tieneVecino = Salas.ContainsKey(celda + dir);
            if (tieneVecino && (dir.x < 0 || dir.y < 0)) continue;

            for (int i = -1; i <= 1; i++)
            {
                if (tieneVecino && i == 0) continue;
                bool sobreX = dir.y != 0;
                Vector3 pos = centro + new Vector3(dir.x * 6, 1.7f, dir.y * 6) + (sobreX ? Vector3.right : Vector3.forward) * i * 4;
                Pieza("Muro", Modulos + "Walls/Wall_1.prefab", pos, new Vector3(4, 3.4f, 0.4f), Quaternion.Euler(0, sobreX ? 0 : 90, 0), room, true);
            }
        }

        GameObject lampara = new GameObject("Lampara");
        lampara.transform.SetParent(room);
        lampara.transform.position = centro + new Vector3(0, 3.1f, 0);
        Light luz = lampara.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = celda.y == 2 ? new Color(1f, 0.86f, 0.56f) : new Color(1f, 0.74f, 0.44f);
        luz.range = 12.5f;
        luz.intensity = celda.y == 2 ? 2.7f : 2.35f;
        luz.shadows = LightShadows.None;
        lampara.AddComponent<LuzAntorcha>();

        for (int lado = -1; lado <= 1; lado += 2)
        {
            Pieza("Pilar", Modulos + "Pillars/Pillar_1.prefab", centro + new Vector3(lado * 4.9f, 1.5f, 4.9f), new Vector3(0.65f, 3, 0.65f), Quaternion.identity, room, true);
            ColocarActor("Antorcha", Props + "97_Othert_Torch_1.prefab", centro + new Vector3(lado * 4.75f, 1.8f, -3.7f), 0.8f, room);
        }
    }

    private static void DecorarMazmorra(Transform mapa)
    {
        Transform deco = new GameObject("Decoracion").transform;
        deco.SetParent(mapa, false);

        // Estandartes en entrada y camara final
        ColocarDecoracion("Estandarte_Entrada_Izq", Props + "99_Decorations_WallBanner_1.prefab", new Vector3(-3.8f, 1.15f, 5.65f), 1.7f, Quaternion.Euler(0, 180, 0), deco);
        ColocarDecoracion("Estandarte_Entrada_Der", Props + "99_Decorations_WallBanner_1.prefab", new Vector3(3.8f, 1.15f, 5.65f), 1.7f, Quaternion.Euler(0, 180, 0), deco);
        ColocarDecoracion("Estandarte_Altar_Izq", Props + "99_Decorations_WallBanner_2.prefab", new Vector3(-3.6f, 1.15f, 29.65f), 1.8f, Quaternion.Euler(0, 180, 0), deco);
        ColocarDecoracion("Estandarte_Altar_Der", Props + "99_Decorations_WallBanner_2.prefab", new Vector3(3.6f, 1.15f, 29.65f), 1.8f, Quaternion.Euler(0, 180, 0), deco);

        // Barriles y anforas en esquinas libres
        ColocarDecoracion("Barril_Entrada_1", Props + "97_Othert_Barrel_Whole.prefab", new Vector3(-4.9f, 0f, -4.9f), 0.9f, Quaternion.identity, deco);
        ColocarDecoracion("Anfora_Entrada_1", Props + "97_Othert_Amphora_Whole.prefab", new Vector3(4.9f, 0f, -4.9f), 0.85f, Quaternion.identity, deco);
        ColocarDecoracion("Barril_Combate_1", Props + "97_Othert_Barrel_Whole.prefab", new Vector3(-16.9f, 0f, 4.9f), 0.9f, Quaternion.identity, deco);
        ColocarDecoracion("Anfora_Secuencia_1", Props + "97_Othert_Amphora_Whole.prefab", new Vector3(-4.9f, 0f, -16.9f), 0.85f, Quaternion.identity, deco);
        ColocarDecoracion("Anfora_Secuencia_2", Props + "97_Othert_Amphora_Whole.prefab", new Vector3(4.9f, 0f, -16.9f), 0.85f, Quaternion.identity, deco);

        // Cofres y velas alrededor del altar final
        ColocarDecoracion("Cofre_Altar_Izq", Props + "05_Vault_01_Chest_Bottom#1.prefab", new Vector3(-4.6f, 0f, 28.4f), 0.7f, Quaternion.Euler(0, 145, 0), deco);
        ColocarDecoracion("Cofre_Altar_Der", Props + "05_Vault_01_Chest_Bottom#1.prefab", new Vector3(4.6f, 0f, 28.4f), 0.7f, Quaternion.Euler(0, -145, 0), deco);
        ColocarDecoracion("Velas_Altar_Izq", Props + "01_DiningHall_01_Candels#1.prefab", new Vector3(-2.1f, 0f, 28.6f), 0.45f, Quaternion.identity, deco);
        ColocarDecoracion("Velas_Altar_Der", Props + "01_DiningHall_01_Candels#1.prefab", new Vector3(2.1f, 0f, 28.6f), 0.45f, Quaternion.identity, deco);
    }

    private static void ColocarDecoracion(string name, string path, Vector3 pos, float height, Quaternion rot, Transform parent)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
        GameObject obj = ColocarActor(name, path, pos, height, parent);
        obj.transform.rotation = rot;
    }

    internal static void AgregarAuraColeccionable(GameObject obj, Color colorLuz, float intensidad, float rango)
    {
        if (obj == null) return;
        if (obj.GetComponent<EfectoReliquia>() == null) obj.AddComponent<EfectoReliquia>();
        GameObject halo = new GameObject("Luz_Aura");
        halo.transform.SetParent(obj.transform, false);
        halo.transform.localPosition = Vector3.up * 0.25f;

        Light l = halo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = colorLuz;
        l.intensity = intensidad;
        l.range = rango;
        l.shadows = LightShadows.None;
    }

    private static GameObject CrearJugador(Transform parent)
    {
        AsegurarCarpeta(Juego + "Prefabs/Player");
        PrepararMaterialesLink();

        GameObject player = new GameObject("Player_Link");
        player.tag = "Player";
        player.transform.SetParent(parent);
        player.transform.position = new Vector3(0, 0.03f, 0);

        GameObject model = ColocarActor("Modelo", PrepararAnimacionesLink.ModeloLink, player.transform.position, 1.85f, player.transform);

        CapsuleCollider col = player.AddComponent<CapsuleCollider>();
        col.height = 1.85f;
        col.radius = 0.38f;
        col.center = Vector3.up * 0.925f;

        Rigidbody body = player.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Animator animator = model.transform.GetChild(0).GetComponent<Animator>();
        if (animator == null) animator = model.transform.GetChild(0).gameObject.AddComponent<Animator>();
        animator.applyRootMotion = false;

        Movimiento3D mv = player.AddComponent<Movimiento3D>();
        SerializedObject so = new SerializedObject(mv);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrepararAnimacionesLink.Asignar(mv, PrepararAnimacionesLink.ObtenerControlador());
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, Juego + "Prefabs/Player/Link.prefab", InteractionMode.AutomatedAction);
        return player;
    }

    private static void CrearCamara(GameObject player)
    {
        GameObject cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";

        Camera camera = cam.AddComponent<Camera>();
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 75f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.08f, 0.09f);

        cam.AddComponent<AudioListener>();
        cam.transform.SetPositionAndRotation(player.transform.position + new Vector3(0, 3, -4), Quaternion.Euler(18, 0, 0));

        CamaraTerceraPersona follow = cam.AddComponent<CamaraTerceraPersona>();
        SerializedObject so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        so = new SerializedObject(player.GetComponent<Movimiento3D>());
        so.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Instanciar(string path, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new FileNotFoundException("Asset no encontrado: " + path);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        foreach (Collider col in instance.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        return instance;
    }

    private static Bounds Limites(GameObject obj) => RepararEscenaV42.LimitesVisuales(obj);

    private static GameObject Pieza(string name, string path, Vector3 pos, Vector3 size, Quaternion rot, Transform parent, bool collider)
    {
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        GameObject model = Instanciar(path, holder.transform);
        Bounds bounds = Limites(model);

        if (size.x > size.z * 3 && bounds.size.z > bounds.size.x)
        {
            model.transform.localRotation = Quaternion.Euler(0, 90, 0);
            bounds = Limites(model);
        }

        Vector3 scale = new Vector3(
            size.x / Mathf.Max(bounds.size.x, 0.001f),
            size.y / Mathf.Max(bounds.size.y, 0.001f),
            size.z / Mathf.Max(bounds.size.z, 0.001f));

        GameObject fit = new GameObject("Visual");
        fit.transform.SetParent(holder.transform, false);
        model.transform.SetParent(fit.transform, true);
        fit.transform.localScale = scale;

        bounds = Limites(model);
        fit.transform.position -= bounds.center - holder.transform.position;
        holder.transform.SetPositionAndRotation(pos, rot);

        if (collider)
        {
            BoxCollider box = holder.AddComponent<BoxCollider>();
            box.size = size;
        }
        return holder;
    }

    internal static GameObject ColocarActor(string name, string path, Vector3 feet, float height, Transform parent)
    {
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        GameObject model = Instanciar(path, holder.transform);

        Bounds b = Limites(model);
        float factor = height / Mathf.Max(b.size.y, 0.001f);
        model.transform.localScale *= factor;

        b = Limites(model);
        model.transform.position += holder.transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
        holder.transform.position = feet;

        foreach (Animator a in model.GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
        foreach (SkinnedMeshRenderer r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen = true;
        return holder;
    }
}
