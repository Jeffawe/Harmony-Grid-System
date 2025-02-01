using UnityEngine;

namespace HarmonyGridSystem.Objects
{

    [CreateAssetMenu(fileName = "AdjacencyRule", menuName = "Harmony Grid System/Constraints/Adjacency Rule")]
    public class AdjacencyRuleSO : ConstraintRuleSO
    {
        public override bool NoDoubleCalls { get; protected set; } = true;

        public override bool ValidatePlacement(PlacedObjectSO objectToPlace, PlacedObjectSO adjacentObject)
        {
            return objectToPlace.allowedAdjacentObjects.Contains(adjacentObject.nameString) ||
                   objectToPlace.allowedAdjacentObjects.Contains(adjacentObject.constraintGroupName) ||
                   adjacentObject.allowedAdjacentObjects.Contains(objectToPlace.nameString) ||
                   adjacentObject.allowedAdjacentObjects.Contains(objectToPlace.constraintGroupName);
        }
    }
}
