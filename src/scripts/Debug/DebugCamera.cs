using Godot;
using System;

public partial class DebugCamera : Camera3D
{
    [Export] public float MoveSpeed { get; set; } = 10.0f;
    [Export] public float LookSensitivity { get; set; } = 0.003f;
    [Export] public float FovChangeSpeed { get; set; } = 3.0f;
    [Export] public float MinFov { get; set; } = 20.0f;
    [Export] public float MaxFov { get; set; } = 120.0f;

    private float _pitch = 0.0f;
    private float _yaw = 0.0f;

    public override void _Ready()
    {
        // Set initial rotation based on the camera's current transform in the editor
        Vector3 currentRotation = Rotation;
        _pitch = currentRotation.X;
        _yaw = currentRotation.Y;

        DebugMaster.Instance.SignalDebug_debugCameraToogle += ToogleCamera;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (DebugMaster.Instance.debugCameraEnabled)
        {
            // 1. Handle Mouse Look
            if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                _yaw -= mouseMotion.Relative.X * LookSensitivity;
                _pitch -= mouseMotion.Relative.Y * LookSensitivity;

                // Clamp pitch to prevent the camera from flipping upside down
                _pitch = Mathf.Clamp(_pitch, -Mathf.Pi / 2, Mathf.Pi / 2);

                Rotation = new Vector3(_pitch, _yaw, 0);
            }

            // 2. Handle Mouse Scrollwheel (FOV)
            if (@event is InputEventMouseButton mouseButton)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    Fov = Mathf.Clamp(Fov - FovChangeSpeed, MinFov, MaxFov);
                }
                else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    Fov = Mathf.Clamp(Fov + FovChangeSpeed, MinFov, MaxFov);
                }

                // Click left mouse button to re-capture the mouse if it was freed
                if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                }
            }

            // 3. Press 'Escape' to free the mouse cursor so you can close the game window
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (DebugMaster.Instance.debugCameraEnabled)
        {
            Vector3 inputDir = Vector3.Zero;

            // Forward / Backward (Local Z axis)
            if (Input.IsKeyPressed(Key.W)) inputDir -= Transform.Basis.Z;
            if (Input.IsKeyPressed(Key.S)) inputDir += Transform.Basis.Z;

            // Left / Right (Local X axis)
            if (Input.IsKeyPressed(Key.A)) inputDir -= Transform.Basis.X;
            if (Input.IsKeyPressed(Key.D)) inputDir += Transform.Basis.X;

            // Up / Down (Global Y axis)
            if (Input.IsKeyPressed(Key.Ctrl)) inputDir += Vector3.Up;
            if (Input.IsKeyPressed(Key.Shift)) inputDir += Vector3.Down;

            // Normalize to prevent faster diagonal movement
            if (inputDir != Vector3.Zero)
            {
                inputDir = inputDir.Normalized();
            }

            // Apply movement
            Position += inputDir * MoveSpeed * (float)delta;
        }

    }

    public void ToogleCamera()
    {


        if (DebugMaster.Instance.debugCameraEnabled)
        {
            this.Current = true;
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else
        {
            this.Current = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

    }
}
