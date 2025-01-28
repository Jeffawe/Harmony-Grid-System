using UnityEngine;

namespace HarmonyGridSystem.Objects
{
    public class FloorEdgeObject : MonoBehaviour
    {
        private EdgeType currentEdge;
        private FloorPlacedObject floorPlacedObject;

        public FloorPlacedObject FloorPlacedObject => floorPlacedObject;
        public EdgeType CurrentEdge => currentEdge;

        public void AddEdge(EdgeType edge)
        {
            currentEdge = edge;
        }

        public void AddFloor(FloorPlacedObject floorPlaced)
        {
            floorPlacedObject = floorPlaced;
        }

        public FloorPlacedObject GetFloor()
        {
            return floorPlacedObject;
        }
    }
}
