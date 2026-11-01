using Godot;

public partial class AttackPlayer : BaseAttack
{
    [Export] public float distanceFromPlayer { get; set; } = 2f; //La distancia del centro del jugador a la que spawnea el ataque.

    /// <summary>
    /// Realiza calculos para que el ataque spawnee adelante del jugador, en las 8 direcciones disponibles.
    /// </summary>
    protected override void PlaceAttack(Vector3 attackDirection, Vector3 attackPosition)
    {
        attackDirection.Y = 0;
        if (attackDirection == Vector3.Zero) attackDirection = Vector3.Back;

        float step = Mathf.Pi / 4f;   // 8 direcciones
        float angle = Mathf.Round(Mathf.Atan2(attackDirection.X, attackDirection.Z) / step) * step;

        Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distanceFromPlayer;
        GlobalPosition = attackPosition + offset;
        Rotation = new Vector3(0, angle, 0);
    }
}

