using Godot;
using System;

public partial class Weapon : Node3D
{
    private Camera3D camera;
    private Node3D player;
    private Sprite3D weaponSprite;

    // ==========================
    // PIVOTE MANUAL
    // ==========================
    // Este NO mueve el arma.
    // Solo cambia el punto desde donde se calcula la rotación.
    private Vector3 pivotOffset = new Vector3(0, 0, 0);

    public override void _Ready()
    {
        player = GetParent<Node3D>();
        camera = player.GetNode<Camera3D>("Camera3D");

        weaponSprite = GetNode<Sprite3D>("Sprite3D");
    }

    public override void _Process(double delta)
    {
        Vector2 mousePosition = GetViewport().GetMousePosition();

        Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
        Vector3 rayDirection = camera.ProjectRayNormal(mousePosition);

        Plane plane = new Plane(
            Vector3.Up,
            player.GlobalPosition.Y
        );

        Vector3? intersection = plane.IntersectsRay(
            rayOrigin,
            rayDirection
        );

        if (!intersection.HasValue)
            return;

        Vector3 target = intersection.Value;

        // ==========================
        // PUNTO DE PIVOTE MANUAL
        // ==========================
        Vector3 pivotPosition =
            GlobalPosition +
            (GlobalTransform.Basis * pivotOffset);

        // Dirección desde el pivote hacia el ratón
        Vector3 direction = target - pivotPosition;

        direction.Y = 0;

        if (direction.LengthSquared() < 0.001f)
            return;

        direction = direction.Normalized();

        // ==========================
        // CALCULAR ROTACIÓN
        // ==========================
        float angle = Mathf.Atan2(direction.X, direction.Z);

        Rotation = new Vector3(
            0,
            angle + Mathf.DegToRad(-90),
            0
        );
    }
}