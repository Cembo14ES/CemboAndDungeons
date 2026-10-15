using Godot;
using System;

public partial class BaseCharacter : CharacterBody3D
{
	// Called when the node enters the scene tree for the first time.
	[Export] public CharacterStats Stats { get; set; }
	[Export] public Timer InvulnerabilityTimer { get; set; }
	protected virtual Vector3 GetMoveDirection() => Vector3.Zero;
	public override void _Ready()
	{
		if (Stats == null)
        {
            GD.PushError($"{Name}: no tiene Stats asignado.");
            SetPhysicsProcess(false);
            return;
        }

        // Copia propia, para que los cambios no afecten al .tres ni a otros personajes
        Stats = (CharacterStats)Stats.Duplicate(true);
        GD.Print(Stats.Health, Stats.Speed, Stats.InvulnerabilityTimer);
        Stats.Health = Stats.MaxHealth;

        if (InvulnerabilityTimer == null) return;

        InvulnerabilityTimer.WaitTime = Stats.InvulnerabilityTimer;
        InvulnerabilityTimer.OneShot = true;
        InvulnerabilityTimer.Timeout += OnInvulnerabilityTimerTimeout;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{  
		Vector3 direction = GetMoveDirection();
        Vector3 velocity = Velocity;

        velocity.X = direction.X * Stats.Speed;
        velocity.Z = direction.Z * Stats.Speed;

        Velocity = velocity;
        MoveAndSlide();

		for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider() is Node collider)
                OnCollided(collider);
        }
	}
    protected virtual void OnCollided(Node collider)
    {
        if (collider is BaseCharacter other)
		{
			if (other.Stats.Attack > this.Stats.Defense)
			{
				GD.Print("Vida: " + this.Stats.Health + " Ataque: " + other.Stats.Attack + " Defensa: " + this.Stats.Defense);
				Stats.Health = this.Stats.Health - other.Stats.Attack + this.Stats.Defense;
				GD.Print("Daño Recibido: " + (other.Stats.Attack - this.Stats.Defense));
				GD.Print("Vida: " + this.Stats.Health);
                InvulnerabilityTimer.Start();
                Stats.CanBeDamaged = false;
			}
			//Lo que quede
		}
    }
	private void GetHurt()
    {
        //
    }
	private void OnInvulnerabilityTimerTimeout()
	{
		Stats.CanBeDamaged = true;
	}
}
