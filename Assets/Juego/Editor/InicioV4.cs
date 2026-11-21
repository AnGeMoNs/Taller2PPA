using System.IO;
using UnityEditor;
using UnityEngine;

// Ofrece crear la escena al abrir el proyecto y valida que no falten referencias
public static class InicioV4
{
    private const string Escena = "Assets/Juego/Scenes/MazmorraV4.unity";

    [InitializeOnLoadMethod]
    private static void AlAbrirEditor()
    {
        if (Application.isBatchMode || AssetDatabase.IsAssetImportWorkerProcess()) return;
        EditorApplication.delayCall += OfrecerMontaje;
    }

    private static void OfrecerMontaje()
    {
        if (File.Exists(Escena) || SessionState.GetBool("Mazmorra.Link.Ofrecido", false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;

        SessionState.SetBool("Mazmorra.Link.Ofrecido", true);
        if (EditorUtility.DisplayDialog("La Mazmorra de Link", "¿Crear la escena de la mazmorra con Link, los cuatro puzzles y la Reliquia Sagrada? Se guardará como MazmorraV4.unity.", "Crear escena", "Más tarde"))
        {
            PrepararMazmorra.CrearEscena();
        }
    }

    [MenuItem("Taller 2/5. Comprobar referencias")]
    public static void Validar()
    {
        MazmorraPuzzles puzzles = Object.FindFirstObjectByType<MazmorraPuzzles>();
        if (puzzles == null)
        {
            Debug.LogError("Abre la escena MazmorraV4.");
            return;
        }

        SerializedObject so = new SerializedObject(puzzles);
        string[] campos =
        {
            "guardianes", "cajas", "placas", "placasPresion", "palancas", "palancasAnimadas",
            "cristalesSecuencia", "controlesTrampa", "antorchasCombate", "recompensas",
            "pedestales", "sellosAltar", "conductosAltar", "trampas", "interacciones"
        };
        int[] cantidades = { 2, 2, 2, 2, 3, 3, 3, 2, 2, 4, 4, 4, 4, 3, 15 };

        int problemas = 0;
        for (int i = 0; i < campos.Length; i++)
        {
            SerializedProperty arreglo = so.FindProperty(campos[i]);
            if (arreglo == null || arreglo.arraySize != cantidades[i])
            {
                Debug.LogError("Cantidad incorrecta: " + campos[i]);
                problemas++;
                continue;
            }
            for (int j = 0; j < arreglo.arraySize; j++)
            {
                if (arreglo.GetArrayElementAtIndex(j).objectReferenceValue == null)
                {
                    Debug.LogError("Referencia vacia: " + campos[i] + "/" + j);
                    problemas++;
                }
            }
        }

        foreach (string campo in new[] { "puertaFinal", "reliquia" })
        {
            if (so.FindProperty(campo).objectReferenceValue == null)
            {
                Debug.LogError("Falta " + campo);
                problemas++;
            }
        }

        if (Object.FindFirstObjectByType<GameManager>() == null)
        {
            Debug.LogError("Falta GameManager");
            problemas++;
        }

        if (problemas == 0)
        {
            Debug.Log("Referencias completas. Todo listo para presionar Play y jugar con Link.");
        }
    }
}
