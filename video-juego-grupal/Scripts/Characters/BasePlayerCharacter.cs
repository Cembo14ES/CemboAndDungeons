using System.Collections.Generic;
using Godot;

public partial class BasePlayerCharacter : BaseCharacter
{
	[ExportGroup ("Misc")]
	[Export] public Camera3D camera;
	[Export] public Sprite3D pointerArrow;
	protected enum CurrentAttackState { None = 0, NormalAttack = 1, SkillAttack = 2, movementSkill = 3, UltimateAttack = 4}

	
	[ExportGroup ("Attack Scenes")]
	[Export] public PackedScene RegularAttackScene { get; set; } // La Escena (.tscn) del ataque Normal.
	[Export] public PackedScene SkillAttackScene { get; set; } // La Escena (.tscn) de la habilidad de ataque.
	[Export] public PackedScene MovementSkillScene { get; set; } // La Escena (.tscn) de la habilidad de movimiento.
	[Export] public PackedScene UltimateAttackScene { get; set; } // La Escena (.tscn) de la Ulti.

	public bool regularAttackCooldownEmpty { get; protected set; } = true; // Booleano que determina si el personaje puede atacar.
	public bool skillAttackCooldownEmpty { get; protected set; } = true; // Booleano que determina si el personaje puede usar la habilidad de ataque.
	public bool movementSkillCooldownEmpty { get; protected set; } = true; // Booleano que determina si el personaje puede usar la habilidad de movimiento.
	public bool ultimateAttackCooldownEmpty { get; protected set; } = true; // Booleano que determina si el personaje puede usar la ulti.

	protected Timer regularAttackCooldownTimer = new Timer(); //Timer para el cooldown del ataque.
	protected Timer skillAttackCooldownTimer = new Timer(); //Timer para el cooldown de la habilidad de ataque.
	protected Timer movementSkillCooldownTimer = new Timer(); //Timer para el cooldown de la habilidad de movimiento.
	protected Timer ultimateAttackCooldownTimer = new Timer(); //Timer para el cooldown de la ulti.

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

		InitializeTimer(regularAttackCooldownTimer, stats.RegularAttackCooldown, true, () => regularAttackCooldownEmpty = true);

		InitializeTimer(skillAttackCooldownTimer, stats.SkillAttackCooldown, true, () => skillAttackCooldownEmpty = true);

		InitializeTimer(movementSkillCooldownTimer, stats.movementSkillCooldown, true, () => movementSkillCooldownEmpty = true);

		InitializeTimer(ultimateAttackCooldownTimer, stats.UltimateAttackCooldown, true, () => ultimateAttackCooldownEmpty = true);
	}

	protected override void CheckExportedValues()
	{
		base.CheckExportedValues();

		if (camera == null)
			GD.PushWarning($"{Name}: No tiene Camera3D asignado.");
		if (pointerArrow == null)
			GD.PushWarning($"{Name}: No tiene PointerArrow asignado.");
		if (RegularAttackScene == null)
			GD.PushWarning($"{Name}: No tiene Regular Attack asignado.");
		if (SkillAttackScene == null)
			GD.PushWarning($"{Name}: No tiene Skill Attack asignado.");
		if (MovementSkillScene == null)
			GD.PushWarning($"{Name}: No tiene Movement Skill asignado.");
		if (UltimateAttackScene == null)
			GD.PushWarning($"{Name}: No tiene Ultimate asignado.");
	}

	protected override Vector3 GetMovementDirection()
	{
		Vector2 input = Input.GetVector(InputActions.character_Right, InputActions.character_Left, InputActions.character_Down, InputActions.character_Up);
		return (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
	}

	/// <summary>
	/// Realiza un ataque normal. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected virtual void RegularAttack(BaseCharacter owner, float directionAngle, Vector3 position)
	{	
		BaseAttack attack = RegularAttackScene.Instantiate<BaseAttack>();
		attack.Setup(owner, new List<BaseCharacter>(), directionAngle, position, stats.Damage);
		AddChild(attack);

		regularAttackCooldownEmpty = false;
		regularAttackCooldownTimer.Start();
	}

	/// <summary>
	/// Realiza una ataque habilidad de ataque. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected virtual void SkillAttack(BaseCharacter owner, float directionAngle, Vector3 position)
	{
		BaseAttack skillAttack = SkillAttackScene.Instantiate<BaseAttack>();
		skillAttack.Setup(owner, new List<BaseCharacter>(), directionAngle, position, stats.Damage);
		AddChild(skillAttack);

		skillAttackCooldownEmpty = false;
		skillAttackCooldownTimer.Start();
	}

	/// <summary>
	/// Realiza una habilidad de movimiento. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected virtual void MovementSkill(BaseCharacter owner, float directionAngle, Vector3 position)
	{
		BaseAttack movementSkill = MovementSkillScene.Instantiate<BaseAttack>();
		movementSkill.Setup(owner, new List<BaseCharacter>(), directionAngle, position, stats.Damage);
		AddChild(movementSkill);

		movementSkillCooldownEmpty = false;
		movementSkillCooldownTimer.Start();
	}

	/// <summary>
	/// Realiza una ulti. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected virtual void UltimateAttack(BaseCharacter owner, float directionAngle, Vector3 position)
	{
		BaseAttack ultimateAttack = UltimateAttackScene.Instantiate<BaseAttack>();
		ultimateAttack.Setup(owner, new List<BaseCharacter>(), directionAngle, position, stats.Damage);
		AddChild(ultimateAttack);

		ultimateAttackCooldownEmpty = false;
		ultimateAttackCooldownTimer.Start();
	}

	/// <summary>
	/// Calcula la direcion que el mouse esta apuntando.
	/// </summary>
	protected float CalculateMouseAimDirection()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Plane plane = new Plane(Vector3.Up, GlobalPosition.Y);

		Vector3? intersection = plane.IntersectsRay(camera.ProjectRayOrigin(mousePos), camera.ProjectRayNormal(mousePos));

		if (!intersection.HasValue)
			return -1;

		Vector3 direction = (intersection.Value - GlobalPosition).Normalized();

		float angle = Mathf.Atan2(direction.X, -direction.Z);

		return angle;
	}
}