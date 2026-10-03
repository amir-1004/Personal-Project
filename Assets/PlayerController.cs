using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    Rigidbody rb;
    bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (isGrounded && Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.upArrowKey.wasPressedThisFrame ||
             Keyboard.current.wKey.wasPressedThisFrame))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        float horizontal = 0f;
        bool canMove = GameManager.Instance == null || !GameManager.Instance.IsGameOver;
        if (canMove && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.x = horizontal * moveSpeed;
        rb.linearVelocity = velocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Destroy(collision.gameObject);
            if (GameManager.Instance != null) GameManager.Instance.LoseLife();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickup"))
        {
            Debug.Log("Player collected: " + other.gameObject.name);
            Destroy(other.gameObject);
            if (GameManager.Instance != null) GameManager.Instance.AddLife();
        }
    }
}
