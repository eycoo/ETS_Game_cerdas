using UnityEngine;

public class Bobbing : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.15f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float spinSpeed;

    private Vector3 basePos;
    private float phase;

    private void Start()
    {
        basePos = transform.localPosition;
        phase = Random.Range(0f, 6f);
    }

    private void Update()
    {
        transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
        if (spinSpeed != 0f)
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }
}
