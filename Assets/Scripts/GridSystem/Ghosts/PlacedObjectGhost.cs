using System;
using UnityEngine;
using HarmonyGridSystem.Grid;

namespace HarmonyGridSystem.Objects
{
    public class PlacedObjectGhost : GhostObject
    {
        void Start()
        {
            RefreshVisual();
            gridBuildingSystem.OnSelectedChanged += Instance_OnSelectedChanged;
        }

        private void Instance_OnSelectedChanged(object sender, EventArgs e)
        {
            RefreshVisual();
        }

        private void LateUpdate()
        {
            Vector3 targetPos = gridBuildingSystem.GetMouseWorldSnappedPosition();
            targetPos.y = (gridBuildingSystem.GetGridIndex() <= 0) ? 1 : (1 * gridBuildingSystem.GetGridIndex() * gridBuildingSystem.gridManager.GridYSize) + 1;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 15f);
            //transform.rotation = Quaternion.Lerp(transform.rotation, gridBuildingSystem.GetPlacedObjectRotation(), Time.deltaTime * 30f);
            transform.rotation = Quaternion.Lerp(transform.rotation, gridBuildingSystem.GetPlacedObjectRotation(), Time.deltaTime * 50f);
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
            if (placedObjectSO.placedObjectType == PlacedObjectType.FloorObject
                || placedObjectSO.placedObjectType == PlacedObjectType.GridObject)
            {

                visual = Instantiate(placedObjectSO.Visual, Vector3.zero, Quaternion.identity);
                visual.parent = transform;
                visual.localPosition = Vector3.zero;
                visual.localEulerAngles = Vector3.zero;
                if (placedObjectSO.placedObjectType == PlacedObjectType.GridObject) SetLayerRecursive(visual.gameObject);
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
