using Godot;

public partial class CameraController : Camera3D
{
    [Export] public float MoveSpeed = 15.0f;
    [Export] public float ZoomSpeed = 2.0f;
    [Export] public float MinZoom = 5.0f;
    [Export] public float MaxZoom = 50.0f;

    private float _currentZoom = 20.0f;

    public override void _Process(double delta)
    {
        // 1. Handle Movement (Uses default UI actions: Arrow keys or WASD)
        Vector2 input = Input.GetVector("Left", "Right", "Up", "Down");
        
        // We move on X and Z (ignoring Y) for a top-down view
        Vector3 moveDir = new Vector3(input.X, 0, input.Y);
        GlobalPosition += moveDir * MoveSpeed * (float)delta;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // 2. Handle Mouse Wheel Zoom
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
                _currentZoom -= ZoomSpeed;
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
                _currentZoom += ZoomSpeed;

            // Clamp the zoom so it doesn't go too close or too far
            _currentZoom = Mathf.Clamp(_currentZoom, MinZoom, MaxZoom);

            // Update the Camera's Y position based on the zoom
            // (Assuming Camera is rotated -90 degrees on X to look down)
            Vector3 pos = GlobalPosition;
            pos.Y = _currentZoom;
            GlobalPosition = pos;
        }
    }
}