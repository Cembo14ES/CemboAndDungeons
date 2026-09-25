using Godot;

public partial class CameraController : Camera3D
{
    [Export] public SimpleCharacter character;
    [Export] public float horizontalDistance;
    [Export] public float verticalDistance;
    [Export] public float rotationSpeed = 2.0f; // Speed of rotation in radians per second

    private float rotation = 0.0f; // Rotation angle in radians
    private float XDistance;
    private float ZDistance;

    public override void _Ready()
    {
        DebugMaster.Instance.SignalDebug_debugCameraToogle += ToogleCamera;
        UpdateCameraOffsets();
    }

    public override void _Process(double delta)
    {
        if (character == null) return;

        // Smoothly adjust rotation based on key holds
        if (Input.IsKeyPressed(Key.E))
        {
            rotation -= rotationSpeed * (float)delta;
        }
        if (Input.IsKeyPressed(Key.Q))
        {
            rotation += rotationSpeed * (float)delta;
        }

        // Keep rotation bounded within 0 to 2*PI radians
        rotation = Mathf.PosMod(rotation, Mathf.Tau);

        UpdateCameraOffsets();

        // Position camera around the player
        Position = new Vector3(
            character.Position.X + XDistance,
            character.Position.Y + verticalDistance,
            character.Position.Z + ZDistance
        );

        // Look directly at player position
        LookAt(character.GlobalPosition);
    }

    private void UpdateCameraOffsets()
    {
        XDistance = Mathf.Sin(rotation) * horizontalDistance;
        ZDistance = Mathf.Cos(rotation) * horizontalDistance;
    }

    public void ToogleCamera()
    {
        if (DebugMaster.Instance.debugCameraEnabled)     
            Current = false;     
        else      
            Current = true;      
    }
}
