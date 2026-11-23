using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Configura el modelo Humanoid de Link, su Animator Controller y su espada y escudo
public static class PrepararAnimacionesLink
{
    public const string ModeloLink = "Assets/CharacterAssets/LinkAnimation/LinkMaximo.fbx";
    private const string FbxDir = "Assets/CharacterAssets/LinkAnimation/";
    private const string Base = "Assets/Juego/Animations/Link/";
    private const string Controlador = Base + "Link.controller";
    private const string Prefab = "Assets/Juego/Prefabs/Player/Link.prefab";

    [MenuItem("Taller 2/3. Configurar personaje, equipo y animaciones (Link)")]
    public static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        PrepararMazmorra.PrepararMaterialesLink();
        AnimatorController controller = ObtenerControlador(forzarReconstruccion: true);

        ActualizarPrefab(Prefab, controller);

        foreach (Movimiento3D movimiento in UnityEngine.Object.FindObjectsByType<Movimiento3D>(FindObjectsSortMode.None))
        {
            Undo.RegisterFullObjectHierarchyUndo(movimiento.gameObject, "Configurar Link, espada y escudo");
            Asignar(movimiento, controller);
            EditorSceneManager.MarkSceneDirty(movimiento.gameObject.scene);
        }
        Debug.Log("Link actualizado con animaciones, Foot IK, espada y escudo.");
    }

    private static void ActualizarPrefab(string prefabPath, AnimatorController controller)
    {
        if (!File.Exists(prefabPath)) return;
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Movimiento3D movimiento = root.GetComponent<Movimiento3D>();
            if (movimiento != null)
            {
                Asignar(movimiento, controller);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static AnimatorController ObtenerControlador(bool forzarReconstruccion = false)
    {
        PrepararMazmorra.AsegurarCarpeta(Base.TrimEnd('/'));
        Avatar linkAvatar = AsegurarRigModelo();

        AnimatorController existente = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controlador);
        if (!forzarReconstruccion && EsControladorValido(existente)) return existente;

        AnimationClip idle = ImportarClip("sword and shield idle.fbx", "Idle", true, linkAvatar);
        AnimationClip walk = ImportarClip("sword and shield walk.fbx", "Walk", true, linkAvatar);
        AnimationClip run = ImportarClip("sword and shield run.fbx", "Run", true, linkAvatar);
        AnimationClip block = ImportarClip("sword and shield block idle.fbx", "Block", true, linkAvatar);
        AnimationClip attack1 = ImportarClip("sword and shield slash.fbx", "Attack1", false, linkAvatar);
        AnimationClip attack2 = ImportarClip("sword and shield slash (3).fbx", "Attack2", false, linkAvatar);
        AnimationClip attack3 = ImportarClip("sword and shield slash (5).fbx", "Attack3", false, linkAvatar);
        AnimationClip hit = ImportarClip("sword and shield impact.fbx", "Hit", false, linkAvatar);
        AnimationClip powerUp = ImportarClip("sword and shield power up.fbx", "PowerUp", false, linkAvatar);
        AnimationClip death = ImportarClip("sword and shield death.fbx", "Death", false, linkAvatar);

        if (existente != null) AssetDatabase.DeleteAsset(Controlador);

        AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(Controlador);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("ComboNext", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Blocking", AnimatorControllerParameterType.Bool);
        ac.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("PowerUp", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ac.layers[0].stateMachine;

        // Estados de movimiento con Foot IK activo
        AnimatorState sIdle = sm.AddState("Idle", new Vector3(160, 0, 0));
        sIdle.motion = idle;
        sIdle.iKOnFeet = true;
        sm.defaultState = sIdle;

        AnimatorState sWalk = sm.AddState("Walk", new Vector3(160, 90, 0));
        sWalk.motion = walk;
        sWalk.speed = 1.15f;
        sWalk.iKOnFeet = true;

        AnimatorState sRun = sm.AddState("Run", new Vector3(160, 180, 0));
        sRun.motion = run;
        sRun.speed = 1.05f;
        sRun.iKOnFeet = true;

        AnimatorState sBlock = sm.AddState("Block", new Vector3(420, 180, 0));
        sBlock.motion = block;
        sBlock.iKOnFeet = true;

        // Combo de 3 ataques encadenados
        AnimatorState a1 = sm.AddState("Attack1", new Vector3(520, -40, 0));
        a1.motion = attack1;
        a1.speed = 1.35f;
        a1.iKOnFeet = true;

        AnimatorState a2 = sm.AddState("Attack2", new Vector3(740, -40, 0));
        a2.motion = attack2;
        a2.speed = 1.35f;
        a2.iKOnFeet = true;

        AnimatorState a3 = sm.AddState("Attack3", new Vector3(960, -40, 0));
        a3.motion = attack3;
        a3.speed = 1.30f;
        a3.iKOnFeet = true;

        AnimatorState hitState = sm.AddState("Hit", new Vector3(260, -150, 0));
        hitState.motion = hit;
        hitState.speed = 1.30f;
        hitState.iKOnFeet = true;

        AnimatorState powerState = sm.AddState("PowerUp", new Vector3(480, -150, 0));
        powerState.motion = powerUp;
        powerState.speed = 1.15f;
        powerState.iKOnFeet = true;

        AnimatorState deathState = sm.AddState("Death", new Vector3(40, -150, 0));
        deathState.motion = death;

        // Transiciones entre quieto, caminar y correr
        TransicionFloat(sIdle, sWalk, "Speed", AnimatorConditionMode.Greater, 0.12f, 0.12f);
        TransicionFloat(sWalk, sIdle, "Speed", AnimatorConditionMode.Less, 0.10f, 0.12f);
        TransicionFloat(sWalk, sRun, "Speed", AnimatorConditionMode.Greater, 1.35f, 0.12f);
        TransicionFloat(sRun, sWalk, "Speed", AnimatorConditionMode.Less, 1.25f, 0.12f);
        TransicionFloat(sRun, sIdle, "Speed", AnimatorConditionMode.Less, 0.10f, 0.12f);

        foreach (AnimatorState st in new[] { sIdle, sWalk, sRun })
        {
            AnimatorStateTransition toB = st.AddTransition(sBlock);
            toB.hasExitTime = false;
            toB.hasFixedDuration = true;
            toB.duration = 0.08f;
            toB.AddCondition(AnimatorConditionMode.If, 0, "Blocking");

            AnimatorStateTransition toA = st.AddTransition(a1);
            toA.hasExitTime = false;
            toA.hasFixedDuration = true;
            toA.duration = 0.06f;
            toA.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        }

        // Salidas desde bloqueo
        AnimatorStateTransition blockToIdle = sBlock.AddTransition(sIdle);
        blockToIdle.hasExitTime = false;
        blockToIdle.hasFixedDuration = true;
        blockToIdle.duration = 0.10f;
        blockToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Blocking");
        blockToIdle.AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

        AnimatorStateTransition blockToWalk = sBlock.AddTransition(sWalk);
        blockToWalk.hasExitTime = false;
        blockToWalk.hasFixedDuration = true;
        blockToWalk.duration = 0.10f;
        blockToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "Blocking");
        blockToWalk.AddCondition(AnimatorConditionMode.Greater, 0.12f, "Speed");

        AnimatorStateTransition blockToAtk = sBlock.AddTransition(a1);
        blockToAtk.hasExitTime = false;
        blockToAtk.hasFixedDuration = true;
        blockToAtk.duration = 0.06f;
        blockToAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        // Encadenamiento del combo
        AnimatorStateTransition a1ToA2 = a1.AddTransition(a2);
        a1ToA2.hasExitTime = false;
        a1ToA2.hasFixedDuration = true;
        a1ToA2.duration = 0.08f;
        a1ToA2.AddCondition(AnimatorConditionMode.If, 0, "ComboNext");

        AnimatorStateTransition a2ToA3 = a2.AddTransition(a3);
        a2ToA3.hasExitTime = false;
        a2ToA3.hasFixedDuration = true;
        a2ToA3.duration = 0.08f;
        a2ToA3.AddCondition(AnimatorConditionMode.If, 0, "ComboNext");

        Regreso(a1, sIdle, 0.85f, 0.10f);
        Regreso(a2, sIdle, 0.85f, 0.10f);
        Regreso(a3, sIdle, 0.88f, 0.12f);

        // Estados globales: dano, powerup y muerte
        AnimatorStateTransition anyHit = sm.AddAnyStateTransition(hitState);
        anyHit.hasExitTime = false;
        anyHit.hasFixedDuration = true;
        anyHit.duration = 0.05f;
        anyHit.canTransitionToSelf = false;
        anyHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        Regreso(hitState, sIdle, 0.85f, 0.10f);

        AnimatorStateTransition anyPower = sm.AddAnyStateTransition(powerState);
        anyPower.hasExitTime = false;
        anyPower.hasFixedDuration = true;
        anyPower.duration = 0.08f;
        anyPower.canTransitionToSelf = false;
        anyPower.AddCondition(AnimatorConditionMode.If, 0, "PowerUp");
        Regreso(powerState, sIdle, 0.90f, 0.12f);

        AnimatorStateTransition anyDeath = sm.AddAnyStateTransition(deathState);
        anyDeath.hasExitTime = false;
        anyDeath.hasFixedDuration = true;
        anyDeath.duration = 0.08f;
        anyDeath.canTransitionToSelf = false;
        anyDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        return ac;
    }

    private static bool EsControladorValido(AnimatorController ac)
    {
        if (ac == null || ac.layers == null || ac.layers.Length == 0) return false;
        string[] reqParams = { "Speed", "Attack", "ComboNext", "Blocking", "Hit", "PowerUp", "Death" };
        foreach (string p in reqParams)
        {
            if (!ac.parameters.Any(x => x.name == p)) return false;
        }

        ChildAnimatorState[] states = ac.layers[0].stateMachine.states;
        string[] reqStates = { "Idle", "Walk", "Run", "Block", "Attack1", "Attack2", "Attack3", "Hit", "PowerUp", "Death" };
        foreach (string s in reqStates)
        {
            AnimatorState st = states.Select(x => x.state).FirstOrDefault(x => x.name == s);
            if (st == null || st.motion == null) return false;
        }
        return true;
    }

    private static Avatar AsegurarRigModelo()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModeloLink) as ModelImporter;
        if (importer == null)
        {
            throw new FileNotFoundException("No se encontro el modelo de Link: " + ModeloLink);
        }

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModeloLink).OfType<Avatar>().FirstOrDefault();
        if (importer.animationType != ModelImporterAnimationType.Human || avatar == null)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            avatar = AssetDatabase.LoadAllAssetsAtPath(ModeloLink).OfType<Avatar>().FirstOrDefault();
        }
        return avatar;
    }

    private static AnimationClip ImportarClip(string archivoFbx, string clipName, bool loop, Avatar linkAvatar)
    {
        string path = FbxDir + archivoFbx;
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            throw new FileNotFoundException("Falta animacion FBX de Link: " + path);
        }

        // Copia el avatar exacto de LinkMaximo para mantener proporciones y orientacion
        bool necesitaReimport = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            necesitaReimport = true;
        }
        if (linkAvatar != null && (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther || importer.sourceAvatar != linkAvatar))
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = linkAvatar;
            necesitaReimport = true;
        }
        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            necesitaReimport = true;
        }
        if (necesitaReimport) importer.SaveAndReimport();

        ModelImporterClipAnimation[] fuente = importer.clipAnimations != null && importer.clipAnimations.Length > 0
            ? importer.clipAnimations
            : importer.defaultClipAnimations;

        ModelImporterClipAnimation take = fuente.FirstOrDefault(c =>
            string.Equals(c.takeName, "mixamo.com", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.name, clipName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.name, "mixamo.com", StringComparison.OrdinalIgnoreCase)) ?? fuente.FirstOrDefault();

        if (take == null)
        {
            throw new InvalidOperationException("No se encontro clip de animacion en " + path);
        }

        bool cambiarClip =
            take.name != clipName ||
            take.loopTime != loop ||
            take.loopPose != loop ||
            !take.lockRootRotation ||
            take.keepOriginalOrientation ||
            !take.lockRootHeightY ||
            take.keepOriginalPositionY ||
            !take.heightFromFeet ||
            take.lockRootPositionXZ;

        if (cambiarClip)
        {
            take.name = clipName;
            take.loopTime = loop;
            take.loopPose = loop;
            take.lockRootRotation = true;
            take.keepOriginalOrientation = false;
            take.lockRootHeightY = true;
            take.keepOriginalPositionY = false;
            take.heightFromFeet = true;
            take.lockRootPositionXZ = false;
            importer.clipAnimations = new[] { take };
            importer.SaveAndReimport();
        }

        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == clipName && !c.name.StartsWith("__preview__"))
            ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        if (clip == null || clip.length <= 0f)
        {
            throw new InvalidOperationException("No se pudo cargar el clip " + clipName + " desde " + path);
        }

        return clip;
    }

    private static void TransicionFloat(AnimatorState origen, AnimatorState destino, string param, AnimatorConditionMode modo, float umbral, float duration)
    {
        AnimatorStateTransition t = origen.AddTransition(destino);
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = duration;
        t.AddCondition(modo, umbral, param);
    }

    private static void Regreso(AnimatorState origen, AnimatorState destino, float exitTime, float duration)
    {
        AnimatorStateTransition t = origen.AddTransition(destino);
        t.hasExitTime = true;
        t.exitTime = exitTime;
        t.hasFixedDuration = true;
        t.duration = duration;
    }

    public static void AsegurarModeloLink(Movimiento3D movimiento)
    {
        GameObject assetLink = AssetDatabase.LoadAssetAtPath<GameObject>(ModeloLink);
        if (assetLink == null)
        {
            throw new FileNotFoundException("No se encontro el modelo de Link: " + ModeloLink);
        }

        Transform holder = movimiento.transform.Find("Modelo");
        bool esLink = false;
        if (holder != null && holder.childCount > 0)
        {
            GameObject hijo = holder.GetChild(0).gameObject;
            GameObject origen = PrefabUtility.GetCorrespondingObjectFromSource(hijo);
            esLink = origen == assetLink || hijo.name.StartsWith("LinkMaximo", StringComparison.OrdinalIgnoreCase);
        }

        if (esLink) return;

        if (PrefabUtility.IsPartOfPrefabInstance(movimiento.gameObject))
        {
            PrefabUtility.UnpackPrefabInstance(movimiento.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        if (holder != null) UnityEngine.Object.DestroyImmediate(holder.gameObject);

        GameObject nuevoModelo = PrepararMazmorra.ColocarActor("Modelo", ModeloLink, movimiento.transform.position, 1.85f, movimiento.transform);
        nuevoModelo.transform.localRotation = Quaternion.identity;
        movimiento.gameObject.name = "Player_Link";
    }

    public static void Asignar(Movimiento3D movimiento, AnimatorController controller)
    {
        AsegurarModeloLink(movimiento);

        Transform holder = movimiento.transform.Find("Modelo");
        if (holder == null || holder.childCount == 0)
        {
            throw new InvalidOperationException("No se encontro Modelo/LinkMaximo bajo el jugador.");
        }

        Transform visual = holder.GetChild(0);
        Animator animator = visual.GetComponent<Animator>();
        if (animator == null) animator = visual.gameObject.AddComponent<Animator>();

        Animator antiguo = holder.GetComponent<Animator>();
        if (antiguo != null) antiguo.enabled = false;

        animator.enabled = true;
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModeloLink).OfType<Avatar>().FirstOrDefault();
        if (avatar != null) animator.avatar = avatar;

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GeneradorEquipoLink.Equipar(movimiento);

        EditorUtility.SetDirty(animator);
        if (PrefabUtility.IsPartOfPrefabInstance(animator))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }

        SerializedObject so = new SerializedObject(movimiento);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.ApplyModifiedProperties();
        if (PrefabUtility.IsPartOfPrefabInstance(movimiento))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(movimiento);
        }
    }
}
