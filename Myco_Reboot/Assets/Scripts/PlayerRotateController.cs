using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

//Prototype: Rotates player object via torque forces. Uses mouse input from the input system.

public class PlayerRotateController : MonoBehaviour
{

    public Rigidbody rb;
    public Vector2 mouseVector;

    public float mouseLookX;
    public float mouseLookY;

    public float lookDrag = 0.5f;
    public float lookStop = 0.8f;

    public float lookThreshold = 1.0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnLook(InputValue lookVector2)
    {
        mouseVector = lookVector2.Get<Vector2>();

        mouseLookX = mouseVector.x;

        mouseLookY = mouseVector.y;
    }

    private void FixedUpdate()
    {
        if(mouseVector.magnitude > lookThreshold)
        {
            rb.angularDrag = lookDrag;
            //mouseLookY rotates around the rb X axis, mouseLookX rotate around the rb Y axis
            rb.AddTorque(mouseLookY, mouseLookX, 0.0f, ForceMode.Force);
        }
        else
        {
            rb.angularDrag = lookStop;
        }
        //mouseLookY rotates around the rb X axis, mouseLookX rotate around the rb Y axis
        //rb.AddTorque(mouseLookY, mouseLookX, 0.0f, ForceMode.Force);

    }


}