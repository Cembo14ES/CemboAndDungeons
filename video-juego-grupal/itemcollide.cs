using Godot;
using System;

public partial class itemcollide : Area3D
{
	private void _OnBodyEntered(Node3D body)
    {
        GD.Print("Body entered");
        if (body.IsInGroup("player"))
        {
            GD.Print("player entered");
            body.QueueFree();
        }
    }
}
