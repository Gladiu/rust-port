using UnityEngine;
using System;



[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    private float speedCap = 0.05f;
    private float jumpForce = 0.075f;
    private float groundFriction = 50f;
    private float airFriction = 0.05f;
    private float airTime = 0f; // Time character has spent in air in ms
    private Vector3 inertia = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private bool jumped = false;
    private Vector3 gravity = new Vector3(0, -0.3f, 0);
    private CharacterController controller = null;

    void Start() {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Define forward,right and up vectors relative to world
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 up = transform.TransformDirection(Vector3.up);

        // Handling movement from keyboard
        // Check if any movement happened at all
        Vector3 wishDir = Vector3.zero;
        
        // Defining wished directory vector in view coordinates
        transform.Rotate(up,Input.GetAxis("Mouse X"));
        wishDir = forward*Input.GetAxis("Vertical") + right * Input.GetAxis("Horizontal");
        wishDir.y = (float)Convert.ToDouble(Input.GetButtonDown("Jump"));


        // Saving original direction of velocity to keep track when to stop applying friction, if dot product of original velocity and current velocity
	    // is negative it means it has opposite direction and we can stop applying friction
	    if (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0 || Input.GetButtonDown("Jump") ){
            inertia = velocity;
        }
		
        // 0.7 is magic number which makes it feel good
        Vector3 next_velocity = wishDir+velocity*0.7f - inertia.normalized *
                                ( 
                                groundFriction * (float)Convert.ToDouble(controller.isGrounded)
                                + airFriction * (float)Convert.ToDouble(controller.isGrounded)
                                );
        // Checking if we can stop applying friction
	    velocity = (Vector3.Dot(inertia,next_velocity) >= 0) ? (next_velocity ) : Vector3.zero;
        // Cap speed
	    velocity = Vector3.ClampMagnitude(velocity, speedCap);

        // Check if we are eligible to jump and if we are adjust velocity
        if (controller.isGrounded){
            airTime = 0;
            jumped = false;
            velocity.y = 0;
        }
        else{
            airTime += Time.deltaTime;
            velocity.y = (float)Convert.ToDouble(jumped)*jumpForce + airTime * gravity.y;
        }

        // Jump if we havnt jumped
        if (Input.GetButtonDown("Jump") && !jumped){
            airTime = 0;
            jumped = true;
            velocity.y = wishDir.y * jumpForce;
        }
        
        // Apply final movement
        controller.Move(velocity);
    }
 
}