using UnityEngine;

[ExecuteInEditMode]
public class GetMainLightDirection : MonoBehaviour
{
    [SerializeField] private Material skyboxMaterial;
    
    // Update is called once per frame
    private void Update()
    {
        skyboxMaterial.SetVector("_MainLightDirection", transform.forward);
    }
}
