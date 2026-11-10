using Godot;
using System;

public partial class CameraPlayer : Camera3D
{
    [Export] public Node3D Target { get; set; }
    [Export] public Vector3 Offset { get; set; } = new Vector3(0, 15, 0);
    [Export] public float Smoothness { get; set; } = 8f;

    public override void _Ready()
    {
        Target ??= GetTree().GetFirstNodeInGroup("Player") as Node3D;
        if (Target == null)
		{
			GD.PushWarning($"{Name}: no hay objetivo (grupo Player).");            
		}
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Target == null || !IsInstanceValid(Target)) return;

        Vector3 desired = Target.GlobalPosition + Offset;
        GlobalPosition = GlobalPosition.Lerp(desired, 1f - Mathf.Exp(-Smoothness * (float)delta));
    }
}
