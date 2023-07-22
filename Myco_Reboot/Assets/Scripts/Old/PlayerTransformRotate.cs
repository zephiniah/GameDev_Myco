using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTransformRotate : MonoBehaviour
{

    //Actually just controls the camera by rotating a the transform of a child object that the camera is following

    public Transform rotateTransform;
    public Vector2 mouseVector;

    Vector3 currentEulerAngles;

    public float mouseLookX;
    public float mouseLookY;
    public float mouseLookZ = 0f;

    public float lookTune = 0.5f;

    //public float xLookClamp = 80f;
    //public float yLookClamp = 80f;
    //public float lookStop = 0.8f;

    //public float lookThreshold = 1.0f;

    void Awake()
    {
        //rotateTransform = GetComponent<Transform>();
    }

    private void OnLook(InputValue lookVector2)
    {
        mouseVector = lookVector2.Get<Vector2>();

        mouseLookX = mouseVector.x;

        mouseLookY = mouseVector.y;
    }

    // Update is called once per frame
    void Update()
    {
        currentEulerAngles += new Vector3(mouseLookY, mouseLookX, mouseLookZ) * Time.deltaTime * lookTune;
        rotateTransform.eulerAngles = currentEulerAngles;
        //rotateTransform.Rotate(mouseLookY * lookTune * Time.deltaTime, mouseLookX * lookTune * Time.deltaTime, 0f, Space.World);
    }
}
