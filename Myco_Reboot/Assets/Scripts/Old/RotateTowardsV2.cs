using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTowardsV2 : MonoBehaviour
{
    // The target marker.
    public Transform target;

    public Transform rotator;

    // Angular speed in radians per sec.
    public float speed = 1.0f;

    void LateUpdate()
    {
        // Determine which direction to rotate towards
        Vector3 targetDirection = target.position - rotator.position;
        targetDirection.y = 0f;

        // The step size is equal to speed times frame time.
        float singleStep = speed * Time.deltaTime;

        // Rotate the forward vector towards the target direction by one step
        Vector3 newDirection = Vector3.RotateTowards(rotator.forward, targetDirection, singleStep, 0.0f);

        // Draw a ray pointing at our target in
        Debug.DrawRay(rotator.position, newDirection, Color.red);

        // Calculate a rotation a step closer to the target and applies rotation to this object
        rotator.rotation = Quaternion.LookRotation(newDirection);
    }
}
