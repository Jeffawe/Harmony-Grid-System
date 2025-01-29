using UnityEngine;
using Jeff.Utilities;

namespace HarmonyGridSystem.Objects
{
    public class FloorPlacedObject : PlacedObject
    {
        [SerializeField] private FloorEdgePosition upFloorEdgePosition;
        [SerializeField] private FloorEdgePosition downFloorEdgePosition;
        [SerializeField] private FloorEdgePosition leftFloorEdgePosition;
        [SerializeField] private FloorEdgePosition rightFloorEdgePosition;

        private FloorEdgeObject upEdgeObject;
        private FloorEdgeObject downEdgeObject;
        private FloorEdgeObject leftEdgeObject;
        private FloorEdgeObject rightEdgeObject;


        public void PlaceEdge(EdgeType edge, PlacedObjectSO placedObjectSO)
        {
            FloorEdgePosition floorEdgePosition = GetFloorEdgePosition(edge);

            Transform floorEdgeObjectTransform = Instantiate(placedObjectSO.prefab, floorEdgePosition.transform.position, floorEdgePosition.transform.rotation);
            placedObjectSO.Prefab.GetComponent<ChildHolder>()?.SetSO(placedObjectSO);
            Vector2 offsetVector = CalculateOffset(floorEdgeObjectTransform.rotation.eulerAngles.y, 0.1f);
            floorEdgeObjectTransform.transform.position = JeffUtils.ChangeXYZ(floorEdgeObjectTransform, 0, offsetVector.x);
            floorEdgeObjectTransform.transform.position = JeffUtils.ChangeXYZ(floorEdgeObjectTransform, 2, offsetVector.y);

            FloorEdgeObject currentFloorEdgeObject = GetFloorEdgeObject(edge);

            //Replaces it with the current one
            if (currentFloorEdgeObject != null)
            {
                Destroy(currentFloorEdgeObject.gameObject);
            }

            FloorEdgeObject _FloorEdgeObject = floorEdgeObjectTransform.GetComponent<FloorEdgeObject>();
            _FloorEdgeObject.AddEdge(edge);
            _FloorEdgeObject.AddFloor(this);
            SetFloorEdgeObject(edge, _FloorEdgeObject);
        }

        public void RemoveEdge(EdgeType edge, Transform gameObject)
        {
            SetFloorEdgeObject(edge, null);
            Destroy(gameObject.gameObject);
        }

        public Vector2 CalculateOffset(float newAngle, float size)
        {
            Vector2 newVector = Vector2.zero;

            if (newAngle == 0 || newAngle == 180)
            {
                float operation = (newAngle == 0) ? -1 : 1;
                newVector = new(0, size * operation);
            }
            else if (newAngle == 90 || newAngle == 270)
            {
                float operation = (newAngle == 90) ? -1 : 1;
                newVector = new(size * operation, 0);
            }

            return newVector;
        }

        private FloorEdgePosition GetFloorEdgePosition(EdgeType edge)
        {
            switch (edge)
            {
                default:
                case EdgeType.Up: return upFloorEdgePosition;
                case EdgeType.Down: return downFloorEdgePosition;
                case EdgeType.Left: return leftFloorEdgePosition;
                case EdgeType.Right: return rightFloorEdgePosition;
            }
        }

        public void SetEdgePositions(FloorEdgePosition up, FloorEdgePosition down, FloorEdgePosition left, FloorEdgePosition right)
        {
            this.upFloorEdgePosition = up;
            downFloorEdgePosition = down;
            leftFloorEdgePosition = left;
            rightFloorEdgePosition = right;
        }

        private void SetFloorEdgeObject(EdgeType edge, FloorEdgeObject FloorEdgeObject)
        {
            switch (edge)
            {
                default:
                case EdgeType.Up:
                    upEdgeObject = FloorEdgeObject;
                    break;
                case EdgeType.Down:
                    downEdgeObject = FloorEdgeObject;
                    break;
                case EdgeType.Left:
                    leftEdgeObject = FloorEdgeObject;
                    break;
                case EdgeType.Right:
                    rightEdgeObject = FloorEdgeObject;
                    break;
            }
        }

        private FloorEdgeObject GetFloorEdgeObject(EdgeType edge)
        {
            switch (edge)
            {
                default:
                case EdgeType.Up:
                    return upEdgeObject;
                case EdgeType.Down:
                    return downEdgeObject;
                case EdgeType.Left:
                    return leftEdgeObject;
                case EdgeType.Right:
                    return rightEdgeObject;
            }
        }


        public override void DestroyGameObject()
        {
            if (upEdgeObject != null) Destroy(upEdgeObject.gameObject);
            if (downEdgeObject != null) Destroy(downEdgeObject.gameObject);
            if (leftEdgeObject != null) Destroy(leftEdgeObject.gameObject);
            if (rightEdgeObject != null) Destroy(rightEdgeObject.gameObject);

            base.DestroyGameObject();
        }
    }

    public enum EdgeType
    {
        Up,
        Down,
        Left,
        Right
    }
}
