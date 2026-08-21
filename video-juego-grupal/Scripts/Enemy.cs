using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    [Export] private float Speed = 3.5f;
    [Export] public Node3D player;

    private bool isPlayerAlive = true;

    private PackedScene slimeBall;
    private PackedScene slimeBallAmmo;

    private bool isAtacking = false;

    private double attackTimer = 0.0f;
    private int lastPrintedSecond = -1;

    public override void _Ready()
    {
        player = GetTree().GetFirstNodeInGroup("player") as Node3D;

        slimeBall = GD.Load<PackedScene>("res://Prefabs/slimeball.tscn");
        slimeBallAmmo = GD.Load<PackedScene>("res://Prefabs/slimeballammo.tscn");

        attackTimer = GD.RandRange(1.0f, 1.0f);

        GD.Print("[ENEMY] Próximo ataque en " + Mathf.CeilToInt(attackTimer) + " segundos.");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isAtacking)
        {
            attackTimer -= delta;
            int secondsLeft = Mathf.CeilToInt((float)attackTimer);

            if (secondsLeft != lastPrintedSecond && secondsLeft > 0)
            {
                GD.Print("[ENEMY] Ataque en " + secondsLeft + " segundos.");
                lastPrintedSecond = secondsLeft;
            }

            if (attackTimer <= 0)
            {
                attack();
            }
        }

        if (isPlayerAlive && player != null && !isAtacking)
        {
            Vector3 direction = player.GlobalPosition - GlobalPosition;
            direction.Y = 0;

            if (direction.Length() > 0.1f)
            {
                direction = direction.Normalized();
                Velocity = direction * Speed;
            }
            else
            {
                Velocity = Vector3.Zero;
            }
        }
        else
        {
            Velocity = Vector3.Zero;
        }

        MoveAndSlide();

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision3D collision = GetSlideCollision(i);
            Node3D body = collision.GetCollider() as Node3D;

            if (body == null)
                continue;

            if (body.Name.ToString().ToLower().Contains("bullet"))
            {
                GD.Print("[ENEMY] ¡Bala detectada!");

                int number = GD.RandRange(1, 10);
                if (number <= 10)
                {
                    GD.Print("[DEBUG] ¡Suerte! Soltando item...");
                    SpawnSlimeBall();
                }

                QueueFree();
                return;
            }
        }
    }

    private async void attack()
    {
        isAtacking = true;
        Velocity = Vector3.Zero;

        GD.Print("[ENEMY] ¡Atacando!");

        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

        SpawnSlimeBallAmmo();

        GD.Print("[ENEMY] ¡Slimeball disparada!");

        isAtacking = false;
        attackTimer = GD.RandRange(1f, 1f);
        lastPrintedSecond = -1;

        GD.Print("[ENEMY] Próximo ataque en " + Mathf.CeilToInt((float)attackTimer) + " segundos.");
    }

    private void SpawnSlimeBall()
    {
        if (slimeBall == null)
        {
            GD.PrintErr("[ENEMY] ERROR: slimeBall no está cargado.");
            return;
        }

        GD.Print("[ENEMY] Generando SlimeBall...");

        Node3D item = slimeBall.Instantiate<Node3D>();
        GetParent().AddChild(item);
        item.GlobalPosition = GlobalPosition;
    }

    private void SpawnSlimeBallAmmo()
    {
        if (slimeBallAmmo == null)
        {
            GD.PrintErr("[ENEMY] ERROR: slimeBallAmmo no está cargado.");
            return;
        }

        GD.Print("[ENEMY] Generando SlimeBallAmmo...");

        SlimeballAmmo item = slimeBallAmmo.Instantiate<SlimeballAmmo>();

        // Añadir al árbol PRIMERO
        GetTree().CurrentScene.AddChild(item);

        // Posición inicial = posición del enemigo
        item.GlobalPosition = GlobalPosition;

        // Evitar que la física intente "despegar" la bola del enemigo o del jugador
        item.IgnoreEnemy(this);
        if (player is CharacterBody3D playerBody)
        {
            item.IgnorePlayerPhysically(playerBody);
        }

        // Calcular dirección y guardar referencia al jugador para detección por distancia
        item.SetStartPosition();

        // Desplazarla fuera del cuerpo del enemigo en la dirección de disparo
        item.GlobalPosition += item.GetDirection() * 1.0f;

        GD.Print("[ENEMY] Posición de spawn: " + item.GlobalPosition);
    }

    public void PlayerDied()
    {
        isPlayerAlive = false;
    }

    private void goBack()
    {
        // Implementar lógica de esquivar aquí
    }
}