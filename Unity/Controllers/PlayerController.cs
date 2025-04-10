/* 
 * author : jiankaiwang
 * description : The script provides you with basic operations of first personal control.
 * platform : Unity
 * date : 2017/12
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public float speed = 10.0f;
    private float translation;
    private float straffe;

    /* Démarrage : Verrouille le curseur */
    void Start () {
        // Désactive le curseur
        Cursor.lockState = CursorLockMode.Locked;		
	}
    
    /* Update : Gère les déplacements du joueur et le déverrouillage du curseur */
	void Update () {
        // Récupère l'entrée utilisateur pour avancer/reculer et strafing
        translation = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        straffe = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        
        // Déplace le joueur selon les axes horizontal et vertical
        transform.Translate(straffe, 0, translation);

        if (Input.GetKeyDown("escape")) {
            // Réactive le curseur
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
