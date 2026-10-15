using Godot;
using System;

public partial class CharacterEnemy : BaseCharacter
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}
	protected override Vector3 GetMoveDirection()
	{
		//Codigo
		return Vector3.Zero;
	}
	protected override void OnCollided(Node collider)
	{
		base.OnCollided(collider);
	}
}
