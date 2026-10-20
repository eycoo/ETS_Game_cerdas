using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 3f;
    [SerializeField] private float amount = 0.25f;
    [SerializeField] private float speed = 9f;
    [SerializeField] private Transform flame;

    private Light lightSource;
    private float seed;
    private Vector3 flameScale;

    private void Awake()
    {
        lightSource = GetComponent<Light>();
        seed = Random.Range(0f, 100f);
        if (flame != null)
            flameScale = flame.localScale;
    }

    private void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed);
        if (lightSource != null)
            lightSource.intensity = baseIntensity * (1f - amount + n * amount * 2f);
        if (flame != null)
            flame.localScale = new Vector3(flameScale.x * (0.9f + n * 0.2f), flameScale.y * (0.85f + n * 0.3f), flameScale.z * (0.9f + n * 0.2f));
    }
}
