using Godot;
using System;

public partial class BasePlayerCharacter : BaseCharacter
{
	[Export] public PackedScene RegularAttackScene { get; set; } // La Escena (.tscn) del ataque Normal.
	[Export] public PackedScene SpecialAttackScene { get; set; } // La Escena (.tscn) del ataque Especial.
	[Export] public PackedScene UltimateAttackScene { get; set; } // La Escena (.tscn) del ataque Ultimo.

	public bool canRegularAttack { get; private set; } = true; // Booleano que determina si el personaje puede atacar.

	private Timer regularAttackCooldownTimer = new Timer(); //Timer para el cooldown del ataque.

	public override void _Ready()
	{
		base._Ready();

		// Si el base falló (sin Stats) no seguimos.
		if (Stats == null) return;

		if (RegularAttackScene == null)
		{
			GD.PushError($"{Name}: No tiene Regular Attack asignado.");
			SetPhysicsProcess(false);
			return;
		}

		AddChild(regularAttackCooldownTimer);
		regularAttackCooldownTimer.WaitTime = Stats.RegularAttackCooldown;
		regularAttackCooldownTimer.OneShot = true;
		regularAttackCooldownTimer.Timeout += () => canRegularAttack = true;
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

	/// <summary>
	/// Realiza un ataque normal. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected virtual void RegularAttack()
	{
		BaseAttack attack = RegularAttackScene.Instantiate<BaseAttack>();
		attack.Setup(this, characterFacingDirection, Position);
		AddChild(attack);

		canRegularAttack = false;
		regularAttackCooldownTimer.Start();
	}
	protected virtual void SpecialAttack()
	{

	}
	protected virtual void UltimateAttack()
	{

	}

	protected override void OnCollided(Node collider)
	{
		base.OnCollided(collider);
	}
}