using Godot;
using System;

public partial class MeleeCharacter : BasePlayerCharacter
{
	private const int ComboLength = 4; // Nº de golpes del combo; después vuelve al 1º.

	[Export] public float ComboResetTime { get; set; } = 1.6f;       // Segundos sin pulsar para reiniciar el combo.
	private Timer comboResetTimer = new Timer(); // Se reinicia en cada click; si termina, el combo vuelve a 0.
	[Export] public int attackIndex = 0;                 // Golpe actual del combo (0 = 1º, 1 = 2º, 2 = 3º, 3 = 4º).
	[Export] public Camera3D camera;
	[Export] public Sprite3D pointerArrow;

	public override void _Ready()
	{
		base._Ready();

		// Si el base falló (sin Stats o sin ataque normal) no seguimos.
		if (stats == null || RegularAttackScene == null) return;

		AddChild(comboResetTimer);
		comboResetTimer.WaitTime = ComboResetTime;
		comboResetTimer.OneShot = true;
		comboResetTimer.Timeout += () => attackIndex = 0;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (regularAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_RegularAttack))
			RegularAttack(this, CalculateAimDirection(), Position);
		pointerArrow.Rotation = new Vector3(0, 0, CalculateAimDirection() - Mathf.DegToRad(90));	
	}

	/// <summary>
	/// Ataque normal en combo: el 3er y 4º golpe hacen más daño.
	/// </summary>
	protected override void RegularAttack(BaseCharacter owner, float direction, Vector3 position)
	{
		BaseAttack attack = RegularAttackScene.Instantiate<BaseAttack>();
		attack.Setup(owner, direction, position);

		GD.Print(attackIndex);

		if (attackIndex == 2)   // 3er y 4º golpe (índices 2 y 3)
		{
			attack.stats.AttackMultiplier = 1.2f;
		}
		else if (attackIndex == 3)
		{
			attack.stats.AttackMultiplier = 1.5f;
		}
		else
		{
			attack.stats.AttackMultiplier = 1f;
		}
		AddChild(attack);

		regularAttackCooldownEmpty = false;
		regularAttackCooldownTimer.Start();

		attackIndex = (attackIndex + 1) % ComboLength;
		comboResetTimer.Start();
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