using Godot;
using System;

public partial class Character_TestEnemy : BaseCharacter
{
	[Export] public PackedScene RegularAttackScene { get; set; } // La Escena (.tscn) del ataque del enemigo (hitbox de contacto).

	private Node3D Target { get; set; } //El objetivo que el Slime persigue.

	public override void _Ready()
	{
		base._Ready();

		// Si el base falló (sin Stats) no seguimos.
		if (stats == null) return;

		Target = GetTree().GetFirstNodeInGroup("player") as Node3D;

		if (RegularAttackScene == null)
		{
			GD.PushError($"{Name}: No tiene Regular Attack asignado.");
			return;
		}

		BaseAttack attack = RegularAttackScene.Instantiate<BaseAttack>();
		attack.Setup(this, Vector3.Zero, Vector3.Zero);
		AddChild(attack);    // hijo del personaje; muere con él
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
}