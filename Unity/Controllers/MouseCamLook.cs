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

    /* Start : Initialise le Rigidbody et verrouille le curseur */
    void Start()
    {
        rb = character.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; // Verrouille le curseur
        Cursor.visible = false;
    }

    /* Update : Gère le regard et le mouvement du personnage */
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    /* Gère la rotation de la caméra en fonction de l'entrée souris */
    void HandleMouseLook()
    {
        // Récupère les valeurs brutes de la souris
        Vector2 md = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // Applique une échelle et un lissage aux valeurs de la souris
        md = Vector2.Scale(md, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        smoothV.x = Mathf.Lerp(smoothV.x, md.x, 1f / smoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, md.y, 1f / smoothing);
        mouseLook += smoothV;

        // Limite la rotation verticale pour éviter un retournement complet
        mouseLook.y = Mathf.Clamp(mouseLook.y, -90f, 90f);

        // Applique la rotation au personnage et à la caméra
        character.transform.localRotation = Quaternion.Euler(0, mouseLook.x, 0);
        transform.localRotation = Quaternion.Euler(-mouseLook.y, 0, 0);
    }

    /* Gère le mouvement du personnage en fonction des entrées clavier */
    void HandleMovement()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += character.transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= character.transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= character.transform.right;
        if (Input.GetKey(KeyCode.D)) move += character.transform.right;

        // Normalise le vecteur pour éviter un gain de vitesse en diagonale
        if (move.magnitude > 1) move.Normalize();

        // Calcule et applique la vitesse de déplacement tout en préservant la gravité
        Vector3 velocity = move * speed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        // Gère le saut si le personnage est au sol
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }

    /* Marque le personnage comme étant au sol lorsqu'il collisionne */
    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    /* Marque le personnage comme étant en l'air lorsqu'il quitte une collision */
    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}
