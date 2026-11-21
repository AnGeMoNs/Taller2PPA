using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Instala los managers, combate y controladores de animacion de enemigos en la escena
public static class PrepararSistemasV3
{
    private const string Enemigos = "Assets/Taller 2 Assets/Enemigos/";
    private const string Animaciones = "Assets/Juego/Animations/Enemies/";

    [MenuItem("Taller 2/4. Instalar sistemas en escena abierta")]
    public static void InstalarEscenaAbierta()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().name != "MazmorraBase" && SceneManager.GetActiveScene().name != "MazmorraV4")
        {
            EditorUtility.DisplayDialog("Escena", "Abre MazmorraV4 o ejecuta primero Crear mazmorra completa.", "Aceptar");
            return;
        }
        Instalar();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Sistemas instalados. Guarda la escena.");
    }

    public static void Instalar()
    {
        Movimiento3D player = UnityEngine.Object.FindFirstObjectByType<Movimiento3D>();
        if (player == null) throw new InvalidOperationException("No hay jugador en escena.");

        GetOrAdd<VidaJugador>(player.gameObject);
        GetOrAdd<CombateJugador>(player.gameObject);

        if (UnityEngine.Object.FindFirstObjectByType<GameManager>() == null)
        {
            GameObject manager = new GameObject("Managers");
            manager.AddComponent<GameManager>();
            manager.AddComponent<ProgresoPartida>();
            manager.AddComponent<HUDManager>();
            manager.AddComponent<AudioManager>();
        }

        ConfigurarEnemigo<SkeletonEnemy>("Esqueleto_muestra", 1.75f, 65, 15, 1.9f, 7.5f, 1.55f, "Esqueleto", ClipsEsqueleto());
        ConfigurarEnemigo<SlimeEnemy>("Slime_muestra", 0.85f, 40, 10, 1.25f, 6.5f, 0.55f, "Slime", RpgClips("Slime", false));
        ConfigurarEnemigo<TurtleEnemy>("Tortuga_muestra", 1.0f, 80, 14, 0.90f, 0f, 0.55f, "Tortuga", RpgClips("TurtleShell", true));
    }

    internal static string[] ClipsEsqueleto()
    {
        return new[]
        {
            "Esqueleto/animation/UD_infantry_05_combat_idle.FBX",
            "Esqueleto/animation/UD_infantry_06_combat_walk.FBX",
            "Esqueleto/animation/UD_infantry_07_attack_A.FBX",
            "Esqueleto/animation/Death.fbx",
            null,
            null
        };
    }

    internal static string[] RpgClips(string nombre, bool conDefensa = false)
    {
        string dir = "RPG monsters/Animations/" + nombre + "/";
        return new[]
        {
            dir + "IdleNormal_" + nombre + "_Anim.fbx",
            dir + "Walk_" + nombre + "_Anim.fbx",
            dir + "Attack01_" + nombre + "_Anim.fbx",
            dir + "Die_" + nombre + "_Anim.fbx",
            dir + "GetHit_" + nombre + "_Anim.fbx",
            conDefensa ? dir + "Defend_" + nombre + "_Anim.fbx" : null
        };
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        return comp != null ? comp : go.AddComponent<T>();
    }

    internal static void ConfigurarEnemigo<T>(string nombre, float altura, int vida, int dano, float speed, float vista, float alcance, string controller, string[] clips) where T : Enemy
    {
        GameObject go = GameObject.Find(nombre);
        if (go == null)
        {
            Debug.LogWarning("No se encontro " + nombre);
            return;
        }

        CapsuleCollider col = GetOrAdd<CapsuleCollider>(go);
        col.height = altura;
        col.radius = 0.34f;
        col.center = Vector3.up * (altura * 0.5f);
        col.enabled = true;
        col.isTrigger = false;

        Rigidbody rb = GetOrAdd<Rigidbody>(go);
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        T enemigo = GetOrAdd<T>(go);
        SerializedObject so = new SerializedObject(enemigo);
        so.FindProperty("vidaMaxima").intValue = vida;
        so.FindProperty("dano").intValue = dano;
        so.FindProperty("velocidad").floatValue = speed;
        so.FindProperty("rangoVista").floatValue = vista;
        so.FindProperty("rangoAtaque").floatValue = alcance;
        so.ApplyModifiedPropertiesWithoutUndo();

        Animator animator = go.GetComponentInChildren<Animator>(true);
        if (animator == null && go.transform.childCount > 0)
        {
            animator = go.transform.GetChild(0).gameObject.AddComponent<Animator>();
        }
        if (animator != null)
        {
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = Controlador(controller, clips);
            EditorUtility.SetDirty(animator);
            if (PrefabUtility.IsPartOfPrefabInstance(animator))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }
        }
    }

    private static AnimatorController Controlador(string nombre, string[] clips)
    {
        PrepararMazmorra.AsegurarCarpeta(Animaciones.TrimEnd('/'));
        string path = Animaciones + nombre + ".controller";
        string[] nombres = { "Idle", "Walk", "Attack", "Death", "Hit", "Defend" };
        bool[] loops = { true, true, false, false, false, true };

        AnimatorController existente = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (EsControladorEnemigoValido(existente, clips, nombres)) return existente;

        AnimationClip[] anims = new AnimationClip[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            if (i >= clips.Length || string.IsNullOrEmpty(clips[i])) continue;

            AnimationClip original = AssetDatabase.LoadAllAssetsAtPath(Enemigos + clips[i])
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

            if (original == null)
            {
                Debug.LogWarning("No se importo " + clips[i]);
                continue;
            }

            string clipPath = Animaciones + nombre + "_" + nombres[i] + ".anim";
            AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            AnimationClip copy = UnityEngine.Object.Instantiate(original);
            copy.name = nombre + "_" + nombres[i];

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = loops[i];
            AnimationUtility.SetAnimationClipSettings(copy, settings);

            if (existingClip != null)
            {
                EditorUtility.CopySerialized(copy, existingClip);
                UnityEngine.Object.DestroyImmediate(copy);
                anims[i] = existingClip;
            }
            else
            {
                AssetDatabase.CreateAsset(copy, clipPath);
                anims[i] = copy;
            }
        }

        if (existente != null) AssetDatabase.DeleteAsset(path);
        AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = ac.layers[0].stateMachine;

        AnimatorState idleState = null;
        for (int i = 0; i < nombres.Length; i++)
        {
            if (anims[i] == null) continue;
            AnimatorState state = sm.AddState(nombres[i], new Vector3(260 + (i >= 4 ? 260 : 0), (i % 4) * 85, 0));
            state.motion = anims[i];
            if (i == 0)
            {
                sm.defaultState = state;
                idleState = state;
            }

            // Vuelve a Idle cuando termina la animacion de ataque o dano
            if ((i == 2 || i == 4) && idleState != null)
            {
                AnimatorStateTransition back = state.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = 0.90f;
                back.hasFixedDuration = true;
                back.duration = 0.10f;
            }
        }

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        return ac;
    }

    private static bool EsControladorEnemigoValido(AnimatorController ac, string[] clips, string[] nombres)
    {
        if (ac == null || ac.layers == null || ac.layers.Length == 0) return false;
        ChildAnimatorState[] states = ac.layers[0].stateMachine.states;

        for (int i = 0; i < nombres.Length; i++)
        {
            if (i >= clips.Length || string.IsNullOrEmpty(clips[i])) continue;
            AnimatorState st = states.Select(s => s.state).FirstOrDefault(s => s.name == nombres[i]);
            if (st == null || st.motion == null) return false;
            if ((i == 2 || i == 4) && st.transitions.Length == 0) return false;
        }
        return true;
    }
}
