using Godot;
using System;

public partial class CharacterBody2d : CharacterBody2D
{
	[Export]
    public int Speed { get; set; } = 400;

    public void GetInput()
    {

        Vector2 inputDirection = Input.GetVector("Left", "Right", "Up", "Down");
        Velocity = inputDirection * Speed;
    }

    public override void _PhysicsProcess(double delta)
    {
        GetInput();
        MoveAndSlide();
    }
}
