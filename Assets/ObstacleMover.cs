using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float speed = 5f;
    public float zDestroyPosition = -5f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        rb.AddForce(Vector3.back * speed, ForceMode.VelocityChange);
    }

    void Update()
    {
        if (transform.position.z <= zDestroyPosition)
        {
            Destroy(gameObject);
        }
    }
}
