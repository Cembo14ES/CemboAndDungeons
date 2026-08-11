using Godot;
using System;

public partial class Minecraft : Label
{
	[Export] public float maxSize = (float) 1;
	[Export] public float minSize = (float) 0.75; 
	[Export] public float acceleration = (float) 0.7; 
	private bool goingUp;
	[Export] public float force = (float) 0.9; 
	private float zSpeed = 1;
	private int settingPresses = 0;
	

	public override void _Process(double delta)
	{

		if(zSpeed > maxSize){
			goingUp = false;
		}
		if(zSpeed < minSize){
			goingUp = true;
		}

		if (goingUp){
			zSpeed += force * acceleration * (float) GetProcessDeltaTime();
		}
		else{
			zSpeed -= force * acceleration * (float) GetProcessDeltaTime();
		}

		Scale = new Vector2 (zSpeed,zSpeed);
	}

	public void On_SettingsButton()
	{
		if (settingPresses == 0)
		{
			Text = "Aun No esta terminado";
		}

		if (settingPresses == 5)
		{
			Text = "Que aun no esta hecho cooooñooooo";
		}
		

		settingPresses ++;
	}

	public void On_ExitButton()
	{
		Text = "¡Chao chao!";
	}
}
