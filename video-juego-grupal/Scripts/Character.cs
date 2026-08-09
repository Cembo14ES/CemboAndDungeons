using Godot;
using System;

public partial class Character : CharacterBody3D
{
    private const float Speed = 5.0f;
    private readonly PackedScene _bulletScene = GD.Load<PackedScene>("res://Prefabs/bullet.tscn");

    // ==========================
    // Vida y Escudo
    // ==========================
    private const int MaxVidas = 3;
    private const int MaxEscudo = 3;

    private int vidas = MaxVidas;
    private int escudo = MaxEscudo;

    // ==========================
    // Temporizador del escudo
    // ==========================
    private Timer shieldTimer;

    // ==========================
    // Invulnerabilidad tras daño
    // ==========================
    private bool puedeRecibirDanio = true;
    private Timer invulnerabilityTimer;

    // ==========================
    // Munición
    // ==========================
    private const int MaxBalas = 10;
    private int balas = MaxBalas;

    public override void _Ready()
    {
        // ==========================
        // Temporizador del escudo
        // ==========================
        shieldTimer = new Timer();
        shieldTimer.WaitTime = 16.0f;
        shieldTimer.OneShot = true;
        shieldTimer.Timeout += RegenerarEscudo;
        AddChild(shieldTimer);

        // ==========================
        // Temporizador de invulnerabilidad
        // ==========================
        invulnerabilityTimer = new Timer();
        invulnerabilityTimer.WaitTime = 0.8f;
        invulnerabilityTimer.OneShot = true;
        invulnerabilityTimer.Timeout += FinInvulnerabilidad;
        AddChild(invulnerabilityTimer);

        // ==========================
        // DEBUG
        // ==========================
        GD.Print("================================");
        GD.Print("[DEBUG] Personaje creado.");
        GD.Print($"[DEBUG] Vida: {vidas}/{MaxVidas}");
        GD.Print($"[DEBUG] Escudo: {escudo}/{MaxEscudo}");
        GD.Print($"[DEBUG] Balas: {balas}/{MaxBalas}");
        GD.Print("================================");
    }

    public override void _PhysicsProcess(double delta)
    {
        // ==========================
        // Movimiento
        // ==========================
        Vector2 inputDir = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_backward"
        );

        Vector3 direction = (
            Transform.Basis *
            new Vector3(inputDir.X, 0, inputDir.Y)
        ).Normalized();

        Vector3 velocity = Velocity;

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;

        MoveAndSlide();

        // ==========================
        // Colisiones
        // ==========================
        int colisiones = GetSlideCollisionCount();

        for (int i = 0; i < colisiones; i++)
        {
            KinematicCollision3D col = GetSlideCollision(i);

            Node objetoChocado = (Node)col.GetCollider();

            if (objetoChocado.Name.ToString().ToLower().Contains("enemy"))
            {
                RecibirDanio();
                break;
            }
        }

        // ==========================
        // Ataque
        // ==========================
        if (Input.IsActionJustPressed("atack"))
        {
            Atacar();
        }

        // ==========================
        // Recargar
        // ==========================
        if (Input.IsActionJustPressed("reload"))
        {
            Recargar();
        }
    }

    // ==========================================================
    // RECIBIR DAÑO
    // ==========================================================

    private void RecibirDanio()
    {
        // Si todavía está en invulnerabilidad, ignoramos el golpe
        if (!puedeRecibirDanio)
            return;

        // Activar invulnerabilidad
        puedeRecibirDanio = false;
        invulnerabilityTimer.Start();

        // Reiniciar temporizador de regeneración del escudo
        shieldTimer.Stop();

        // ==========================
        // Primero se consume el escudo
        // ==========================
        if (escudo > 0)
        {
            escudo--;

            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡Golpe recibido!");
            GD.Print("[DEBUG] Se pierde 1 punto de ESCUDO.");
        }
        else
        {
            // ==========================
            // Si no hay escudo, pierde vida
            // ==========================
            vidas--;

            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡Golpe recibido!");
            GD.Print("[DEBUG] No queda escudo.");
            GD.Print("[DEBUG] Se pierde 1 punto de VIDA.");

            // ==========================
            // Muerte
            // ==========================
            if (vidas <= 0)
            {
                GD.Print("--------------------------------");
                GD.Print("[DEBUG] ¡El personaje ha muerto!");
                GD.Print("--------------------------------");

                QueueFree();

                SetPhysicsProcess(false);

                return;
            }
        }

        // ==========================
        // Mostrar estado
        // ==========================
        GD.Print($"[DEBUG] Vida: {vidas}/{MaxVidas}");
        GD.Print($"[DEBUG] Escudo: {escudo}/{MaxEscudo}");

        // ==========================
        // Iniciar regeneración
        // ==========================
        shieldTimer.Start();

        GD.Print("[DEBUG] El escudo se regenerará en 16 segundos si no recibes más daño.");
        GD.Print("[DEBUG] Invulnerable durante 0.8 segundos.");
        GD.Print("--------------------------------");
    }

    // ==========================================================
    // FIN INVULNERABILIDAD
    // ==========================================================

    private void FinInvulnerabilidad()
    {
        puedeRecibirDanio = true;

        GD.Print("[DEBUG] Fin de la invulnerabilidad.");
    }

    // ==========================================================
    // REGENERAR ESCUDO
    // ==========================================================

    private void RegenerarEscudo()
    {
        escudo = MaxEscudo;

        GD.Print("================================");
        GD.Print("[DEBUG] ¡ESCUDO REGENERADO!");
        GD.Print($"[DEBUG] Vida: {vidas}/{MaxVidas}");
        GD.Print($"[DEBUG] Escudo: {escudo}/{MaxEscudo}");
        GD.Print("================================");
    }

    // ==========================================================
    // ATAQUE
    // ==========================================================

    private void Atacar()
    {
        // ==========================
        // Si no quedan balas
        // ==========================
        if (balas <= 0)
        {
            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡No quedan balas!");
            GD.Print("[DEBUG] ATACK utilizado como recarga.");

            Recargar();

            GD.Print("--------------------------------");

            return;
        }

        // ==========================
        // Crear bala
        // ==========================
        Node3D nuevoObjeto = _bulletScene.Instantiate<Node3D>();

        GetParent().AddChild(nuevoObjeto);

        // ==========================
        // Posición de aparición
        // ==========================
        Vector3 offsetLocal = new Vector3(2.0f, 0.0f, 0.2f);

        nuevoObjeto.GlobalPosition =
            GlobalPosition +
            (GlobalTransform.Basis * offsetLocal);

        // ==========================
        // Gastar bala
        // ==========================
        balas--;

        GD.Print("--------------------------------");
        GD.Print("[DEBUG] ¡Disparo!");
        GD.Print($"[DEBUG] Balas restantes: {balas}/{MaxBalas}");

        // ==========================
        // Avisar si se queda vacío
        // ==========================
        if (balas == 0)
        {
            GD.Print("[DEBUG] ¡Cargador vacío!");
            GD.Print("[DEBUG] Pulsa ATACK para recargar.");
            GD.Print("[DEBUG] También puedes usar RELOAD.");
        }

        GD.Print("--------------------------------");
    }

    // ==========================================================
    // RECARGAR
    // ==========================================================

    private void Recargar()
    {
        // ==========================
        // Si ya está lleno
        // ==========================
        if (balas >= MaxBalas)
        {
            GD.Print("[DEBUG] El cargador ya está lleno.");

            return;
        }

        // ==========================
        // Recargar
        // ==========================
        balas = MaxBalas;

        GD.Print("================================");
        GD.Print("[DEBUG] ¡ARMA RECARGADA!");
        GD.Print($"[DEBUG] Balas: {balas}/{MaxBalas}");
        GD.Print("================================");
    }
}