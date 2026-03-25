using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public Vector2 MoveInput {get;private set;}

    public bool JumpPressed {get;private set;}
    
    public bool JumpHeld {get;private set;}

    public bool SlidePressed {get;private set;}

    public bool WallRunHeld {get; private set;}




    void Update()
    {
        MoveInput = new Vector2 ( 
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );


        JumpPressed = Input.GetButtonDown("Jump");
        JumpHeld = Input.GetButton("Jump");
        WallRunHeld = Input.GetKey(KeyCode.LeftShift);

        //SlidePressed = Input.GetButtonDown("Slide");
        SlidePressed = false;
    }



}