using Godot;
using System;
using System.Collections.Generic;

public partial class Character_Melee : BasePlayerCharacter
{
	private const int ComboLength = 4; // Nº de golpes del combo; después vuelve al 1º.

	[Export] public float ComboResetTime { get; set; } = 1.6f;       // Segundos sin pulsar para reiniciar el combo.
	[Export] public float UltimateAttackDuration { get; set; } = 10f; // Segundos que dura la ulti.
	private Timer comboResetTimer = new Timer(); // Se reinicia en cada click; si termina, el combo vuelve a 0.
	private Timer ultimateAttackTimer = new Timer(); // Timer para el cooldown de la ulti.
	[Export] public int attackIndex = 0;                 // Golpe actual del combo (0 = 1º, 1 = 2º, 2 = 3º, 3 = 4º).
	private Timer dashTimer = new Timer();          // duración del dash
	private Timer dashCooldownTimer = new Timer();  // espera entre dashes
	private bool dashCooldownEmpty = true;
	private Vector3 dashDirection = Vector3.Zero;
	private Vector3 lastMoveDirection = Vector3.Zero;
	private CurrentAttackState currentAttackState = 0;
	protected new enum CurrentAttackState { None = 0, NormalAttack = 1, SkillAttack = 2, movementSkill = 3, UltimateAttack = 4}

	private float CurrentDamage => stats.Damage * (ultimateAttackTimer.IsStopped() ? 1f : 1.5f);

	public override void _Ready()
	{
		base._Ready();

		InitializeTimer(comboResetTimer, ComboResetTime, true, () => attackIndex = 0);
		
		InitializeTimer(ultimateAttackTimer, UltimateAttackDuration, true, onUltimateAttackTimeout);

		InitializeTimer(dashTimer, 0.2f, true, onDashTimeout);

		InitializeTimer(dashCooldownTimer, 4f, true, () => dashCooldownEmpty = true);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		Vector3 input = base.GetMovementDirection();
		if (input != Vector3.Zero)
		{
			lastMoveDirection = input.Normalized();
		}

		float aim = CalculateAimDirection();
		pointerArrow.Rotation = new Vector3(0, 0, aim - Mathf.DegToRad(90));

		if (regularAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_RegularAttack))
		{
			RegularAttack(this, aim, Position);
		}
		if (skillAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_SkillAttack))
		{
			SkillAttack(this, aim, Position);
		}
		if (ultimateAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_UltimateAttack))
		{
			UltimateAttack(this, aim, Position);
		}
		if (dashCooldownEmpty && currentAttackState != CurrentAttackState.movementSkill && Input.IsActionJustPressed(InputActions.character_MovementSkill))
		{
			StartDash();
		}
		GD.Print($"CurrentAttackState: {currentAttackState}, attackIndex: {attackIndex}, dashCooldownEmpty: {dashCooldownEmpty}");
	}
	protected override Vector3 GetMovementDirection()
	{
		if (currentAttackState == CurrentAttackState.movementSkill)
		{
			return dashDirection * 4f;   // dirección fija, ignora el input
		}
		if (currentAttackState == CurrentAttackState.NormalAttack)
		{
			return Vector3.Zero; // No se puede mover mientras se está atacando.
		}
		return base.GetMovementDirection();
	}

	/// <summary>
	/// Ataque normal en combo: el 3er y 4º golpe hacen más daño.
	/// </summary>
	protected override void RegularAttack(BaseCharacter owner, float direction, Vector3 position)
	{
		if(currentAttackState == CurrentAttackState.SkillAttack)
		{
			GD.Print($"{Name} no puede usar Basic Attack mientras Skill Attack está activo.");
			return;
		}
		Attack_Melee_Basic attack = RegularAttackScene.Instantiate<Attack_Melee_Basic>();	
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
		attack.Signal_ReturnToNormalityBasic += onBasicAttackTimeout; // restaura la velocidad del jugador cuando termina el ataque
		AddChild(attack);

		regularAttackCooldownEmpty = false;
		regularAttackCooldownTimer.Start();

		attackIndex = (attackIndex + 1) % ComboLength;
		comboResetTimer.Start();
		currentAttackState = CurrentAttackState.NormalAttack;
	}
	protected override void SkillAttack(BaseCharacter owner, float direction, Vector3 position)
	{
		if(currentAttackState == CurrentAttackState.NormalAttack)
		{
			GD.Print($"{Name} no puede usar Skill Attack mientras Basic Attack está activo.");
			return;
		}
		if (SkillAttackScene == null)
		{
			GD.PushWarning($"{Name}: no tiene Skill Attack Scene asignada.");
			return;
		}
		stats.MovementSpeed = stats.MovementSpeed * 0.5f; // reduce la velocidad del jugador mientras ataca

		Attack_Melee_Skill skill = SkillAttackScene.Instantiate<Attack_Melee_Skill>();
		skill.Setup(owner, new List<BaseCharacter>(), direction, position, CurrentDamage);
		skill.Signal_ReturnToNormality += onSkillAttackTimeout; // restaura la velocidad del jugador cuando termina el ataque
		AddChild(skill);

		skillAttackCooldownEmpty = false;
		skillAttackCooldownTimer.Start();
		currentAttackState = CurrentAttackState.SkillAttack;
	}
    protected override void UltimateAttack(BaseCharacter owner, float directionAngle, Vector3 position)
    {
		GD.Print($"{Name} activa su Ultimate Attack.");
		ultimateAttackCooldownEmpty = false;    // cooldown propio de la ulti
		ultimateAttackTimer.Start();
		currentAttackState = CurrentAttackState.UltimateAttack;
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
	private void StartDash()
	{
		// Dirección: la tecla que se está pulsando; si estás quieto, la última dirección en la que te moviste.
		if (currentAttackState == CurrentAttackState.NormalAttack || currentAttackState == CurrentAttackState.SkillAttack)
		{
			GD.Print($"{Name} no puede hacer dash mientras está atacando.");
			return;
		}
		Vector3 dir = base.GetMovementDirection();
		if (dir == Vector3.Zero)
			dir = lastMoveDirection;

		if (dir == Vector3.Zero)
			return; // Nunca se ha movido: no hay dirección, no hace dash.

		SetInvulnerable(true);
		dashDirection = dir.Normalized();

		currentAttackState = CurrentAttackState.movementSkill;
		dashCooldownEmpty = false;
		dashTimer.Start();
	}

	private void onDashTimeout()
	{
		RestoreDefaultAttackState();
		SetInvulnerable(false);
		dashCooldownTimer.Start();
	}
	private void onSkillAttackTimeout()
	{
		stats.MovementSpeed *= 2f;
		RestoreDefaultAttackState();
	}
	private void onUltimateAttackTimeout()
	{
		ultimateAttackCooldownTimer.Start();
		if(currentAttackState == CurrentAttackState.UltimateAttack)
		{
			currentAttackState = CurrentAttackState.None;
		}
	}
	private void onBasicAttackTimeout()
	{
		RestoreDefaultAttackState();
	}
	private void RestoreDefaultAttackState()
	{
		if (ultimateAttackTimer.IsStopped())
		{
			currentAttackState = CurrentAttackState.None;
		}
		else
		{
			currentAttackState = CurrentAttackState.UltimateAttack;
		}
	}
	protected void SetInvulnerable(bool value) => canBeDamaged = value;

}