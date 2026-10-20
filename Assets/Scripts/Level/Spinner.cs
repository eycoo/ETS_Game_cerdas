using UnityEngine;

public class Spinner : MonoBehaviour
{
    [SerializeField] private Vector3 axis = Vector3.up;
    [SerializeField] private float speed = 30f;

    private void Update()
    {
        transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
    }
}
