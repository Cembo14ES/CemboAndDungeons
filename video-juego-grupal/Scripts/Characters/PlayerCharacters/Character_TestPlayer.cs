using Godot;
using System;

public partial class Character_TestPlayer : BasePlayerCharacter
{
	[Export] public Sprite3D pointerArrow;
	[Export] public Camera3D camera;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
	}
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (regularAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_RegularAttack))
			RegularAttack(this, CalculateAimDirection(), Position);
		
		if (skillAttackCooldownEmpty && Input.IsActionJustPressed(InputActions.character_SkillAttack))
			SkillAttack(this, CalculateAimDirection(), Position);
	
		pointerArrow.Rotation = new Vector3(0, 0, CalculateAimDirection() - Mathf.DegToRad(90));	
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
    