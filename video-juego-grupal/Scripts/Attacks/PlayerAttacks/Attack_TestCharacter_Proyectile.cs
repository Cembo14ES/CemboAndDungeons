using Godot;
using System;

public partial class Attack_TestCharacter_Proyectile : BaseAttack
{
	[Export] public float distanceFromPlayer { get; set; } = 2f; //La distancia del centro del jugador a la que spawnea el ataque.

    public override void _Ready()
    {
        base._Ready();
        if (stats.projectileSpeed != 0)
        {
            TopLevel = true;
        }
    }
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

	public override void _PhysicsProcess(double delta)
	{
		if (stats.AttackAlwaysActive)
        {
            if (!IsInstanceValid(owner)) //Si el padre desaparece, el ataque permanente tambien
            {
                QueueFree();
                return;
            }
        }

        foreach (Area3D area in hitboxArea.GetOverlappingAreas())
        {

            //if (area3Dcoll.GetParent() is BaseAttack) continue;   // Ignora otros ataques.
            if (area.GetParent() is not BaseCharacter target) continue; // Ignora todo lo que no sea personajes.
            if (target == owner) continue; // Ignora el personaje que creo este ataque.
            if (!target.canBeDamaged) continue; //Ignora a los personajes que estan en un estado de invulnerabilidad.
            if (!stats.AttackAlwaysActive)
                if (!alreadyHit.Add(target)) continue; //Ignora los personajes que el ataque ya haya atacado.

            target.TakeDamage(stats.FinalDamage);
            QueueFree();
        }

		float rotation = attackDirection.Y;

		Vector2 velocity = Vector2.FromAngle(rotation - Mathf.DegToRad(90));
        Velocity = new Vector3(velocity.X, 0, velocity.Y) * stats.projectileSpeed;
        MoveAndSlide();
	}
}
