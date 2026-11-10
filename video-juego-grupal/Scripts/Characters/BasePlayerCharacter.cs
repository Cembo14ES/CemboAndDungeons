using Godot;

public partial class BasePlayerCharacter : BaseCharacter
{
	[Export] public PackedScene RegularAttackScene { get; set; } // La Escena (.tscn) del ataque Normal.
	[Export] public PackedScene SkillAttackScene { get; set; } // La Escena (.tscn) de la habilidad de ataque.
	[Export] public PackedScene UltimateAttackScene { get; set; } // La Escena (.tscn) de la Ulti.

	public bool regularAttackCooldownEmpty { get; protected set; } = true; // Booleano que determina si el personaje puede atacar.
	public bool skillAttackCooldownEmpty { get; protected set; } = true; // Booleano que determina si el personaje puede usar la habilidad de ataque.
	public bool ultimateAttackCooldownEmpty { get; protected set; } = true; // Booleano que determina si el personaje puede usar la ulti.

	protected Timer regularAttackCooldownTimer = new Timer(); //Timer para el cooldown del ataque.
	protected Timer skillAttackCooldownTimer = new Timer(); //Timer para el cooldown del ataque.
	protected Timer ultimateAttackCooldownTimer = new Timer(); //Timer para el cooldown del ataque.

	public override void _Ready()
	{
		base._Ready();

		// Si el base falló (sin Stats) no seguimos.
		if (stats == null) return;

		if (RegularAttackScene == null)
		{
			GD.PushError($"{Name}: No tiene Regular Attack asignado.");
			SetPhysicsProcess(false);
			return;
		}

		AddChild(regularAttackCooldownTimer);
		regularAttackCooldownTimer.WaitTime = stats.RegularAttackCooldown;
		regularAttackCooldownTimer.OneShot = true;
		regularAttackCooldownTimer.Timeout += () => regularAttackCooldownEmpty = true;

		AddChild(skillAttackCooldownTimer);
		skillAttackCooldownTimer.WaitTime = stats.SkillAttackCooldown;
		skillAttackCooldownTimer.OneShot = true;
		skillAttackCooldownTimer.Timeout += () => skillAttackCooldownEmpty = true;

		AddChild(ultimateAttackCooldownTimer);
		ultimateAttackCooldownTimer.WaitTime = stats.UltimateAttackCooldown;
		ultimateAttackCooldownTimer.OneShot = true;
		ultimateAttackCooldownTimer.Timeout += () => ultimateAttackCooldownEmpty = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		
	}

	protected override Vector3 GetMovementDirection()
	{
		Vector2 input = Input.GetVector(InputActions.character_Right, InputActions.character_Left, InputActions.character_Down, InputActions.character_Up);
		return (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
	}

	/// <summary>
	/// Realiza un ataque normal. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected virtual void RegularAttack(BaseCharacter owner, Vector3 direction, Vector3 position)
	{	
		BaseAttack attack = RegularAttackScene.Instantiate<BaseAttack>();
		attack.Setup(owner, direction, position);
		AddChild(attack);

		regularAttackCooldownEmpty = false;
		regularAttackCooldownTimer.Start();
	}

	/// <summary>
	/// Realiza una ataque habilidad de ataque. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected virtual void SkillAttack(BaseCharacter owner, Vector3 direction, Vector3 position)
	{
		BaseAttack skillAttack = SkillAttackScene.Instantiate<BaseAttack>();
		skillAttack.Setup(owner, direction, position);
		AddChild(skillAttack);

		skillAttackCooldownEmpty = false;
		skillAttackCooldownTimer.Start();
	}

	/// <summary>
	/// Realiza una ulti. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected virtual void UltimateAttack()
	{
		BaseAttack ultimateAttack = UltimateAttackScene.Instantiate<BaseAttack>();
		ultimateAttack.Setup(this, characterFacingDirection, Position);
		AddChild(ultimateAttack);

		ultimateAttackCooldownEmpty = false;
		ultimateAttackCooldownTimer.Start();
	}

	protected override void OnCollided(Node collider)
	{
		base.OnCollided(collider);
	}
}