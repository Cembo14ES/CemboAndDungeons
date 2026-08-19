using Godot;
using System;

public partial class Bullet : CharacterBody3D
{
    private const float Speed = 6.0f;
    private const float AliveTime = 3.0f;

    [Export] private Area3D area3D;

    public override void _Ready()
    {
        // ==========================
        // Destruir bala después de 3 segundos
        // ==========================
        SceneTreeTimer timer = GetTree().CreateTimer(AliveTime);
        timer.Timeout += () =>
        {
            if (IsInstanceValid(this))
            {
                QueueFree();
            }
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        // ==========================
        // Mover la bala hacia delante
        // Eje X local
        // ==========================
        Velocity = GlobalTransform.Basis.X * Speed;

        MoveAndSlide();

        // ==========================
        // Detectar colisiones físicas
        // ==========================
        int colisions = GetSlideCollisionCount();

        for (int i = 0; i < colisions; i++)
        {
            KinematicCollision3D col = GetSlideCollision(i);

            Node colidedObject = col.GetCollider() as Node;

            if (colidedObject == null)
                continue;

            // ==========================
            // Si toca un enemigo
            // ==========================
            if (colidedObject.Name.ToString().ToLower().Contains("enemy"))
            {
                GD.Print("[BULLET] ¡Enemigo alcanzado!");

                //colidedObject.QueueFree();
                QueueFree();

                return;
            }

            // ==========================
            // Si toca cualquier otra cosa
            // ==========================
            QueueFree();
            return;
        }
    }
}