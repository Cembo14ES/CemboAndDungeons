using Godot;
using System;
using System.Collections.Generic;

public partial class Character_Ranger : BasePlayerCharacter
{
	
	[ExportGroup("Main Attack")]
	[Export] public float main_MaxChargeTime = 1;
	[Export] public float main_MaxChargeDamageMultiplier = 2;
	[Export] public float main_MaximunChargeForNoCharge = 10;
	[Export] public float main_MinimunChargeForMaxCharge = 10;
	private Timer main_ChargeTimer = new Timer();
	private float main_ChargeDamageAmount;

	[ExportGroup("Skill Attack")]
	[Export] public Area3D skill_RangeArea;
	[Export] public CollisionShape3D skill_RangeAreaShape;
	[Export] public float skill_AreaRadious = 8;
	[Export] public float skill_DelayBettewnArrows = 0.3f;
	private List<BaseCharacter> skill_AttackTargets = new List<BaseCharacter>();
	private Timer skill_AttackDelaysTimer = new Timer();

	[ExportGroup("Movement Skill")]
	[Export] public Area3D movement_GrappleEndArea;
	[Export] public CollisionShape3D movement_GrappleEndAreaShape;
	private Attack_Ranger_MovementSkill movement_CurrentInstance;
	private float movement_dragSpeed = 20;

	private CurrentAttackState currentAttackState = 0;
	protected new enum CurrentAttackState { None = 0, NormalAttack = 1, SkillAttack = 2, movementSkill = 3, UltimateAttack = 4, ChargingNormalAttack = 5, MovementSkillClinged = 6 }

	public override void _Ready()
	{
		base._Ready();

		SphereShape3D shape = (SphereShape3D) skill_RangeAreaShape.Shape;
		shape.Radius = skill_AreaRadious;	

		InitializeTimer(main_ChargeTimer, main_MaxChargeTime, true, () => main_ChargeDamageAmount = main_MaxChargeDamageMultiplier);

		InitializeTimer(skill_AttackDelaysTimer, skill_DelayBettewnArrows, false, SkillAttackTimeout);
	}
	public override void _PhysicsProcess(double delta)
	{
		if (currentAttackState != CurrentAttackState.SkillAttack && currentAttackState != CurrentAttackState.movementSkill && currentAttackState != CurrentAttackState.MovementSkillClinged)
		{
			Movement(GetMovementDirection(), stats.MovementSpeed);
		}

		else if (currentAttackState == CurrentAttackState.MovementSkillClinged)
		{
			Movement(Position.DirectionTo(movement_CurrentInstance.Position), movement_dragSpeed);

			foreach(Area3D overlapingArea in movement_GrappleEndArea.GetOverlappingAreas())
			{
				if(overlapingArea.GetParent() == movement_CurrentInstance && movement_CurrentInstance.isClinged == true)
				{
					
					movement_CurrentInstance.Die();
					movement_CurrentInstance = null;
					currentAttackState = CurrentAttackState.None;
				}
			}
		}

		if (currentAttackState == CurrentAttackState.None && regularAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_RegularAttack))
		{
			currentAttackState = CurrentAttackState.ChargingNormalAttack;
			main_ChargeTimer.Start();
		}

		if (currentAttackState == CurrentAttackState.ChargingNormalAttack && Input.IsActionJustReleased(InputActions.character_RegularAttack))
		{
			currentAttackState = CurrentAttackState.NormalAttack;

			if (main_ChargeTimer.TimeLeft > main_MaxChargeTime - (main_MaxChargeTime / main_MaximunChargeForNoCharge)) //Si el ataque se carga menos de un 10%, realiza daño base.
			{
				GD.Print($"[Main Attack]: Carga del ataque es menor del {main_MaximunChargeForNoCharge}%, por lo que el daño sera el minimo.");
				main_ChargeDamageAmount = stats.Damage;
			}
			else if (main_ChargeTimer.TimeLeft < main_MaxChargeTime / main_MinimunChargeForMaxCharge) //Si el ataque se carga mas de un 90%, realiza daño maximo.
			{
				GD.Print($"[Main Attack]: Carga del ataque es mayor del {100 - main_MinimunChargeForMaxCharge}%, por lo que el daño sera el maximo.");
				main_ChargeDamageAmount = stats.Damage * main_MaxChargeDamageMultiplier;
			}
			else //Sino, calcula el daño promedio.
			{
				float chargePercent = (main_MaxChargeTime - (float)main_ChargeTimer.TimeLeft) / main_MaxChargeTime;
				main_ChargeDamageAmount = stats.Damage * (1 + (main_MaxChargeDamageMultiplier - 1) * chargePercent);
				GD.Print($"[Main Attack]: La carga del ataque es del {chargePercent}%, por lo que le daño base sera {main_ChargeDamageAmount}");
			}

			RegularAttack(this, CalculateMouseAimDirection(), Position, main_ChargeDamageAmount);
			main_ChargeTimer.Stop();
			currentAttackState = CurrentAttackState.None;
		}

		if (currentAttackState == CurrentAttackState.None && skillAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_SkillAttack))
		{
			skill_AttackTargets.Clear();
			GD.Print("[Skill Attack]: Buscando objetivos");
			foreach (Area3D area in skill_RangeArea.GetOverlappingAreas())
			{
				if (area.GetParent() is BaseAttack) continue;   // Ignora otros ataques.
				if (area.GetParent() is not BaseCharacter target) continue; // Ignora todo lo que no sea personajes.
				if (target == this) continue; // Ignora el personaje que creo este ataque.
				if (!target.canBeDamaged) continue; //Ignora a los personajes que estan en un estado de invulnerabilidad.
				if (skill_AttackTargets.Contains(target)) continue; //Ignora los personajes que el ataque ya haya registrado.

				skill_AttackTargets.Add(target);
				currentAttackState = CurrentAttackState.SkillAttack;
				GD.Print($"[Skill Attack]: Se ha encontrado objetivo: {target}");
			}

			if (skill_AttackTargets.Count != 0)
			{
				GD.Print($"[Skill Attack]: Se han encontrado {skill_AttackTargets.Count} objetivos.");
				SkillAttackTimeout();
				skill_AttackDelaysTimer.Start();
			}
			else
			{
				GD.Print("[Skill Attack]: No se han encotrado objetivos para el ataque.");
			}
		}
		if (currentAttackState == CurrentAttackState.None && movementSkillCooldownEmpty && Input.IsActionJustPressed(InputActions.character_MovementSkill))
		{
			MovementSkill(this, CalculateMouseAimDirection(), Position);
		}

		pointerArrow.Rotation = new Vector3(0, -CalculateMouseAimDirection() + Mathf.DegToRad(90), 0);
	}

	/// <summary>
	/// La habilidad de ataque de Ranger realiza varios ataques en intervalos de delayBettewnArrows.
	/// Este metodo es llamado en cada intervalo, para crear un proyectil.
	/// </summary>
	private void SkillAttackTimeout()
	{
		GD.Print("[Skill Attack]: Realizando ataque.");
		if (skill_AttackTargets.Count != 0) //Comprueba si quedan objetivos a los que atacar
		{
			BaseCharacter target = skill_AttackTargets[0];

			foreach (BaseCharacter targetCandidate in skill_AttackTargets)
			{
				if (Position.DistanceTo(targetCandidate.Position) < Position.DistanceTo(target.Position))
				{
					target = targetCandidate;
				}
			}

			if (target.canBeDamaged) //Ignora a los personajes que estan en un estado de invulnerabilidad.
			{
				SkillAttack(this, target, Position);
			}
			skill_AttackTargets.Remove(target);
		}
		else //Termina el estado de la habilidad de ataque.
		{
			GD.Print("[Skill Attack]: No hay mas objetivos para el ataque de habilidad. Finalizado.");
			skill_AttackDelaysTimer.Stop();
			currentAttackState = CurrentAttackState.None;
			skillAttackCooldownEmpty = false;
			skillAttackCooldownTimer.Start();
		}
	}

	protected void RegularAttack(BaseCharacter owner, float directionAngle, Vector3 position, float damage)
	{
		Attack_Ranger_Main attack = RegularAttackScene.Instantiate<Attack_Ranger_Main>();
		attack.Setup(owner, new List<BaseCharacter>(), directionAngle, position, damage);
		AddChild(attack);

		regularAttackCooldownEmpty = false;
		regularAttackCooldownTimer.Start();
	}

	protected void SkillAttack(BaseCharacter owner, BaseCharacter target, Vector3 position)
	{
		Vector3 targetDirection = target.GlobalPosition - GlobalPosition;
		targetDirection.Y = 0;
		targetDirection = targetDirection.Normalized();
		float angle = Mathf.Atan2(targetDirection.X, -targetDirection.Z);

		List<BaseCharacter> targets = new List<BaseCharacter>();
		targets.Add(target);

		BaseAttack skillAttack = SkillAttackScene.Instantiate<BaseAttack>();
		skillAttack.Setup(owner, targets, angle, position, stats.Damage);
		AddChild(skillAttack);
	}

	/// <summary>
	/// Realiza una habilidad de movimiento. Los hijos pueden sobreescribirlo.
	/// </summary>
	protected override void MovementSkill(BaseCharacter owner, float directionAngle, Vector3 position)
	{
		currentAttackState = CurrentAttackState.movementSkill;
		Attack_Ranger_MovementSkill movementSkill = MovementSkillScene.Instantiate<Attack_Ranger_MovementSkill>();

		movement_CurrentInstance = movementSkill;

		movementSkill.Setup(owner, new List<BaseCharacter>(), directionAngle, position, stats.Damage);
		movementSkill.Signal_ResolveClingState += ResolveMovementSkill;
		AddChild(movementSkill);

		movementSkillCooldownEmpty = false;
		movementSkillCooldownTimer.Start();
	}

	private void ResolveMovementSkill(bool hasClinged)
	{
		if (hasClinged)
		{
			GD.Print("[Movement Skill]: Attack has clinged");
			currentAttackState = CurrentAttackState.MovementSkillClinged;	
		}
		else
		{
			GD.Print("[Movement Skill]: Attack has not clinged");
			currentAttackState = CurrentAttackState.None;
		}
	}

	protected override Vector3 GetMovementDirection()
	{
		Vector2 input = Input.GetVector(InputActions.character_Right, InputActions.character_Left, InputActions.character_Down, InputActions.character_Up);
		return (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
	}
}
