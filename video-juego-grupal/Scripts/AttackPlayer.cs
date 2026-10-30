using Godot;

public partial class AttackPlayer : BaseAttack
{
    [Export] public float distance { get; set; } = 2f;
    protected override void AimAt(Vector3 aimDirection)
    {
        aimDirection.Y = 0;
        if (aimDirection == Vector3.Zero) aimDirection = Vector3.Back;

        float step = Mathf.Pi / 4f;   // 8 direcciones
        float angle = Mathf.Round(Mathf.Atan2(aimDirection.X, aimDirection.Z) / step) * step;

        Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distance;
        GlobalPosition = owner.GlobalPosition + offset;
        Rotation = new Vector3(0, angle, 0);
    }
}

