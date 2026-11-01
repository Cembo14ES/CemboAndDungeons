using Godot;
using System;

public partial class CharacterEnemy : BaseCharacter
{
	private Node3D Target { get; set; } //El objetivo que el Slime persigue.
	public override void _Ready()
	{
		base._Ready();
		Target = GetTree().GetFirstNodeInGroup("player") as Node3D;

		BaseAttack attack = RegularAttackScene.Instantiate<BaseAttack>();
        attack.Setup(this, Vector3.Zero, Vector3.Zero);
        AddChild(attack);    // hijo del personaje; muere con él
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}
    protected override Vector3 GetMovementDirection()
    {
        if (!IsInstanceValid(Target))
		{
			Target = null;
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
	protected override void RegularAttack()
	{
		base.RegularAttack();
	}
}
