using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    private const float Speed = 3.5f;

    public override void _PhysicsProcess(double delta)
    {
        // ==========================================================
        // BUSCAR AL JUGADOR
        // ==========================================================

        Node3D player = GetTree().GetFirstNodeInGroup("player") as Node3D;

        if (player != null)
        {
            // Calculamos la dirección hacia el jugador
            Vector3 direction = player.GlobalPosition - GlobalPosition;

            // Mantener al enemigo en el suelo
            direction.Y = 0;

            // Comprobar que existe una distancia suficiente
            if (direction.Length() > 0.1f)
            {
                direction = direction.Normalized();

                // Moverse hacia el jugador
                Velocity = direction * Speed;
            }
            else
            {
                Velocity = Vector3.Zero;
            }
        }
        else
        {
            // Si no encuentra al jugador, se queda quieto
            Velocity = Vector3.Zero;
        }

        // ==========================================================
        // MOVIMIENTO
        // ==========================================================

        MoveAndSlide();

        // ==========================================================
        // DETECCIÓN DE IMPACTO DE BALAS
        // ==========================================================

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision3D colision = GetSlideCollision(i);

            Node objetoChocado = colision.GetCollider() as Node;

            if (objetoChocado != null &&
                objetoChocado.IsInGroup("balas"))
            {
                GD.Print("[DEBUG] ¡Bala detectada! Muriendo...");

                // Eliminar la bala
                objetoChocado.QueueFree();

                // Eliminar el enemigo
                QueueFree();

                return;
            }
        }
    }
}