using Godot;
using System;

public partial class Character : CharacterBody3D
{
    [Export] private float playerSpeed = 5.0f;

    // ==========================
    // Vida y Escudo
    // ==========================
    [Export] private int maxLives = 3;
    [Export] private int maxShields = 3;

    private int currentLives;
    private int currentShields;

    // ==========================
    // Temporizador del escudo
    // ==========================
    private Timer shieldTimer;

    // ==========================
    // Invulnerabilidad tras daño
    // ==========================
    [Export] private bool canBeDamaged = true;
    private Timer invulnerabilityTimer;

    // ==========================
    // Munición
    // ==========================
    [Export] private int maxAmmo = 10;
    private int currentAmmo;

    // ==========================
    // Escenas
    // ==========================

    [Export] private PackedScene _bulletScene;

    public override void _Ready()
    {
        // ==========================
        // Variable initialization
        // ==========================

        currentAmmo = maxAmmo;
        currentLives = maxLives;
        currentShields = maxShields;

        // ==========================
        // Temporizador Escudo
        // ==========================
        shieldTimer = new Timer
        {
            WaitTime = 16.0f,
            OneShot = true
        };
        shieldTimer.Timeout += RegenerateShield;
        AddChild(shieldTimer);

        // ==========================
        // Temporizador Invulnerabilidad
        // ==========================
        invulnerabilityTimer = new Timer
        {
            WaitTime = 0.8f,
            OneShot = true
        };
        invulnerabilityTimer.Timeout += EndInvulnerability;
        AddChild(invulnerabilityTimer);

        // ==========================
        // DEBUG
        // ==========================
        GD.Print("================================");
        GD.Print("[DEBUG] Character Created");
        GD.Print($"[DEBUG] Lives: {currentLives}/{maxLives}");
        GD.Print($"[DEBUG] Shield: {currentShields}/{maxShields}");
        GD.Print($"[DEBUG] Ammo: {currentAmmo}/{maxAmmo}");
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
            velocity.X = direction.X * playerSpeed;
            velocity.Z = direction.Z * playerSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, playerSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, playerSpeed);
        }

        Velocity = velocity;

        MoveAndSlide();

        // ==========================
        // Colisiones
        // ==========================
        int colisions = GetSlideCollisionCount();

        for (int i = 0; i < colisions; i++)
        {
            KinematicCollision3D col = GetSlideCollision(i);

            Node collidedObject = (Node)col.GetCollider();

            if (collidedObject.Name.ToString().ToLower().Contains("enemy"))
            {
                Enemy enemy = (Enemy) collidedObject;
                enemy.PlayerDied();
                GetHurt();
                break;
            }
        }

        // ==========================
        // Ataque
        // ==========================
        if (Input.IsActionJustPressed("atack"))
        {
            Atack();
        }

        // ==========================
        // Recargar
        // ==========================
        if (Input.IsActionJustPressed("reload"))
        {
            Reload();
        }
    }

    // ==========================================================
    // RECIBIR DAÑO
    // ==========================================================

    private void GetHurt()
    {
        // Si todavía está en invulnerabilidad, ignoramos el golpe
        if (!canBeDamaged)
            return;

        // Activar invulnerabilidad
        canBeDamaged = false;
        invulnerabilityTimer.Start();

        // Reiniciar temporizador de regeneración del escudo
        shieldTimer.Stop();

        // ==========================
        // Primero se consume el escudo
        // ==========================
        if (currentShields > 0)
        {
            currentShields--;

            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡Golpe recibido!");
            GD.Print("[DEBUG] Se pierde 1 punto de ESCUDO.");
        }
        else
        {
            // ==========================
            // Si no hay escudo, pierde vida
            // ==========================
            currentLives--;

            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡Golpe recibido!");
            GD.Print("[DEBUG] No queda escudo.");
            GD.Print("[DEBUG] Se pierde 1 punto de VIDA.");

            // ==========================
            // Muerte
            // ==========================
            if (currentLives <= 0)
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
        GD.Print($"[DEBUG] Vida: {currentLives}/{maxLives}");
        GD.Print($"[DEBUG] Escudo: {currentShields}/{maxShields}");

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

    private void EndInvulnerability()
    {
        canBeDamaged = true;
        GD.Print("[DEBUG] Fin de la invulnerabilidad.");
    }

    // ==========================================================
    // REGENERAR ESCUDO
    // ==========================================================

    private void RegenerateShield()
    {
        currentShields = maxShields;

        GD.Print("================================");
        GD.Print("[DEBUG] ¡ESCUDO REGENERADO!");
        GD.Print($"[DEBUG] Vida: {currentLives}/{maxLives}");
        GD.Print($"[DEBUG] Escudo: {currentShields}/{maxShields}");
        GD.Print("================================");
    }

    // ==========================================================
    // ATAQUE
    // ==========================================================

    private void Atack()
    {
        // ==========================
        // Si no quedan balas
        // ==========================
        if (currentAmmo <= 0)
        {
            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡No quedan balas!");
            GD.Print("[DEBUG] ATACK utilizado como recarga.");

            Reload();

            GD.Print("--------------------------------");

            return;
        }

        // ==========================
        // Crear bala
        // ==========================
        Node3D bulletInstance = _bulletScene.Instantiate<Node3D>();

        GetParent().AddChild(bulletInstance);

        // ==========================
        // Posición de aparición
        // ==========================
        Vector3 localOffset = new Vector3(2.0f, 0.0f, 0.2f);

        bulletInstance.GlobalPosition = GlobalPosition + (GlobalTransform.Basis * localOffset);

        // ==========================
        // Gastar bala
        // ==========================
        currentAmmo--;

        GD.Print("--------------------------------");
        GD.Print("[DEBUG] ¡Disparo!");
        GD.Print($"[DEBUG] Balas restantes: {currentAmmo}/{maxAmmo}");

        // ==========================
        // Avisar si se queda vacío
        // ==========================
        if (currentAmmo == 0)
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

    private void Reload()
    {
        // ==========================
        // Si ya está lleno
        // ==========================
        if (currentAmmo >= maxAmmo)
        {
            GD.Print("[DEBUG] El cargador ya está lleno.");

            return;
        }

        // ==========================
        // Recargar
        // ==========================
        currentAmmo = maxAmmo;

        GD.Print("================================");
        GD.Print("[DEBUG] ¡ARMA RECARGADA!");
        GD.Print($"[DEBUG] Balas: {currentAmmo}/{maxAmmo}");
        GD.Print("================================");
    }
}