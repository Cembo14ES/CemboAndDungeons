using Godot;
using System;

public partial class Attack_Ranger_MovementSkill : BaseAttack
{
	[Signal] public delegate void Signal_ResolveClingStateEventHandler(bool clinged);
	[Export] public float distanceFromPlayer { get; set; } = 2f; //La distancia del centro del jugador a la que spawnea el ataque.
	[Export] public float stats_MaxDistance = 10;
	public bool isClinged = false;

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

			float rotation = attackDirectionAngle;

			Vector2 velocity = Vector2.FromAngle(rotation - Mathf.DegToRad(90));
			Velocity = new Vector3(velocity.X, 0, velocity.Y) * stats.projectileSpeed;
			MoveAndSlide();
		}
	}

	protected override void OnAreaEntered(Area3D area)
	{
		if (area.GetParent() is BaseCharacter target && target.damageTrigger == area && target != owner) //Comprueba que el objetivo sea la hitbox de otro personaje.
			if (attackTargets.Contains(target) || attackTargets.Count == 0)
			{ //Comprueba que el personaje se aun objetivo.
				target.TakeDamage(stats.FinalDamage, attackDirectionAngle);
				ClingToTarget();
			}
	}

	private void ClingToTarget()
	{
		isClinged = true;
		EmitSignal(SignalName.Signal_ResolveClingState, isClinged);
	}
}
