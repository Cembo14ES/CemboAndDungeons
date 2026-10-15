using Godot;
using System;

public partial class CharacterSon : BaseCharacter
{
	[Export] public int Arrows;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		
		Arrows = 10;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}
	protected override Vector3 GetMoveDirection()
	{
		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		if (input != Vector2.Zero)
		{
			GD.Print("Me estoy moviendo");
		}
		return (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
	}
}
