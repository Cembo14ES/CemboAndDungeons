using Godot;
using System;

public partial class EndLevel : Area3D
{
	[Signal] public delegate void ExitEnteredEventHandler();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void On_PlayerEnterCollision(Node3D body)
	{
		if (body.Name == "Character")
		{
			GD.Print("End entered");
			EmitSignal(SignalName.ExitEntered);
		}
	}
}
