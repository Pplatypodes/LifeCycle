using UnityEngine;

public class MinimapToggle : MonoBehaviour
{
    // Référence au canvas de la minimap.
    public GameObject minimapCanvas;

    void Start()
    {
        if (minimapCanvas == null)
        {
            Debug.LogError("MinimapToggle: No minimap canvas assigned!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            // Alterne l'état actif du canvas.
            minimapCanvas.SetActive(!minimapCanvas.activeSelf);
        }
    }
}
