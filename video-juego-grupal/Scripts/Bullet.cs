using Godot;
using System;

public partial class Bullet : CharacterBody3D
{
    private const float Velocidad = 6.0f;
    private const float TiempoVida = 3.0f;

    public override void _Ready()
    {
        // Temporizador para destruir la bala tras 3 segundos
        SceneTreeTimer timer = GetTree().CreateTimer(TiempoVida);
        timer.Timeout += () => QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        // 1. Mover la bala siempre hacia adelante (su eje X local)
        Velocity = GlobalTransform.Basis.X * Velocidad;
        MoveAndSlide();

        // 2. Detectar colisión igual que en el Character
        int colisiones = GetSlideCollisionCount();
        for (int i = 0; i < colisiones; i++)
        {
            KinematicCollision3D col = GetSlideCollision(i);
            Node objetoChocado = (Node)col.GetCollider();

            //GD.Print($"[BULLET] La bala ha tocado a: {objetoChocado.Name}");

            if (objetoChocado.Name.ToString().ToLower().Contains("enemy"))
            {
                //GD.Print("[BULLET] ¡Impacto con enemigo! Destruyendo bala...");
                QueueFree();
                return; // Salimos para no seguir procesando
            }
            else
            {
                // Si toca una pared, también destruimos la bala
                //GD.Print("[BULLET] Impacto con entorno. Destruyendo bala...");
                QueueFree();
                return;
            }
        }
    }
}