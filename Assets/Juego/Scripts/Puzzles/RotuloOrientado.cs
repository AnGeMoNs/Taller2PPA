using UnityEngine;

// Orienta los letreros de texto en el mundo para que siempre miren hacia la camara
public class RotuloOrientado : MonoBehaviour
{
    private Camera camaraPrincipal;

    private void Start()
    {
        camaraPrincipal = Camera.main;
    }

    private void LateUpdate()
    {
        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
        if (camaraPrincipal != null)
        {
            transform.rotation = camaraPrincipal.transform.rotation;
        }
    }
}
