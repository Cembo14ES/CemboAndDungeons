using Godot;

public partial class CameraController : Camera3D
{
    [Export] public float MoveSpeed = 15.0f;
    [Export] public float ZoomSpeed = 2.0f;
    [Export] public float MinZoom = 5.0f;
    [Export] public float MaxZoom = 50.0f;

    [Export] public Node3D character;

    private float _currentZoom = 20.0f;

    public override void _Process(double delta)
    {
        Position = new Vector3(character.Position.X, 25, character.Position.Z + 5);
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

    public void setPosition(Vector3 position)
    {
        position = new Vector3(position.X, _currentZoom ,position.Z);
        Position = position;
    }
}