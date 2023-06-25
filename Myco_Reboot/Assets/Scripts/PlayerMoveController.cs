using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


//This script is for moving a player character via WASD and for "flying" via Spacebar
public class PlayerMoveController : MonoBehaviour
{

    private Rigidbody rb;

//Movement
    private float movementX;
    private float movementY;
    private float movementFly;
    public float speed = 1f;
    public float lift = 1f;



    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnMove(InputValue movementValue)
    {
        //WASD style movement implemented with the input system
        Vector2 movementVector = movementValue.Get<Vector2>();

        movementX = movementVector.x;
        movementY = movementVector.y;

    }

    private void OnFly(InputValue flyValue)
    {
        //Simple fly controls while the "Fly" control is active in the input system
        if(flyValue.isPressed)
        {
            movementFly = lift;
        }
        else
        {
            movementFly = 0.0f;
        }
        
    }

    void FixedUpdate()
    {
        //Combines flying movement and WASD movement, physics based and tuneable with the "speed" variable
        Vector3 movement = new Vector3(movementX, movementFly, movementY);
        rb.AddRelativeForce(movement * speed);
    }
    
}
