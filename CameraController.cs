using Godot;
using System;

public class CameraController : Position3D
{
    Main MainObj;
    Boolean RightDragging;
    Boolean MiddleDragging;

    // Camera Settings
    
    // Min: 1, Max: 20
    [Export(PropertyHint.Range, "1,20,1")] float ZoomSpeed = 10;

    // Min: 1, Max 10, Snap to nearest Integer
    [Export(PropertyHint.Range, "1,20,1")] float RotationSpeed = 10;
    [Export(PropertyHint.Range, "1,20,1")] float MovementSpeed = 10;
    [Export] Boolean InvertedY = false;
    [Export] Boolean InvertedX = false;
    [Export] Boolean AllowZoom = true;
    [Export] Boolean AllowRotation = true;
    [Export] Boolean AllowMovement = true;
    

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // not used for now
        MainObj = (Main)GetParent().GetParent();
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      // dont think we need anything in _Process but might as well leave it commented out
    //      // not paying github per line lol
    //  }

    public override void _UnhandledInput(InputEvent @event) // weird architecture
    {
        //base._UnhandledInput(@event);
        if (@event is InputEventMouseButton mouseEvent)
        {
            if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.Right)
            {
                // Start dragging if right click pressed
                if (!RightDragging && mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    RightDragging = true;
                }
                // stop dragging if right click released
                if (RightDragging && !mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    RightDragging = false;
                }
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.Middle)
            {
                // Start dragging if middle click pressed
                if (!MiddleDragging && mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    MiddleDragging = true;
                }
                // stop dragging if middle click released
                if (MiddleDragging && !mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    MiddleDragging = false;
                }
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelUp && AllowZoom)
            {
                ClippedCamera cam = GetNode<ClippedCamera>("CameraPivot/ClippedCamera");
                Vector3 direction = cam.Translation;
                direction.z -= ZoomSpeed;
                cam.Translation = direction;
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelDown && AllowZoom)
            {
                ClippedCamera cam = GetNode<ClippedCamera>("CameraPivot/ClippedCamera");
                Vector3 direction = cam.Translation;
                direction.z += ZoomSpeed;
                cam.Translation = direction;
            }
        }
        else if (@event is InputEventMouseMotion motionEvent)
        {
            if (MiddleDragging && AllowRotation)
            {
                Vector2 relative = -motionEvent.Relative; // Make controls normal and then check if inverted
                if (InvertedX) relative.x *= -1;
                if (InvertedY) relative.y *= -1;

                Position3D camPivot = GetNode<Position3D>("CameraPivot");

                RotateY((RotationSpeed * relative.x) / (10000));
                camPivot.RotateX((RotationSpeed * relative.y) / (10000));
            }
            else if (RightDragging && AllowMovement)
            {
                Vector2 relative = -motionEvent.Relative; // invert the controls so they don't feel weird
                Vector3 movement = new Vector3((MovementSpeed * relative.x) / 40, 0, (MovementSpeed * relative.y) / 40);
                TranslateObjectLocal(movement);
            }
        }
    }
}
