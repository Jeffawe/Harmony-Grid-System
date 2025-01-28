using UnityEngine;
using HarmonyGridSystem.Grid;

namespace HarmonyGridSystem.Objects
{
    public abstract class GhostObject : MonoBehaviour
    {
        public GridBuildingSystem3D gridBuildingSystem;

        protected Transform visual;
        protected PlacedObjectSO placedObjectSO;

        protected virtual void RefreshVisual() { }
    }
}
