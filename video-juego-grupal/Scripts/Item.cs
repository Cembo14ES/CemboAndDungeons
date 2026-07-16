using Godot;
using System;

public partial class Item : CharacterBody3D
{
	[Export] public float maxSpeed = (float) 5; // Items = 1
	[Export] public float acceleration = (float) 1; // Items = 0.005
	private bool goingUp;
	[Export] public float force = (float) 0.5; // Items = 1
	private float zSpeed;
	[Export] CharacterBody3D body;

	public override void _Process(double delta)
	{

		if(zSpeed > maxSpeed){
			goingUp = false;
			GD.Print("FALSE" + zSpeed);
		}
		if(zSpeed < -maxSpeed){
			goingUp = true;
			GD.Print("TRUE" + zSpeed);
		}

		if (goingUp){
			zSpeed = zSpeed + force * acceleration * (float) GetProcessDeltaTime();
		}
		else{
			zSpeed = zSpeed - force * acceleration * (float) GetProcessDeltaTime();
		}

		body.Velocity = new Vector3(body.Velocity.X, body.Velocity.Z, zSpeed);
		body.MoveAndSlide();
	}
}
