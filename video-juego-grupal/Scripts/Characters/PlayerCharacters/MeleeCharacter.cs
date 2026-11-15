using Godot;
using System;
using System.Collections.Generic;

public partial class MeleeCharacter : BasePlayerCharacter
{
	private const int ComboLength = 4; // Nº de golpes del combo; después vuelve al 1º.

	[Export] public float ComboResetTime { get; set; } = 1.6f;       // Segundos sin pulsar para reiniciar el combo.
	[Export] public float UltimateAttackDuration { get; set; } = 10f; // Segundos que dura la ulti.
	private Timer comboResetTimer = new Timer(); // Se reinicia en cada click; si termina, el combo vuelve a 0.
	private Timer ultimateAttackTimer = new Timer(); // Timer para el cooldown de la ulti.
	[Export] public int attackIndex = 0;                 // Golpe actual del combo (0 = 1º, 1 = 2º, 2 = 3º, 3 = 4º).
	[Export] public Camera3D camera;
	[Export] public Sprite3D pointerArrow;
	private float CurrentDamage => stats.Damage * (ultimateAttackTimer.IsStopped() ? 1f : 1.5f);

	public override void _Ready()
	{
		base._Ready();

		// Si el base falló (sin Stats o sin ataque normal) no seguimos.
		if (stats == null || RegularAttackScene == null) return;

		AddChild(comboResetTimer);
		comboResetTimer.WaitTime = ComboResetTime;
		comboResetTimer.OneShot = true;
		comboResetTimer.Timeout += () => attackIndex = 0;
		AddChild(ultimateAttackTimer);
		ultimateAttackTimer.WaitTime = UltimateAttackDuration;
		ultimateAttackTimer.OneShot = true;
		ultimateAttackTimer.Timeout += () => ultimateAttackCooldownTimer.Start();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		float aim = CalculateAimDirection();
		pointerArrow.Rotation = new Vector3(0, 0, aim - Mathf.DegToRad(90));

		if (regularAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_RegularAttack))
			RegularAttack(this, aim, Position);
		if (skillAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_SkillAttack))
			SkillAttack(this, aim, Position);
		if (ultimateAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_UltimateAttack))
			UltimateAttack(this, aim, Position);
	}

	/// <summary>
	/// Ataque normal en combo: el 3er y 4º golpe hacen más daño.
	/// </summary>
	protected override void RegularAttack(BaseCharacter owner, float direction, Vector3 position)
	{
		BaseAttack attack = RegularAttackScene.Instantiate<BaseAttack>();	
		attack.Setup(owner, new List<BaseCharacter>(), direction, position, CurrentDamage);
		
		if (attackIndex == 2)   // 3er y 4º golpe (índices 2 y 3)
		{
			attack.stats.AttackMultiplier *= 1.2f;
		}
		else if (attackIndex == 3)
		{
			attack.stats.AttackMultiplier *= 1.5f;
		}
		else
		{
			attack.stats.AttackMultiplier *= 1f;
		}

		AddChild(attack);

		regularAttackCooldownEmpty = false;
		regularAttackCooldownTimer.Start();

		attackIndex = (attackIndex + 1) % ComboLength;
		comboResetTimer.Start();
	}
	protected override void SkillAttack(BaseCharacter owner, float direction, Vector3 position)
	{
		if (SkillAttackScene == null)
		{
			GD.PushWarning($"{Name}: no tiene Skill Attack Scene asignada.");
			return;
		}

		BaseAttack skill = SkillAttackScene.Instantiate<BaseAttack>();
		skill.Setup(owner, new List<BaseCharacter>(), direction, position, CurrentDamage);
		AddChild(skill);

		skillAttackCooldownEmpty = false;
		skillAttackCooldownTimer.Start();
	}
    protected override void UltimateAttack(BaseCharacter owner, float directionAngle, Vector3 position)
    {
		GD.Print($"{Name} activa su Ultimate Attack.");
		ultimateAttackCooldownEmpty = false;    // cooldown propio de la ulti
		ultimateAttackTimer.Start();
    }


	protected override void OnCollided(Node collider)
	{
		base.OnCollided(collider);
	}
	protected float CalculateAimDirection()
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