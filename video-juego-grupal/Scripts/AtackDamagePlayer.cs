using Godot;

public partial class AtackDamagePlayer : BaseAttackDamage
{
    [Export] public float Distance { get; set; } = 2f;
    protected override void AimAt(Vector3 direction) //Hay que ajustarlas mejor, pero por el momento lo dejo así
    {
        direction.Y = 0;
        if (direction == Vector3.Zero) return;

        float step = Mathf.Pi / 4f;
        float angle = Mathf.Round(Mathf.Atan2(direction.X, direction.Z) / step) * step;

        Position = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 2;

        Rotation = new Vector3(0, angle, 0);
    }
}

