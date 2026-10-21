using Godot;
using System;

public partial class CharacterPlayer : BaseCharacter
{
	//[Export] public int Arrows;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		
		//Arrows = 10;
	}
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}
	protected override Vector3 GetMoveDirection()
	{
		Vector2 input = Input.GetVector("move_backward", "move_forward", "move_left", "move_right");
		return (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
	}
	protected override void Attack()
	{
		base.Attack();
	}
	protected override void OnCollided(Node collider)
	{
		base.OnCollided(collider);
	}
	protected override bool WantsToAttack()
	{
		return Input.IsActionJustPressed("attack"); //click
	}
}
    