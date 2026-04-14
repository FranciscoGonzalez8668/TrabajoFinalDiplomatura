using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public bool InputEnabled { get; private set; } = true;
    public Vector2 MoveInput {get;private set;}

    public bool JumpPressed {get;private set;}
    
    public bool JumpHeld {get;private set;}

    public bool SlidePressed {get;private set;}

    public bool SprintHeld  {get; private set;}

    public bool ClimbPressed {get; private set;}

    void Update()
    {
        if (!InputEnabled)
        {
            ClearInputState();
            return;
        }

        MoveInput = new Vector2 ( 
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );


        JumpPressed = Input.GetButtonDown("Jump");
        JumpHeld = Input.GetButton("Jump");
        SprintHeld = Input.GetKey(KeyCode.LeftShift);
        ClimbPressed = Input.GetKeyDown(KeyCode.LeftShift);

        //SlidePressed = Input.GetButtonDown("Slide");
        SlidePressed = false;
    }

    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        if (!enabled)
        {
            ClearInputState();
        }
    }

    private void ClearInputState()
    {
        MoveInput = Vector2.zero;
        JumpPressed = false;
        JumpHeld = false;
        SlidePressed = false;
        SprintHeld = false;
        ClimbPressed = false;
    }

}
