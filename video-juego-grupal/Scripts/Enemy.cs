using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    [Export] private float Speed = 3.5f;
    [Export] public Node3D player;
    private bool isPlayerAlive = true;

    public override void _Ready()
    {
        player = GetTree().GetFirstNodeInGroup("player") as Node3D;
    }

    public override void _PhysicsProcess(double delta)
    {
        // ==========================================================
        // BUSCAR AL JUGADOR
        // ==========================================================

        if (isPlayerAlive)
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

            Node collidedObject = colision.GetCollider() as Node;

            if (collidedObject != null && collidedObject.IsInGroup("balas"))
            {
                GD.Print("[DEBUG] ¡Bala detectada! Muriendo...");

                // Eliminar la bala
                collidedObject.QueueFree();

                // Eliminar el enemigo
                QueueFree();

                return;
            }
        }
    }

    //este metodo es llamado por el jugador cuando muere por este enemigo.
    public void PlayerDied()
    {
        isPlayerAlive = false;
    }
}