using UnityEngine;

public class FollowCamera : MonoBehaviour {
    public Transform playerCamera;
    public float followSpeed = 10f;

    /* Update : Suit la position de la caméra du joueur en interpolant sa position */
    private void Update() {
        Vector3 newPosition = playerCamera.position;
        transform.position = Vector3.Lerp(transform.position, newPosition, followSpeed * Time.deltaTime);
    }
}
