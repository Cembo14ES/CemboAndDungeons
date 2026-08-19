using Godot;
using System;

public partial class WeaponRotation : Sprite3D
{
    [Export] private Camera3D camera;
    [Export] private Node3D player;

    public bool IsLookingLeft { get; private set; }

    public override void _Process(double delta)
    {
        Vector2 mousePosition = GetViewport().GetMousePosition();

        Vector3 rayOrigin =
            camera.ProjectRayOrigin(mousePosition);

        Vector3 rayDirection =
            camera.ProjectRayNormal(mousePosition);

        Plane plane = new Plane(
            Vector3.Up,
            player.GlobalPosition.Y
        );

        Vector3? intersection =
            plane.IntersectsRay(
                rayOrigin,
                rayDirection
            );

        if (!intersection.HasValue)
            return;

        Vector3 target = intersection.Value;

        Vector3 direction =
            target - GlobalPosition;

        direction.Y = 0;

        if (direction.LengthSquared() < 0.001f)
            return;

        direction = direction.Normalized();

        // ==========================================
        // IZQUIERDA / DERECHA
        // ==========================================

        IsLookingLeft = direction.X < 0;

        // SOLO FLIP DEL SPRITE DEL ARMA
		FlipV = IsLookingLeft;

        // ==========================================
        // ROTACIÓN
        // ==========================================

        float angle =
            Mathf.Atan2(
                direction.X,
                direction.Z
            );

        Rotation = new Vector3(
            Mathf.DegToRad(-90),
            angle + Mathf.DegToRad(-90),
            0
        );
    }
}