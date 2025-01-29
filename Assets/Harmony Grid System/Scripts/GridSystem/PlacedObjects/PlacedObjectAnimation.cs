using UnityEngine;
using HarmonyGridSystem.Grid;

namespace HarmonyGridSystem.Objects
{
    public class PlacedObjectAnimation : MonoBehaviour
    {
        public AnimationCurve AnimationCurve
        {
            get
            {
                if (GridManager.Instance == null) return null;
                return GridManager.Instance.AnimationCurve;
            }
        }

        private float time;

        private void Update()
        {
            if (AnimationCurve == null) return;
            time += Time.deltaTime;

            transform.localScale = new Vector3(1, AnimationCurve.Evaluate(time), 1);
        }
    }
}
