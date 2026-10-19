using Godot;
using System;

public partial class CharacterEnemy : BaseCharacter
{
	// Called when the node enters the scene tree for the first time.
	[Export] public Node3D Target { get; set; }
	public override void _Ready()
	{
		base._Ready();
		Target = GetTree().GetFirstNodeInGroup("player") as Node3D;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}
    protected override Vector3 GetMoveDirection()
    {
        if (Target == null)
		{ 
			return Vector3.Zero;
		}
        Vector3 toTarget = Target.GlobalPosition - GlobalPosition;
        toTarget.Y = 0;
        return toTarget.Normalized();
    }
	protected override void OnCollided(Node collider)
	{
		base.OnCollided(collider);
	}
}
