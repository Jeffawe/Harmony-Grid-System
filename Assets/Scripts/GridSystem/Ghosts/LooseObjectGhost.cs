using UnityEngine;
using HarmonyGridSystem.Grid;

namespace HarmonyGridSystem.Objects
{
    public class LooseObjectGhost : GhostObject
    {
        private void Start()
        {
            RefreshVisual();

            gridBuildingSystem.OnSelectedChanged += Instance_OnSelectedChanged;
        }

        private void Instance_OnSelectedChanged(object sender, System.EventArgs e)
        {
            RefreshVisual();
        }

        private void LateUpdate()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                transform.position = Vector3.Lerp(transform.position, raycastHit.point, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, gridBuildingSystem.GetLooseObjectEulerY(), 0), Time.deltaTime * 50f);
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
            if (placedObjectSO.placedObjectType == PlacedObjectType.LooseObject)
            {
                visual = Instantiate(placedObjectSO.Visual, Vector3.zero, Quaternion.identity);
                visual.parent = transform;
                visual.localPosition = Vector3.zero;
                visual.localEulerAngles = Vector3.zero;
                SetLayerRecursive(visual.gameObject);

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
