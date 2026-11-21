using UnityEngine;

// Simula el parpadeo suave de las luces de antorcha en cada sala
[RequireComponent(typeof(Light))]
public class LuzAntorcha : MonoBehaviour
{
    [SerializeField] private float variacion = 0.22f;
    [SerializeField] private float velocidad = 4.5f;

    private Light luz;
    private float intensidadBase;
    private float semilla;

    private void Awake()
    {
        luz = GetComponent<Light>();
        intensidadBase = luz.intensity;
        semilla = transform.position.x * 0.37f + transform.position.z * 0.71f;
    }

    private void Update()
    {
        float ruido = Mathf.PerlinNoise(semilla, Time.time * velocidad) * 2f - 1f;
        luz.intensity = Mathf.Max(0.4f, intensidadBase + ruido * variacion);
    }
}
