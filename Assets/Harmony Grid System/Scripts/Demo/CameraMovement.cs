using UnityEngine;
using HarmonyGridSystem.Utils;

public class CameraMovement : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] float rotSpeed;

    private Vector3 rotateValue;
    

    private void Update()
    {
        float rotY = 0f;
        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDir = transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDir = -transform.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDir = -transform.right;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDir = transform.right;
        }
        if(Input.GetKey(KeyCode.Z))
        {
            moveDir = transform.up;
        }
        if (Input.GetKey(KeyCode.X))
        {
            moveDir = -transform.up;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            rotY = -1f;
        }
        if (Input.GetKey(KeyCode.E))
        {
            rotY = +1f;
        }


        Vector3 newMoveDir = UtilsClass.ApplyRotationToVectorXZ(moveDir, 30f);

        transform.localPosition += newMoveDir * moveSpeed * Time.deltaTime;

        rotateValue = new Vector3(0, rotY * -1, 0) * rotSpeed;
        rotateValue = UtilsClass.ApplyRotationToVectorXZ(rotateValue, 30f);
        transform.eulerAngles -= rotateValue;

        //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(rotateValue), 2 * Time.deltaTime);
        
    }
}
