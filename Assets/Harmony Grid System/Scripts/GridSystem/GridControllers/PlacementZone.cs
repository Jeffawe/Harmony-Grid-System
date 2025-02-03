using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlacementZone : MonoBehaviour
{
    private Collider zoneCollider;
    public GameObject placedObject;
    public List<string> allowedTags;

    private void Awake()
    {
        // Try to get an existing collider
        zoneCollider = GetComponent<Collider>();

        // If no collider is found, add a BoxCollider as the default
        if (zoneCollider == null)
        {
            zoneCollider = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("No collider found on PlacementZone. Added a default BoxCollider.");
        }

        // Ensure the collider is a trigger (optional)
        zoneCollider.isTrigger = true;
    }

    public bool CanPlaceObject(GameObject obj)
    {
        if (allowedTags.Count <= 0) return true;
        return allowedTags.Contains(obj.tag);
    }

    public void PlaceObject(GameObject obj)
    {
        if (CanPlaceObject(obj))
        {
            placedObject = obj;
            obj.transform.position = zoneCollider.bounds.center;
            obj.transform.parent = this.transform;
        }
    }

    public void RemoveObject()
    {
        if (placedObject != null)
        {
            Destroy(placedObject);
            placedObject = null;
        }
    }
}