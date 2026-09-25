using Godot;
using System;

public partial class SimpleCharacter : CharacterBody3D
{
	[Export] private AnimatedSprite3D sprite; //
	[Export] private float Speed = 50.0f; //

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity; //

		Vector2 inputDir = new Vector2(); //[cite: 12]
		if (!DebugMaster.Instance.debugCameraEnabled) //[cite: 12]
		{
			inputDir = Input.GetVector("Left", "Right", "Down", "Up"); //[cite: 12]
		}
		
		Vector3 direction = Vector3.Zero;

		// Camera-relative movement logic
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera != null && inputDir != Vector2.Zero)
		{
			// Get the camera's forward and right vectors
			Vector3 camForward = -camera.GlobalTransform.Basis.Z;
			Vector3 camRight = camera.GlobalTransform.Basis.X;

			// Flatten the vectors on the Y axis so the character stays on the ground
			camForward.Y = 0;
			camForward = camForward.Normalized();
			
			camRight.Y = 0;
			camRight = camRight.Normalized();

			// Calculate final direction based on input and camera rotation
			direction = (camRight * inputDir.X + camForward * inputDir.Y).Normalized();
		}

		if (direction != Vector3.Zero) //[cite: 12]
		{
			velocity.X = direction.X * Speed; //[cite: 12]
			velocity.Z = direction.Z * Speed; //[cite: 12]
		}
		else //[cite: 12]
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed); //[cite: 12]
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed); //[cite: 12]
		}

		Velocity = velocity; //[cite: 12]
		MoveAndSlide(); //[cite: 12]

		// Animation logic stays the same
		if (inputDir != Vector2.Zero) //[cite: 12]
		{
			if(inputDir.X > inputDir.Y) //[cite: 12]
			{
				if(inputDir.X > 0)		 //[cite: 12]
					sprite.Play("Walk_Right"); //[cite: 12]
				else //[cite: 12]
					sprite.Play("Walk_Down"); //[cite: 12]
			}
			else //[cite: 12]
			{
				if(inputDir.Y > 0)		 //[cite: 12]
					sprite.Play("Walk_Up"); //[cite: 12]
				else //[cite: 12]
					sprite.Play("Walk_Left"); //[cite: 12]
			}
		}
		else //[cite: 12]
			sprite.Play("Idle");		 //[cite: 12]
	}
}
