using Godot;
using System;

public partial class Character : CharacterBody3D
{
    [Export] private float playerSpeed = 5.0f;

    // ==========================
    // VIDA Y ESCUDO
    // ==========================
    [Export] private int maxLives = 3;
    [Export] private int maxShields = 3;

    private int currentLives;
    private int currentShields;

    // ==========================
    // TEMPORIZADOR ESCUDO
    // ==========================
    private Timer shieldTimer;

    // ==========================
    // INVULNERABILIDAD
    // ==========================
    [Export] private bool canBeDamaged = true;
    private Timer invulnerabilityTimer;

    // ==========================
    // MUNICIÓN
    // ==========================
    [Export] private int maxAmmo = 10;
    private int currentAmmo;

    // ==========================
    // BALA
    // ==========================
    private PackedScene _bulletScene;

    // ==========================
    // ARMA
    // ==========================
    private Node3D gun;
    private WeaponRotation weaponRotation;

    public override void _Ready()
    {
        // ==========================
        // Cargar Bullet.tscn
        // ==========================
        _bulletScene =
            GD.Load<PackedScene>(
                "res://Prefabs//bullet.tscn"
            );

        if (_bulletScene == null)
        {
            GD.PrintErr(
                "[ERROR] No se ha encontrado Bullet.tscn"
            );

            GD.PrintErr(
                "[ERROR] Comprueba la ruta: res://Prefabs//bullet.tscn"
            );
        }

        // ==========================
        // Valores iniciales
        // ==========================
        currentAmmo = maxAmmo;
        currentLives = maxLives;
        currentShields = maxShields;

        // ==========================
        // Gun
        // ==========================
        gun = GetNode<Node3D>("Gun");

        // ==========================
        // Sprite3D del arma
        // ==========================
        weaponRotation =
            GetNode<WeaponRotation>(
                "Gun/Sprite3D"
            );

        // ==========================
        // Temporizador escudo
        // ==========================
        shieldTimer = new Timer
        {
            WaitTime = 16.0f,
            OneShot = true
        };

        shieldTimer.Timeout += RegenerateShield;
        AddChild(shieldTimer);

        // ==========================
        // Temporizador invulnerabilidad
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
        GD.Print($"[DEBUG] Gun encontrado: {gun != null}");
        GD.Print(
            $"[DEBUG] WeaponRotation encontrado: {weaponRotation != null}"
        );
        GD.Print(
            $"[DEBUG] Bullet Scene cargada: {_bulletScene != null}"
        );
        GD.Print("================================");
    }

    public override void _PhysicsProcess(double delta)
    {
        // ==========================
        // MOVIMIENTO
        // ==========================
        Vector2 inputDir = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_backward"
        );

        Vector3 direction =
            (
                Transform.Basis *
                new Vector3(
                    inputDir.X,
                    0,
                    inputDir.Y
                )
            ).Normalized();

        Vector3 velocity = Velocity;

        if (direction != Vector3.Zero)
        {
            velocity.X =
                direction.X * playerSpeed;

            velocity.Z =
                direction.Z * playerSpeed;
        }
        else
        {
            velocity.X =
                Mathf.MoveToward(
                    Velocity.X,
                    0,
                    playerSpeed
                );

            velocity.Z =
                Mathf.MoveToward(
                    Velocity.Z,
                    0,
                    playerSpeed
                );
        }

        Velocity = velocity;

        MoveAndSlide();

        // ==========================
        // COLISIONES
        // ==========================
        int colisions =
            GetSlideCollisionCount();

        for (int i = 0; i < colisions; i++)
        {
            KinematicCollision3D col =
                GetSlideCollision(i);

            Node collidedObject =
                (Node)col.GetCollider();

            if (
                collidedObject != null &&
                collidedObject.Name
                    .ToString()
                    .ToLower()
                    .Contains("enemy")
            )
            {
                Enemy enemy =
                    (Enemy)collidedObject;

                enemy.PlayerDied();

                GetHurt();

                break;
            }
        }

        // ==========================
        // ATAQUE
        // ==========================
        if (Input.IsActionJustPressed("atack"))
        {
            Atack();
        }

        // ==========================
        // RECARGAR
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
        if (!canBeDamaged)
            return;

        canBeDamaged = false;

        invulnerabilityTimer.Start();

        shieldTimer.Stop();

        if (currentShields > 0)
        {
            currentShields--;

            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡Golpe recibido!");
            GD.Print("[DEBUG] Se pierde 1 punto de ESCUDO.");
        }
        else
        {
            currentLives--;

            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡Golpe recibido!");
            GD.Print("[DEBUG] No queda escudo.");
            GD.Print("[DEBUG] Se pierde 1 punto de VIDA.");

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

        GD.Print(
            $"[DEBUG] Vida: {currentLives}/{maxLives}"
        );

        GD.Print(
            $"[DEBUG] Escudo: {currentShields}/{maxShields}"
        );

        shieldTimer.Start();

        GD.Print(
            "[DEBUG] El escudo se regenerará en 16 segundos si no recibes más daño."
        );

        GD.Print(
            "[DEBUG] Invulnerable durante 0.8 segundos."
        );

        GD.Print("--------------------------------");
    }

    // ==========================================================
    // FIN INVULNERABILIDAD
    // ==========================================================

    private void EndInvulnerability()
    {
        canBeDamaged = true;

        GD.Print(
            "[DEBUG] Fin de la invulnerabilidad."
        );
    }

    // ==========================================================
    // REGENERAR ESCUDO
    // ==========================================================

    private void RegenerateShield()
    {
        currentShields = maxShields;

        GD.Print("================================");
        GD.Print("[DEBUG] ¡ESCUDO REGENERADO!");

        GD.Print(
            $"[DEBUG] Vida: {currentLives}/{maxLives}"
        );

        GD.Print(
            $"[DEBUG] Escudo: {currentShields}/{maxShields}"
        );

        GD.Print("================================");
    }

    // ==========================================================
    // ATAQUE
    // ==========================================================

    private void Atack()
    {
        // ==========================
        // SIN MUNICIÓN
        // ==========================
        if (currentAmmo <= 0)
        {
            GD.Print("--------------------------------");
            GD.Print("[DEBUG] ¡No quedan balas!");
            GD.Print(
                "[DEBUG] ATACK utilizado como recarga."
            );

            Reload();

            GD.Print("--------------------------------");

            return;
        }

        // ==========================
        // COMPROBAR BULLET
        // ==========================
        if (_bulletScene == null)
        {
            GD.PrintErr(
                "[ERROR] Bullet.tscn no está cargado."
            );

            return;
        }

        // ==========================
        // COMPROBAR ARMA
        // ==========================
        if (weaponRotation == null)
        {
            GD.PrintErr(
                "[ERROR] No se encuentra WeaponRotation."
            );

            return;
        }

        // ==========================
        // CREAR BALA
        // ==========================
        Node3D bulletInstance =
            _bulletScene.Instantiate<Node3D>();

        GetParent().AddChild(bulletInstance);

        // ==========================
        // POSICIÓN DE SALIDA
        // ==========================
        Vector3 localOffset =
            new Vector3(
                2.4f,
                0.0f,
                -0.4f
            );

        bulletInstance.GlobalPosition =
            weaponRotation.GlobalPosition +
            (
                weaponRotation.GlobalTransform.Basis *
                localOffset
            );

        // ======================================================
        // DIRECCIÓN DE LA BALA
        // ======================================================
        //
        // NO calculamos el ratón.
        //
        // Usamos directamente el Rotation.Y
        // del arma.
        //
        // El arma apunta con su eje X.
        //
        // Y = 0°   → +X
        // Y = 90°  → -Z
        // Y = 180° → -X
        // Y = -90° → +Z
        //
        // ======================================================

        float weaponAngle =
            weaponRotation.GlobalRotation.Y;

        Vector3 bulletDirection =
            new Vector3(
                Mathf.Cos(weaponAngle),
                0,
                -Mathf.Sin(weaponAngle)
            );

        bulletDirection =
            bulletDirection.Normalized();

        // ==========================
        // ROTAR LA BALA
        // ==========================
        float bulletAngle =
            Mathf.Atan2(
                bulletDirection.Z,
                bulletDirection.X
            );

        bulletInstance.GlobalRotation =
            new Vector3(
                0,
                -bulletAngle,
                0
            );

        // ==========================
        // GASTAR BALA
        // ==========================
        currentAmmo--;

        GD.Print("--------------------------------");
        GD.Print("[DEBUG] ¡Disparo!");
        GD.Print(
            $"[DEBUG] Balas restantes: {currentAmmo}/{maxAmmo}"
        );

        // ==========================
        // CARGADOR VACÍO
        // ==========================
        if (currentAmmo == 0)
        {
            GD.Print("[DEBUG] ¡Cargador vacío!");
            GD.Print(
                "[DEBUG] Pulsa ATACK para recargar."
            );
            GD.Print(
                "[DEBUG] También puedes usar RELOAD."
            );
        }

        GD.Print("--------------------------------");
    }

    // ==========================================================
    // RECARGAR
    // ==========================================================

    private void Reload()
    {
        if (currentAmmo >= maxAmmo)
        {
            GD.Print(
                "[DEBUG] El cargador ya está lleno."
            );

            return;
        }

        currentAmmo = maxAmmo;

        GD.Print("================================");
        GD.Print("[DEBUG] ¡ARMA RECARGADA!");
        GD.Print(
            $"[DEBUG] Balas: {currentAmmo}/{maxAmmo}"
        );
        GD.Print("================================");
    }
}