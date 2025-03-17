using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vegetation : MonoBehaviour {
    private string state = "Healthy";
    private Vector2Int gridCoordinates;

    public Material healthyTrunkMaterial;
    public Material healthyLeavesMaterial;
    public Material burningTrunkMaterial;
    public Material burningLeavesMaterial;
    public Material ashTrunkMaterial;
    public Material ashLeavesMaterial;

    public float probabilityOfFire = 0.0001f;
    public float proximityRadius = 1f; 
    public float ashDisplayTime = 3f;

    [Header("DEBUG :")]
    public Material highlightMaterial;
    public float highlightDuration = 20f; 

    private ObjectStorage objectStorage; 
    private bool fireSpreadEnabled;

    public void Initialize(ObjectStorage storage, bool fireSpreadEnabled) {
        objectStorage = storage;
        this.fireSpreadEnabled = fireSpreadEnabled;

        Vector2Int? coords = objectStorage.GetCoordinates(gameObject);
        if (coords.HasValue) {
            gridCoordinates = coords.Value;
        } else {
            Debug.LogWarning("Vegetation could not retrieve its coordinates from storage!");
        }

        StartCoroutine(CheckFireStatus());
    }

    void OnDestroy() {
        if (objectStorage != null) {
            objectStorage.RemoveObject(gameObject);
        }
    }

    private IEnumerator CheckFireStatus() {
        while (state != "Ash") {
            if (state == "Healthy" && fireSpreadEnabled) {

                if (IsNearbyVegetationBurning()) {
                    state = "Burning";
                    UpdateMaterials();
                    StartCoroutine(Burn());
                }

                else if (Random.value < probabilityOfFire) {
                    state = "Burning";
                    UpdateMaterials();
                    StartCoroutine(Burn());
                }
            }
            yield return new WaitForSeconds(1); 
        }
    }

    private bool IsNearbyVegetationBurning() {
        int gridRange = Mathf.CeilToInt(proximityRadius);

        List<GameObject> nearbyObjects = objectStorage.GetObjectsInRadius(gridCoordinates, gridRange);

        foreach (var obj in nearbyObjects) {
            if (obj != gameObject) {
                Vegetation otherVegetation = obj.GetComponent<Vegetation>();
                if (otherVegetation != null && otherVegetation.state == "Burning") {
                    return true;
                }
            }
        }
        return false;
    }

    private IEnumerator Burn() {
        yield return new WaitForSeconds(2); 
        state = "Ash";
        UpdateMaterials();

        yield return new WaitForSeconds(ashDisplayTime); 
        Destroy(gameObject);
    }

    private void UpdateMaterials() {
        foreach (Transform child in transform) {
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();

            if (meshRenderer != null) {
                if (child.name.ToLower().Contains("trunk")) {
                    switch (state) {
                        case "Healthy":
                            meshRenderer.material = healthyTrunkMaterial;
                            break;
                        case "Burning":
                            meshRenderer.material = burningTrunkMaterial;
                            break;
                        case "Ash":
                            meshRenderer.material = ashTrunkMaterial;
                            break;
                    }
                } else if (child.name.ToLower().Contains("leaves")) {
                    switch (state) {
                        case "Healthy":
                            meshRenderer.material = healthyLeavesMaterial;
                            break;
                        case "Burning":
                            meshRenderer.material = burningLeavesMaterial;
                            break;
                        case "Ash":
                            meshRenderer.material = ashLeavesMaterial;
                            break;
                    }
                }
            }
        }
    }

    private void OnMouseDown() {
        Vector2Int? coordinates = objectStorage.GetCoordinates(gameObject);
        if (coordinates.HasValue)
        {
            Debug.Log("Object clicked: " + gameObject.name);
            objectStorage.HighlightObjectsInRadius(coordinates.Value, Mathf.CeilToInt(proximityRadius), highlightMaterial, highlightDuration);
        }
    }
}
