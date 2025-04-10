using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class StoredObject
{
    public Vector2Int Coordinate;
    public string ObjectType;

    /* Constructeur : Initialise un objet stocké avec ses coordonnées et son type */
    public StoredObject(Vector2Int coord, string objectType)
    {
        Coordinate = coord;
        ObjectType = objectType;
    }
}

[System.Serializable]
public class StoredObjectPair
{
    public StoredObject storedData;
    [System.NonSerialized]
    public GameObject instance;

    /* Constructeur : Associe les données stockées à l'instance de GameObject correspondante */
    public StoredObjectPair(StoredObject data, GameObject obj)
    {
        storedData = data;
        instance = obj;
    }
}

public class ObjectStorage : MonoBehaviour
{
    private static ObjectStorage _instance;
    public static ObjectStorage Instance
    {
        get
        {
            // Vérifier s'il existe déjà une instance d'ObjectStorage
            if (_instance == null)
            {
                GameObject storageObj = GameObject.Find("ObjectStorage");
                if (storageObj == null)
                {
                    // Créer un nouvel objet de stockage s'il n'existe pas
                    storageObj = new GameObject("ObjectStorage");
                    _instance = storageObj.AddComponent<ObjectStorage>();
                    DontDestroyOnLoad(storageObj);
                }
                else
                {
                    _instance = storageObj.GetComponent<ObjectStorage>();
                }
            }
            return _instance;
        }
    }

    [SerializeField]
    private List<StoredObjectPair> storedPairs = new List<StoredObjectPair>();

    /* Retourne ou crée l'objet parent pour le prefab donné */
    public GameObject GetOrCreateParentObject(GameObject prefab)
    {
        // Définir le nom du parent basé sur le nom du prefab
        string parentName = prefab.name + "_Parent";

        // Chercher l'objet parent dans la scène
        GameObject parent = GameObject.Find(parentName);

        // Créer l'objet parent s'il n'existe pas
        if (parent == null)
            parent = new GameObject(parentName);
        return parent;
    }

    /* Ajoute un objet à la liste de stockage avec ses coordonnées et son type */
    public void AddObject(GameObject obj, Vector2Int coord, string objectType)
    {
        // Renommer l'objet pour inclure le type et les coordonnées
        obj.name = objectType + "_" + coord.x + "_" + coord.y;

        // Créer une nouvelle instance de StoredObject
        StoredObject newStored = new StoredObject(coord, objectType);
        
        // Ajouter la paire (données + instance) à la liste de stockage
        storedPairs.Add(new StoredObjectPair(newStored, obj));
    }

    /* Supprime un objet de la liste de stockage */
    public void RemoveObject(GameObject obj)
    {
        // Supprimer toutes les paires dont l'instance correspond à l'objet donné
        storedPairs.RemoveAll(pair => pair.instance == obj);
    }

    /* Retourne tous les objets stockés d'un type donné */
    public List<StoredObject> GetAllObjects(string objectType)
    {
        // Filtrer et retourner les objets qui correspondent au type donné
        return storedPairs.Where(pair => pair.storedData.ObjectType == objectType)
                          .Select(pair => pair.storedData)
                          .ToList();
    }

    /* Retourne l'instance de GameObject associée à des données stockées spécifiques */
    public GameObject GetGameObjectFor(StoredObject stored)
    {
        // Chercher la paire correspondant aux données stockées
        var pair = storedPairs.FirstOrDefault(p =>
            p.storedData.Coordinate == stored.Coordinate &&
            p.storedData.ObjectType == stored.ObjectType);
        return pair != null ? pair.instance : null;
    }

    /* Retourne les coordonnées associées à un objet donné, si elles existent */
    public Vector2Int? GetCoordinates(GameObject obj)
    {
        // Rechercher l'objet dans la liste des paires stockées
        foreach (var pair in storedPairs)
        {
            if (pair.instance == obj)
                return pair.storedData.Coordinate;
        }
        return null;
    }

    /* Récupère tous les GameObject situés dans un rayon donné autour d'un centre */
    public List<GameObject> GetObjectsInRadius(Vector2Int center, int radius)
    {
        List<GameObject> nearObjects = new List<GameObject>();

        int sqrRadius = radius * radius;

        // Parcourir chaque paire stockée pour vérifier la distance au centre
        foreach (var pair in storedPairs)
        {
            Vector2Int coord = pair.storedData.Coordinate;
            int deltaX = coord.x - center.x;
            int deltaY = coord.y - center.y;
            if ((deltaX * deltaX + deltaY * deltaY) <= sqrRadius)
                nearObjects.Add(pair.instance);
        }
        return nearObjects;
    }

    /* Coroutine : Réinitialise la surbrillance d'un MeshRenderer après un délai */
    private System.Collections.IEnumerator ResetHighlight(MeshRenderer renderer, Material originalMaterial, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Réappliquer le matériau original si le renderer existe toujours
        if (renderer != null)
            renderer.material = originalMaterial;
    }

    /* Efface tous les objets stockés */
    public void Clear()
    {
        // Vider la liste des paires stockées
        storedPairs.Clear();
    }
}
