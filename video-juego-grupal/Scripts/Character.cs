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
    [Export] private PackedScene _bulletScene;

    // ==========================
    // ARMA
    // ==========================
    [Export] private Node3D gun;
    [Export] private WeaponRotation weaponRotation;
    [Export] private AudioStreamPlayer gunShotSound;
    [Export] private AudioStreamPlayer reloadSound;
    private double positionTimer = 0.0;

    public override void _Ready()
    {
        //Con poner [Export] en _bulletScene te ahorras tener que buscar a mano la ruta (y que de error si esta mal)


        // ==========================
        // Valores iniciales
        // ==========================
        currentAmmo = maxAmmo;
        currentLives = maxLives;
        currentShields = maxShields;

        //Lo mismo que con _bulletScene
        //gun = GetNode<Node3D>("Gun");

        //Lo mismo que con _bulletScene
        //weaponRotation = GetNode<WeaponRotation>("Gun/Sprite3D");

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

        //Lo mismo que con _bulletScene
        //gunShotSound = GetNode<AudioStreamPlayer>("Shot");
        //reloadSound = GetNode<AudioStreamPlayer>("Reload");
    }

    public override void _PhysicsProcess(double delta)
    {
        // ==========================
        // MOVIMIENTO
        // ==========================
        positionTimer += delta;

        if (positionTimer >= 1.0)
        {
            GD.Print(
                "POSICIÓN DEL JUGADOR → X: " +
                GlobalPosition.X +
                " | Y: " +
                GlobalPosition.Y +
                " | Z: " +
                GlobalPosition.Z
            );

            positionTimer = 0.0;
        }
        
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

        // COLISIONES        
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            Node collidedObject = (Node)GetSlideCollision(i).GetCollider();

            // Colisión con Enemy
            if (collidedObject is Enemy enemy)
            {
                GD.Print("[DEBUG] Colisión con Enemy detectada.");

                GetHurt();

                if (currentLives == 0)
                {
                    enemy.PlayerDied();
                }
            }

            // Colisión con SlimeballAmmo
            else if (collidedObject is SlimeballAmmo slimeball)
            {
                GD.Print("[DEBUG] Colisión con SlimeballAmmo detectada.");

                GetHurt();

                // Destruir la slimeball al golpear al jugador
                slimeball.QueueFree();
            }
        }

        // ATAQUE
        if (Input.IsActionJustPressed("atack"))
        {
            Atack();
        }

        // RECARGAR
        if (Input.IsActionJustPressed("reload"))
        {
            Reload();
        }
    }

    // RECIBIR DAÑO
    private void GetHurt()
    {
        if (!canBeDamaged)
            return;

        canBeDamaged = false;

        GD.Print("aaaaa  " + currentShields + " " + currentLives);

        invulnerabilityTimer.Start();

        shieldTimer.Stop();

        if (currentShields > 0)
        {
            currentShields--;
        }
        
        else
        {
            currentLives--;

            if (currentLives <= 0)
            {
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
    }

    // FIN INVULNERABILIDAD
    private void EndInvulnerability()
    {
        canBeDamaged = true;

        GD.Print(
            "[DEBUG] Fin de la invulnerabilidad."
        );
    }

    // REGENERAR ESCUDO
    private void RegenerateShield()
    {
        currentShields = maxShields;

        GD.Print("[DEBUG] ¡ESCUDO REGENERADO!");
    }

    // ATAQUE
    private void Atack()
    {
        // COMPROBAR SI ESTÁ RECARGANDO
        if (reloadSound.Playing)
        {
            return;
        }

        // SIN MUNICIÓN
        if (currentAmmo <= 0)
        {
            reloadSound.Play();

            GD.Print("[DEBUG] ¡No quedan balas!");

            Reload();

            return;
        }

        // CREAR BALA
        Node3D bulletInstance =
            _bulletScene.Instantiate<Node3D>();

        GetParent().AddChild(bulletInstance);

        // POSICIÓN DE SALIDA
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

        // ROTAR LA BALA
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

        // GASTAR BALA
        currentAmmo--;

        GD.Print(
            $"[DEBUG] Balas restantes: {currentAmmo}/{maxAmmo}"
        );

        gunShotSound.Play();

        // CARGADOR VACÍO
        if (currentAmmo == 0)
        {
            GD.Print("[DEBUG] ¡Cargador vacío!");
        }
    }

    private void Reload()
    {
        if (currentAmmo >= maxAmmo)
        {
            return;
        }

        currentAmmo = maxAmmo;

        GD.Print("[DEBUG] ¡ARMA RECARGADA!");
        reloadSound.Play();
    }
}