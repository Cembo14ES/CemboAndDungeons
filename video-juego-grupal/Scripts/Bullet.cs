using Godot;
using System;

public partial class Bullet : CharacterBody3D
{
    private const float Velocidad = 6.0f;
    private const float TiempoVida = 3.0f;

    private Area3D area3D;

    public override void _Ready()
    {
        // ==========================
        // Buscar el Area3D
        // ==========================
        area3D = GetNode<Area3D>("Area3D");

        // Conectar la señal para detectar cuerpos
        area3D.BodyEntered += AlEntrarEnArea;

        // ==========================
        // Destruir bala después de 3 segundos
        // ==========================
        SceneTreeTimer timer = GetTree().CreateTimer(TiempoVida);
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
        Velocity = GlobalTransform.Basis.X * Velocidad;

        MoveAndSlide();

        // ==========================
        // Detectar colisiones físicas
        // ==========================
        int colisiones = GetSlideCollisionCount();

        for (int i = 0; i < colisiones; i++)
        {
            KinematicCollision3D col = GetSlideCollision(i);

            Node objetoChocado = col.GetCollider() as Node;

            if (objetoChocado == null)
                continue;

            // ==========================
            // Si toca un enemigo
            // ==========================
            if (objetoChocado.Name.ToString().ToLower().Contains("enemy"))
            {
                GD.Print("[BULLET] ¡Enemigo alcanzado!");

                objetoChocado.QueueFree();
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

    // ==========================================================
    // CUANDO ALGO ENTRA EN EL AREA3D
    // ==========================================================

    private void AlEntrarEnArea(Node3D body)
    {
        if (body == null)
            return;

        // ==========================
        // Comprobar si es enemigo
        // ==========================

        if (body.Name.ToString().ToLower().Contains("enemy"))
        {
            GD.Print("[BULLET] ¡Area3D ha detectado un enemigo!");

            // Eliminar enemigo
            body.QueueFree();

            // Eliminar bala
            QueueFree();
        }
    }
}