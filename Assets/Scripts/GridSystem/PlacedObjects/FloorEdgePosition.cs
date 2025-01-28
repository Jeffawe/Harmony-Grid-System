using UnityEngine;

namespace HarmonyGridSystem.Objects
{
    public class FloorEdgePosition : MonoBehaviour
    {
        public EdgeType edge;

        public void SetEdge(EdgeType _edge)
        {
            edge = _edge;
        }
    }
}
