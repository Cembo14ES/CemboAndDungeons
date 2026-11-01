using Godot;
using System;

public partial class CharacterPlayer : BaseCharacter
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
	}
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (canRegularAttack && Input.IsActionJustPressed(InputActions.character_RegularAttack))
            RegularAttack();
	}
	protected override Vector3 GetMovementDirection()
	{
		Vector2 input = Input.GetVector(InputActions.character_Right, InputActions.character_Left, InputActions.character_Down, InputActions.character_Up);
		return (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
	}
	protected override void RegularAttack()
	{
		base.RegularAttack();
	}
	protected override void OnCollided(Node collider)
	{
		base.OnCollided(collider);
	}
	
}
    