using System.Collections.Generic;
using UnityEngine;

// Ajusta la altura visual para modelos Generic y se desactiva en rigs Humanoid
[DisallowMultipleComponent]
public class AjusteVisualSuelo : MonoBehaviour
{
    [SerializeField] private float frecuencia = 0.12f;
    [SerializeField] private float ajusteMaximo = 1.8f;

    private Transform modelo;
    private Animator animator;
    private SkinnedMeshRenderer[] mallas;
    private Mesh mallaTemporal;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private Vector3 posicionBase;
    private float alturaObjetivo;
    private float siguienteRevision;

    private void Awake()
    {
        modelo = transform.Find("Modelo");
        if (modelo == null)
        {
            enabled = false;
            return;
        }

        animator = modelo.GetComponentInChildren<Animator>();
        // En personajes Humanoid los pies ya se alinean con Foot IK y heightFromFeet
        if (animator != null && animator.isHuman)
        {
            enabled = false;
            return;
        }

        posicionBase = modelo.localPosition;
        mallas = modelo.GetComponentsInChildren<SkinnedMeshRenderer>();
        mallaTemporal = new Mesh();
    }

    private void LateUpdate()
    {
        if (modelo == null || mallas == null || mallas.Length == 0) return;

        if (Time.unscaledTime >= siguienteRevision)
        {
            siguienteRevision = Time.unscaledTime + frecuencia;
            float minimo = float.PositiveInfinity;

            foreach (SkinnedMeshRenderer malla in mallas)
            {
                if (malla == null || malla.sharedMesh == null || !malla.enabled) continue;
                malla.BakeMesh(mallaTemporal);
                mallaTemporal.GetVertices(vertices);
                for (int i = 0; i < vertices.Count; i++)
                {
                    float y = transform.InverseTransformPoint(malla.transform.TransformPoint(vertices[i])).y;
                    if (y < minimo) minimo = y;
                }
            }

            if (!float.IsInfinity(minimo))
            {
                alturaObjetivo = Mathf.Clamp(modelo.localPosition.y - posicionBase.y - minimo, -ajusteMaximo, ajusteMaximo);
            }
        }

        Vector3 pos = modelo.localPosition;
        pos.y = Mathf.Lerp(pos.y, posicionBase.y + alturaObjetivo, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        modelo.localPosition = pos;
    }

    private void OnDestroy()
    {
        if (mallaTemporal != null) Destroy(mallaTemporal);
    }
}
