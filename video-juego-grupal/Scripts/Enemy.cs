using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    private const float Speed = 3.5f;

    public override void _PhysicsProcess(double delta)
    {
        // 1. Lógica de persecución
        Node3D player = GetTree().CurrentScene.GetNodeOrNull<Node3D>("Character");
        if (player != null)
        {
            Vector3 direction = (player.GlobalPosition - GlobalPosition).Normalized();
            direction.Y = 0; // Mantenemos el enemigo en el suelo
            Velocity = direction * Speed;
        }

        MoveAndSlide();

        // 2. Detección de impacto
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision3D colision = GetSlideCollision(i);
            Node objetoChocado = colision.GetCollider() as Node;

            if (objetoChocado != null && objetoChocado.IsInGroup("balas"))
            {
                GD.Print("[DEBUG] ¡Bala detectada! Muriendo...");
                objetoChocado.QueueFree(); // Borra la bala
                QueueFree();               // Borra al enemigo
                return;                    // Salimos para evitar errores
            }
        }
    }
}