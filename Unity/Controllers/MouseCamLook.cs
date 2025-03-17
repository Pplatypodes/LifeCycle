using UnityEngine;

public class MouseCamLook : MonoBehaviour
{
    public GameObject character;
    
    [SerializeField] private float sensitivity = 5.0f;
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float jumpForce = 5.0f;

    private Rigidbody rb;
    private Vector2 mouseLook;
    private Vector2 smoothV;
    private float smoothing = 2.0f;
    private bool isGrounded;

    void Start()
    {
        rb = character.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; // Lock cursor in place
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        // Get raw mouse input
        Vector2 md = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // Scale and smooth input
        md = Vector2.Scale(md, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        smoothV.x = Mathf.Lerp(smoothV.x, md.x, 1f / smoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, md.y, 1f / smoothing);
        mouseLook += smoothV;

        // Clamp vertical look angle to prevent flipping
        mouseLook.y = Mathf.Clamp(mouseLook.y, -90f, 90f);

        // Apply camera rotation
        character.transform.localRotation = Quaternion.Euler(0, mouseLook.x, 0);
        transform.localRotation = Quaternion.Euler(-mouseLook.y, 0, 0);
    }

    void HandleMovement()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += character.transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= character.transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= character.transform.right;
        if (Input.GetKey(KeyCode.D)) move += character.transform.right;

        // Normalize to prevent diagonal speed boost
        if (move.magnitude > 1) move.Normalize();

        // Apply movement using Rigidbody
        Vector3 velocity = move * speed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z); // Preserve gravity

        // Jumping
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
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
}
