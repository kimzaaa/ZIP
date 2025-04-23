using UnityEngine;

public class PackageSpin : MonoBehaviour
{
    [SerializeField] private float rotationSpeed;
    private void Update()
    {
        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
    }
}
