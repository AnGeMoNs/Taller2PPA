using UnityEngine;

// Mantiene los letreros 3D orientados hacia la camara principal
public class RotuloOrientado : MonoBehaviour
{
    #region CLASS_VARIABLES

    private Camera camaraPrincipal;

    #endregion

    #region UNITY_METHODS

    private void Start()
    {
        camaraPrincipal = Camera.main;
    }

    private void LateUpdate()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }
        if (camaraPrincipal != null)
        {
            transform.rotation = camaraPrincipal.transform.rotation;
        }
    }

    #endregion
}
