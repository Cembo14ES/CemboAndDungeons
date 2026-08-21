using Godot;
using System;

public partial class SlimeballAmmo : CharacterBody3D
{
    private const float Speed = 6.0f;
    private const float AliveTime = 3.0f;
    private const float HitRadius = 0.6f; // ajusta según el tamaño de tu bola/jugador

    private Vector3 direction = Vector3.Zero;
    private Node3D player;
    private bool hasHit = false;

    public override void _Ready()
    {
        GD.Print("[SLIMEBALL] Creada.");

        player = GetTree().GetFirstNodeInGroup("player") as Node3D;

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
            Vector3 targetPosition = new Vector3(
                player.GlobalPosition.X,
                GlobalPosition.Y,
                player.GlobalPosition.Z
            );

            Vector3 directionVector = targetPosition - GlobalPosition;
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
        if (hasHit)
            return;

        // ---------------------------------------------------
        // Detección de impacto al jugador POR DISTANCIA
        // (no depende de layers/masks ni de colisión física)
        // ---------------------------------------------------
        if (player != null)
        {
            float distToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (distToPlayer <= HitRadius)
            {
                GD.Print("[SLIMEBALL] ¡Jugador alcanzado!");
                hasHit = true;
                QueueFree();
                return;
            }
        }

        // ---------------------------------------------------
        // Movimiento y colisión física (paredes, otros objetos)
        // ---------------------------------------------------
        KinematicCollision3D collision = MoveAndCollide(direction * Speed * (float)delta);

        if (collision != null)
        {
            Node3D body = collision.GetCollider() as Node3D;

            if (body != null)
            {
                // Si por lo que sea SÍ detecta físicamente al jugador
                // (ignorar la excepción, layers, etc.)
                if (body.IsInGroup("player"))
                {
                    GD.Print("[SLIMEBALL] ¡Jugador alcanzado! (colisión física)");
                    hasHit = true;
                    QueueFree();
                    return;
                }

                // Cualquier otra cosa: destruir la bola
                hasHit = true;
                QueueFree();
            }
        }
    }

    public void IgnoreEnemy(CharacterBody3D enemy)
    {
        AddCollisionExceptionWith(enemy);
    }

    // Ignora físicamente al jugador para que nunca haya
    // empuje/depenetración entre la bola y el Player
    public void IgnorePlayerPhysically(CharacterBody3D playerBody)
    {
        AddCollisionExceptionWith(playerBody);
    }
}