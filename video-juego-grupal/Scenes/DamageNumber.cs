using Godot;
using System;

public partial class DamageNumber : Marker3D
{
	[Export] public Label3D label;
	private float damageValue = 0;
	private float textOffsetAngle = 180;
	private Timer aliveTimer = new Timer();
	private float aliveTime = 1;
	private Vector3 textOffsetDistance = Vector3.Up;

	public override void _Ready()
	{	
		label.Text = damageValue.ToString();

		AddChild(aliveTimer);
			aliveTimer.WaitTime = aliveTime;
			aliveTimer.OneShot = true;
			aliveTimer.Timeout += Die;
			aliveTimer.Start();

		Tween tween = GetTree().CreateTween();

		Vector3 offset = new Vector3(Mathf.Sin(-textOffsetAngle), 0, Mathf.Cos(-textOffsetAngle)) * -textOffsetDistance;

		tween.TweenProperty(this, "position", offset, 0.1);

	}

	public void Setup(float damage, float angle, Vector3 direction)
	{
		damageValue = damage;
		textOffsetAngle = angle;
		textOffsetDistance = direction;
	}

	private void Die()
	{
		QueueFree();
	}
}
