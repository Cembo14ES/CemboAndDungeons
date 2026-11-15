using Godot;
using System;

public partial class Attack_Ranger_MovementSkill : BaseAttack
{
	[Signal] public delegate void Signal_ResolveClingStateEventHandler(bool clinged);
	[Export] public float distanceFromPlayer { get; set; } = 2f; //La distancia del centro del jugador a la que spawnea el ataque.
	[Export] public float stats_MaxDistance = 10;
	public bool isClinged = false;

	public override void _Ready()
	{
		base._Ready();
		if (stats.projectileSpeed != 0)
		{
			TopLevel = true;
		}

		hitboxArea.Monitorable = true;
	}

	/// <summary>
	/// Realiza calculos para que el ataque spawnee adelante del jugador.
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
		if (Position.DistanceTo(attackPosition) >= stats_MaxDistance)
		{
			GD.Print($"[Movement Skill]: Attack has reached maximun distnace of {stats_MaxDistance}.");
			EmitSignal(SignalName.Signal_ResolveClingState, isClinged);
			Die();
		}

		if (stats.AttackAlwaysActive)
		{
			if (!IsInstanceValid(owner)) //Si el padre desaparece, el ataque permanente tambien
			{
				QueueFree();
				return;
			}
		}
		if (!isClinged)
		{
			foreach (Node3D body in hitboxArea.GetOverlappingBodies())
			{
				if (body.GetParent() is not StaticBody3D staticBody) continue; // Ignora todo lo que no sea collisiones.
				if (staticBody.CollisionLayer == 1) //Solo interactua con collisiones tipo "World".
				{
					ClingToTarget();
				}
			}

			foreach (Area3D area in hitboxArea.GetOverlappingAreas())
			{
				if (area.GetParent() is BaseAttack) continue;   // Ignora otros ataques.
				if (area.GetParent() is not BaseCharacter target) continue; // Ignora todo lo que no sea personajes.
				if (target == owner) continue; // Ignora el personaje que creo este ataque.
				
				//if (attackTargets.Count == 0 || !attackTargets.Contains(target)) continue;
				
				if (target.damageTrigger != area) continue; //Ignora los Area3D que no sean la hitbox de ataques.
				if (!target.canBeDamaged) continue; //Ignora a los personajes que estan en un estado de invulnerabilidad.
				if (!stats.AttackAlwaysActive)
					if (!alreadyHit.Add(target)) continue; //Ignora los personajes que el ataque ya haya atacado.

				ClingToTarget();
				GD.Print("Target Clinged");
			}

			float rotation = attackDirectionAngle;

			Vector2 velocity = Vector2.FromAngle(rotation - Mathf.DegToRad(90));
			Velocity = new Vector3(velocity.X, 0, velocity.Y) * stats.projectileSpeed;
			MoveAndSlide();
		}
	}

	private void ClingToTarget()
	{
		isClinged = true;
		EmitSignal(SignalName.Signal_ResolveClingState, isClinged);
	}
}
