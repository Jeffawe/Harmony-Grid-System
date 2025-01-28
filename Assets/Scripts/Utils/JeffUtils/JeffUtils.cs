using UnityEngine;

namespace Jeff.Utilities
{
    public static class JeffUtils
    {
        /// <summary>
        /// Changes either the X, Y or Z of a Vector3 or transform
        /// 0 means X axis, 1 
        /// </summary>
        /// <param name="objectTransform">The transform to change. Input null if you just want to create a new Vector from a Vector zero</param>
        /// <param name="dimensionInteger">The dimension to change. 0 means X axis, 1 means Y axis and 2 means Z axis</param>
        /// <param name="newValue">the new value to add or multiply</param>
        /// <param name="shouldOverride">If the new value should override the existing on that axis or add or multiply to it</param>
        /// <returns></returns>
        public static Vector3 ChangeXYZ(Transform objectTransform, int dimensionInteger, float newValue, bool shouldOverride = false)
        {
            Vector3 transformVector = Vector3.zero;

            if(objectTransform != null) transformVector = objectTransform.position;

            float x = transformVector.x;
            float y = transformVector.y;
            float z = transformVector.z;

            if (shouldOverride)
            {
                if (dimensionInteger == 0) transformVector = new Vector3(newValue, y, z);
                if (dimensionInteger == 1) transformVector = new Vector3(x, newValue, z);
                if (dimensionInteger == 2) transformVector = new Vector3(x, y, newValue);
            }
            else
            {
                if (dimensionInteger == 0) transformVector = new Vector3(x + newValue, y, z);
                if (dimensionInteger == 1) transformVector = new Vector3(x, y + newValue, z);
                if (dimensionInteger == 2) transformVector = new Vector3(x, y, z + newValue);
            }


            return transformVector;
        }
    }
}