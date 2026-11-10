using Godot;

public partial class Attack_TestCharacter_Melee : BaseAttack
{
    [Export] public float distanceFromPlayer { get; set; } = 2f; //La distancia del centro del jugador a la que spawnea el ataque.

    /// <summary>
    /// Realiza calculos para que el ataque spawnee adelante del jugador, en las 8 direcciones disponibles.
    /// </summary>
    protected override void PlaceAttack(Vector3 attackDirection, Vector3 attackPosition)
    {
        if (attackDirection == Vector3.Zero) attackDirection = Vector3.Back;

        float angle = -(attackDirection.Y + Mathf.DegToRad(180));

        Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distanceFromPlayer;
        GlobalPosition = attackPosition + offset;
        Rotation = -attackDirection;
    }
}

