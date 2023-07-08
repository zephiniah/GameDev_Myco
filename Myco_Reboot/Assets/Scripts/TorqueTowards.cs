using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorqueTowards : MonoBehaviour
{
    Vector3 targetVector;
    Vector3 playerOrientation;
    public Transform torqueTarget;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //get the vector3 that points from the player to the torque target
        targetVector = torqueTarget.position - transform.position;

        //modifiy this vector to an overground heading
        targetVector.y = 0;

        Debug.DrawRay(transform.position, targetVector, Color.red);
        
        //get a perpendicular vector to apply torque in


    }
}
