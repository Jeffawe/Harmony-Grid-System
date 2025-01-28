using UnityEngine;

namespace HarmonyGridSystem.Grid
{
    public class InputController : MonoBehaviour
    {
        public static bool GetLeftMouseButton()
        {
            return Input.GetMouseButton(0);
        }

        public static bool GetRightMouseButton() { return Input.GetMouseButton(1); }

        public static Vector3 GetMousePosition()
        {
            return Input.mousePosition;
        }

        public static bool ChangeObject()
        {
            return Input.GetKeyDown(KeyCode.O);
        }

        public static bool DeselectObject()
        {
            return Input.GetKey(KeyCode.V);
        }

        public static bool EditObject()
        {
            return Input.GetKeyDown(KeyCode.B);
        }

        public static bool RotateObject()
        {
            return Input.GetKeyDown(KeyCode.R);
        }

        public static bool ChangeCurrentGrid()
        {
            return Input.GetKeyDown(KeyCode.G);
        }
    }
}
