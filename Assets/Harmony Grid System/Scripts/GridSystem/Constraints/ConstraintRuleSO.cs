using UnityEngine;

namespace HarmonyGridSystem.Objects
{
    public abstract class ConstraintRuleSO : ScriptableObject
    {
        //Prevents this rule from being called more than once
        public abstract bool NoDoubleCalls { get; protected set; }

        public abstract bool ValidatePlacement(PlacedObjectSO objectToPlace, PlacedObjectSO adjacentObject);
    }
}
