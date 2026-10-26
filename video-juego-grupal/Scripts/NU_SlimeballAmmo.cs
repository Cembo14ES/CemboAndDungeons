using Godot;
using System;

public partial class SlimeballAmmo : CharacterBody3D
{
    private const float Speed = 6.0f;
    private const float AliveTime = 3.0f;

    private Vector3 direction = Vector3.Zero;
    private Node3D player;

    public override void _Ready()
    {
        // Buscar al jugador
        player = GetTree().GetFirstNodeInGroup("player") as Node3D;

        // Destruir la bola después de AliveTime segundos
        SceneTreeTimer timer = GetTree().CreateTimer(AliveTime);

        timer.Timeout += () =>
        {
            if (IsInstanceValid(this))
            {
                QueueFree();
            }
        };
    }

    public void SetStartPosition()
    {
        GD.Print("[SLIMEBALL] Posición inicial: " + GlobalPosition);

        if (player != null)
        {
            // Registrar la posición del jugador SOLO al disparar
            Vector3 targetPosition = new Vector3(
                player.GlobalPosition.X,
                GlobalPosition.Y,
                player.GlobalPosition.Z
            );

            Vector3 directionVector = targetPosition - GlobalPosition;

            // Movimiento solamente en X y Z
            directionVector.Y = 0;

            direction = directionVector.Normalized();

            GD.Print("[SLIMEBALL] Objetivo registrado: " + targetPosition);
            GD.Print("[SLIMEBALL] Dirección: " + direction);
        }
        else
        {
            GD.Print("[SLIMEBALL] No se encontró al jugador.");

            direction = Vector3.Zero;
        }
    }

    public Vector3 GetDirection()
    {
        return direction;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Mover la bola
        KinematicCollision3D collision =
            MoveAndCollide(direction * Speed * (float)delta);

        // Comprobar si ha chocado con algo
        if (collision != null)
        {
            Node3D body = collision.GetCollider() as Node3D;

            if (body != null)
            {
                // Si ha chocado con el jugador
                if (body.IsInGroup("player"))
                {
                    GD.Print("[SLIMEBALL] ¡Jugador alcanzado!");

                    // Desaparece inmediatamente
                    //QueueFree();
                    return;
                }

                // Si choca con cualquier otra cosa
                QueueFree();
            }
        }
    }

    public void IgnoreEnemy(CharacterBody3D enemy)
    {
        AddCollisionExceptionWith(enemy);
    }
}