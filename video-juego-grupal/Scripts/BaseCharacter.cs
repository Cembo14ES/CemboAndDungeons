using Godot;
using System;

public partial class BaseCharacter : CharacterBody3D
{
	// Called when the node enters the scene tree for the first time.
	[Export] public CharacterStats Stats { get; set; }
	[Export] public Timer InvulnerabilityTimer { get; set; }
    [Export] public bool CanBeDamaged { get; protected set; } = true;
	protected virtual Vector3 GetMoveDirection() => Vector3.Zero;

	public float Health { get; protected set; }
    protected virtual void OnCollided(Node collider) { }
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
        Health = Stats.Health;

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
	private void GetHurt()
    {
        //if (CanBeDamaged)
        //{
        //    Health--;
		//	InvulnerabilityTimer.Start();
//
        //    if (Health <= 0)
        //    {
        ///        QueueFree();
//
        //        SetPhysicsProcess(false);
//
        //        return;
        //    }
		//} 
        //else
		//{
		//	return;
		//}
		//CanBeDamaged = false;
//
        //GD.Print(
        //    $"[DEBUG] Vida: {Health}"
        //);
    }
	private void OnInvulnerabilityTimerTimeout()
	{
		CanBeDamaged = true;
	}
}
