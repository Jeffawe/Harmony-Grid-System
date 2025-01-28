using HarmonyGridSystem.Objects;

namespace HarmonyGridSystem.Grid
{
    /// <summary>
    /// Represents a single cell in the grid
    /// </summary>
    public class GridObject
    {
        private Grid3D<GridObject> gridObject;
        private int x;
        private int z;
        private PlacedObject placedObject;

        public GridObject(int x, int z, Grid3D<GridObject> gridObject)
        {
            this.x = x;
            this.z = z;
            this.gridObject = gridObject;
        }

        public PlacedObject GetPlacedObject()
        {
            return placedObject;
        }

        public void SetPlacedObject(PlacedObject _placedObject)
        {
            placedObject = _placedObject;
            gridObject.TriggerOnGridObjectChanged(x, z);
        }

        public void ClearPlacedObject()
        {
            placedObject = null;
            gridObject.TriggerOnGridObjectChanged(x, z);
        }

        /// <summary>
        /// Checks if the Player can build on a particular grid;
        /// </summary>
        /// <returns>true if can build</returns>
        public bool CanBuild(PlacedObjectType placedObjectType = PlacedObjectType.GridObject)
        {
            if (placedObject == null) return true;

            if (placedObjectType == PlacedObjectType.FloorObject)
            {
                if (placedObject.placedObjectType == PlacedObjectType.GridObject) return true;
            }

            return false;
        }

        public override string ToString()
        {
            return x + "," + z;
        }
    }
}
    
