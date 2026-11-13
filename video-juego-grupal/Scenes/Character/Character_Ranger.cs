using Godot;
using System;
using System.Collections.Generic;

public partial class Character_Ranger : BasePlayerCharacter
{
	[Export] public Sprite3D pointerArrow;
	[Export] public Camera3D camera;

	[ExportGroup("Main Attack")]
	[Export] public float maxChargeTime = 1;
	[Export] public float maxChargeDamageMultiplier = 2;
	[Export] public float maximunChargeForNoCharge = 10;
	[Export] public float minimunChargeForMaxCharge = 10;
	private Timer chargeTimer = new Timer();
	private float chargeDamageAmount;

	[ExportGroup("Skill Attack")]
	[Export] public Area3D skillRangeArea;
	[Export] public CollisionShape3D skillRangeAreaShape;
	[Export] public float areaRadious = 8;
	[Export] public float delayBettewnArrows = 0.3f;
	private List<BaseCharacter> skillAttackTargets = new List<BaseCharacter>();
	private Timer skillAttackDelaysTimer = new Timer();

	[ExportGroup ("Movement Skill")]
	[Export] public PackedScene movementSkillScene;
	private CurrentAttackState currentAttackState = 0;
	private enum CurrentAttackState { None = 0, NormalAttack = 1, SkillAttack = 2, UltimateAttack = 3}

	public override void _Ready()
	{
		SphereShape3D shape = (SphereShape3D)skillRangeAreaShape.Shape;
		shape.Radius = areaRadious;

		base._Ready();

		AddChild(chargeTimer);
		chargeTimer.WaitTime = maxChargeTime;
		chargeTimer.OneShot = true;
		chargeTimer.Timeout += () => chargeDamageAmount = maxChargeDamageMultiplier;

		AddChild(skillAttackDelaysTimer);
		skillAttackDelaysTimer.WaitTime = delayBettewnArrows;
		skillAttackDelaysTimer.OneShot = false;
		skillAttackDelaysTimer.Timeout += SkillAttackTimeout;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (currentAttackState != CurrentAttackState.SkillAttack)
		{
			Movement();
		}

		if (currentAttackState == CurrentAttackState.None && regularAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_RegularAttack))
		{
			currentAttackState = CurrentAttackState.NormalAttack;
			chargeTimer.Start();
		}

		if (currentAttackState == CurrentAttackState.NormalAttack && Input.IsActionJustReleased(InputActions.character_RegularAttack))
		{
			if (chargeTimer.TimeLeft > maxChargeTime - (maxChargeTime / maximunChargeForNoCharge)) //Si el ataque se carga menos de un 10%, realiza daño base.
			{
				GD.Print("Low charge, base damage");
				chargeDamageAmount = stats.Damage;
			}
			else if (chargeTimer.TimeLeft < maxChargeTime / minimunChargeForMaxCharge) //Si el ataque se carga mas de un 90%, realiza daño maximo.
			{
				GD.Print("High charge, max damage");
				chargeDamageAmount = stats.Damage * maxChargeDamageMultiplier;
			}
			else //Sino, calcula el daño promedio.
			{
				float chargePercent = (maxChargeTime - (float)chargeTimer.TimeLeft) / maxChargeTime;
				GD.Print("chargePercent:" + chargePercent);
				chargeDamageAmount = stats.Damage * (1 + (maxChargeDamageMultiplier - 1) * chargePercent);
			}

			GD.Print("chargeDamageAmount:" + chargeDamageAmount);
			RegularAttack(this, CalculateAimDirection(), Position, chargeDamageAmount);
			chargeTimer.Stop();
			currentAttackState = CurrentAttackState.None;
		}

		if (currentAttackState == CurrentAttackState.None && skillAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_SkillAttack))
		{
			skillAttackTargets.Clear();
			GD.Print("Start Skill Attack");
			foreach (Area3D area in skillRangeArea.GetOverlappingAreas())
			{
				if (area.GetParent() is BaseAttack) continue;   // Ignora otros ataques.
				if (area.GetParent() is not BaseCharacter target) continue; // Ignora todo lo que no sea personajes.
				if (target == this) continue; // Ignora el personaje que creo este ataque.
				if (!target.canBeDamaged) continue; //Ignora a los personajes que estan en un estado de invulnerabilidad.
				if (skillAttackTargets.Contains(target)) continue; //Ignora los personajes que el ataque ya haya registrado.

				skillAttackTargets.Add(target);
				currentAttackState = CurrentAttackState.SkillAttack;
				GD.Print("Added Target " + target.ToString());
			}

			if (skillAttackTargets.Count != 0)
			{
				SkillAttackTimeout();
				skillAttackDelaysTimer.Start();
				GD.Print("Skill Target Started");
			}	
		}

		pointerArrow.Rotation = new Vector3(0, 0, CalculateAimDirection() - Mathf.DegToRad(90));
	}

	/// <summary>
	/// La habilidad de ataque de Ranger realiza varios ataques en intervalos de delayBettewnArrows.
	/// Este metodo es llamado en cada intervalo, para crear un proyectil.
	/// </summary>
	private void SkillAttackTimeout()
	{
		GD.Print("Skill Target Timeout");
		if (skillAttackTargets.Count != 0) //Comprueba si quedan objetivos a los que atacar
		{		
			GD.Print("Running Skill Attack");
			BaseCharacter target = skillAttackTargets[0];

			foreach (BaseCharacter targetCandidate in skillAttackTargets)
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
			skillAttackTargets.Remove(target);
		}
		else //Termina el estado de la habilidad de ataque.
		{
			GD.Print("Skill Attack has no more targets");
			skillAttackDelaysTimer.Stop();
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

		List<BaseCharacter> targets = [target];
		
		BaseAttack skillAttack = SkillAttackScene.Instantiate<BaseAttack>();
		skillAttack.Setup(owner, targets, angle, position, stats.Damage);
		AddChild(skillAttack);
	}

	protected override Vector3 GetMovementDirection()
	{
		Vector2 input = Input.GetVector(InputActions.character_Right, InputActions.character_Left, InputActions.character_Down, InputActions.character_Up);
		return (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
	}

	protected override void OnCollided(Node collider)
	{
		base.OnCollided(collider);
	}

	/// <summary>
	/// Calcula la direcion que el mouse esta apuntando.
	/// </summary>
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
