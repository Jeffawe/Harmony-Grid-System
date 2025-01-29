using UnityEngine;
using HarmonyGridSystem.Grid;

namespace HarmonyGridSystem.Objects
{
    public class WallObjectGhost : GhostObject
    {
        Vector3 lastKnownPos;
        Quaternion lastKnownRot;

        private void Start()
        {
            RefreshVisual();
            lastKnownPos = Vector3.zero;
            lastKnownRot = Quaternion.identity;

            gridBuildingSystem.OnSelectedChanged += Instance_OnSelectedChanged;
        }

        private void Instance_OnSelectedChanged(object sender, System.EventArgs e)
        {
            RefreshVisual();
        }

        private void LateUpdate()
        {
            FloorEdgePosition floorEdgePosition = gridBuildingSystem.GetMouseFloorEdgePosition();
            if (floorEdgePosition != null)
            {
                lastKnownPos = floorEdgePosition.transform.position;
                lastKnownRot = floorEdgePosition.transform.rotation;
                transform.position = Vector3.Lerp(transform.position, floorEdgePosition.transform.position, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Lerp(transform.rotation, floorEdgePosition.transform.rotation, Time.deltaTime * 25f);
            }
            else
            {
                Vector3 targetPosition = gridBuildingSystem.GetMouseWorldSnappedPosition();
                if (lastKnownPos != Vector3.zero) transform.position = Vector3.Lerp(transform.position, lastKnownPos, Time.deltaTime * 15f);
                else transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);

                if (lastKnownRot != Quaternion.identity) transform.rotation = Quaternion.Lerp(transform.rotation, lastKnownRot, Time.deltaTime * 25f);
                else transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 25f);
            }
        }

        protected override void RefreshVisual()
        {
            if (visual != null)
            {
                Destroy(visual.gameObject);
                visual = null;
            }


            placedObjectSO = gridBuildingSystem.GetPlacedObjectTypeSO();
            if (placedObjectSO == null) return;
            if (placedObjectSO.placedObjectType == PlacedObjectType.WallObject)
            {
                visual = Instantiate(placedObjectSO.Visual, Vector3.zero, Quaternion.identity);
                visual.parent = transform;
                visual.localPosition = Vector3.zero;
                visual.localEulerAngles = Vector3.zero;
                //SetLayerRecursive(visual.gameObject);

            }
        }

        private void SetLayerRecursive(GameObject targetGameObject)
        {
            Renderer renderer = targetGameObject.GetComponent<Renderer>();
            if (renderer != null) targetGameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.cyan);
            foreach (Transform child in targetGameObject.transform)
            {
                SetLayerRecursive(child.gameObject);
            }
        }
    }
}
