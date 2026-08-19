using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    [Export] private float Speed = 3.5f;
    [Export] public Node3D player;

    private bool isPlayerAlive = true;
    private PackedScene slimeBall;

    public override void _Ready()
    {
        // Buscar al jugador
        player = GetTree().GetFirstNodeInGroup("player") as Node3D;

        // Cargar el objeto que puede soltar
        slimeBall = GD.Load<PackedScene>("res://Prefabs/slimeball.tscn");
    }

    public override void _PhysicsProcess(double delta)
    {
        // ==========================================================
        // BUSCAR AL JUGADOR
        // ==========================================================

        if (isPlayerAlive && player != null)
        {
            // Dirección hacia el jugador
            Vector3 direction = player.GlobalPosition - GlobalPosition;

            // Mantener el enemigo en el suelo
            direction.Y = 0;

            // Comprobar que hay distancia suficiente
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
            // Si el jugador está muerto o no existe, quedarse quieto
            Velocity = Vector3.Zero;
        }

        // ==========================================================
        // MOVIMIENTO Y COLISIONES
        // ==========================================================

        MoveAndSlide();

        // ==========================================================
        // COMPROBAR SI HEMOS CHOCADO CON UNA BALA
        // ==========================================================

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision3D collision = GetSlideCollision(i);

            Node3D body = collision.GetCollider() as Node3D;

            if (body == null)
                continue;

            // Comprobar si el objeto que ha chocado es una Bullet
            if (body.Name.ToString().ToLower().Contains("bullet"))
            {
                GD.Print("[ENEMY] ¡Bala detectada!");

                // ==================================================
                // PROBABILIDAD DE SOLTAR SLIME BALL
                // ==================================================

                int number = GD.RandRange(1, 10);

                if (number <= 10)
                {
                    GD.Print("[DEBUG] ¡Suerte! Soltando item...");

                    if (slimeBall != null)
                    {
                        Node3D item = slimeBall.Instantiate<Node3D>();

                        GetParent().AddChild(item);

                        item.GlobalPosition = GlobalPosition;
                    }
                }

                // Eliminar enemigo
                QueueFree();

                return;
            }
        }
    }

    // ==============================================================
    // LLAMADO CUANDO EL JUGADOR MUERE POR ESTE ENEMIGO
    // ==============================================================

    public void PlayerDied()
    {
        isPlayerAlive = false;
    }
}