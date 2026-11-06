using Godot;

public partial class BaseCharacter : CharacterBody3D
{
    [Export] public CharacterStats Stats { get; set; } // Las estadisticas del personaje.

    [Export] public PackedScene RegularAttackScene { get; set; } // La Escena (.tscn) del ataque Normal.
    [Export] public EnumCharacterType characterType { get; set; } // El Tipo de entidad que es.
    public enum EnumCharacterType {Player = 0, Enemy = 1}

    public float currentHealth { get; protected set; } // Vida actual del personaje.
    public bool canBeDamaged { get; private set; } = true; // Booleano que determina si el personaje esta en un estado de invulnerabilidad o no.
    public bool canRegularAttack { get; private set; } = true; // Booleano que determina si el personaje puede atacar.
    public Vector3 characterFacingDirection { get; private set; } = Vector3.Back; // Direccion en la que mira el personaje.

    private Timer invulnerabilityTimer = new Timer(); //Timer para la invulnerabilidad del personaje.
    private Timer regularAttackCooldownTimer = new Timer(); //Timer para el cooldown del ataque.

    protected virtual Vector3 GetMovementDirection() => Vector3.Zero; //Los personajes lo sobreescriben para determinar la direccion de movimiento.
    protected virtual void OnCollided(Node collider) { }
    [Export] AnimatedSprite3D characterAnimation { get; set; } // Animacion del personaje.

    public override void _Ready()
    {
        //Realiza comprobaciones de varias variables para ver si estan inicializadas.
        if (Stats == null)
        {
            GD.PushError($"{Name}: No tiene Stats asignado.");
            SetPhysicsProcess(false);
            return;
        }

        if (RegularAttackScene == null)
        {
            GD.PushError($"{Name}: No tiene Regular Attack asignado.");
            SetPhysicsProcess(false);
            return;
        }

        currentHealth = Stats.MaxHealth;

        //Inicializa los Timers.
        AddChild(invulnerabilityTimer);
        invulnerabilityTimer.WaitTime = Mathf.Max(Stats.InvulnerabilityTimerStat, 0.05f);
        invulnerabilityTimer.OneShot = true;
        invulnerabilityTimer.Timeout += () => canBeDamaged = true;

        AddChild(regularAttackCooldownTimer);
        regularAttackCooldownTimer.WaitTime = Stats.RegularAttackCooldown;
        regularAttackCooldownTimer.OneShot = true;
        regularAttackCooldownTimer.Timeout += () => canRegularAttack = true;
    }


    public override void _PhysicsProcess(double delta)
    {
        Movement();

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider() is Node collider)
                OnCollided(collider);
        }
    }

    /// <summary>
    /// Realiza el movimiento del personaje. Utiliza el valor de GetMovementDirection(). Los hijos pueden sobreescribirlo.
    /// </summary>
    protected virtual void Movement()
    {
        Vector3 direction = GetMovementDirection();
        if (direction != Vector3.Zero)
        {
            characterFacingDirection = direction;
            PlayAnimation(GetWalkAnimation(direction));
        }
        else
        {
            characterAnimation.Play("Idle");
        }

        Vector3 velocity = Velocity;
        velocity.X = direction.X * Stats.MovementSpeed;
        velocity.Z = direction.Z * Stats.MovementSpeed;
        Velocity = velocity;
        MoveAndSlide();
    }
    private void PlayAnimation(string animationName)
    {
        if (characterAnimation == null || characterAnimation.SpriteFrames == null) return;
        // Evita reiniciar la animación en cada frame
        if (characterAnimation.Animation != animationName)
        {
            characterAnimation.Play(animationName);
        }
    }
    private string GetWalkAnimation(Vector3 direction)
    {
        // Animacion izquierda o/y derecha
        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Z))
        {
            return direction.X > 0 ? "Walking_Right" : "Walking_Left";
        }
        // Animacion arriba o/y abajo
        return direction.Z < 0 ? "Walking_Down" : "Walking_Up";
    }

    /// <summary>
    /// Realiza un ataque nomral. Los hijo pueden sobreescribirlo.
    /// </summary>
    protected virtual void RegularAttack()
    {
        BaseAttack attack = RegularAttackScene.Instantiate<BaseAttack>();
        attack.Setup(this, characterFacingDirection, Position);
        AddChild(attack);

        canRegularAttack = false;
        regularAttackCooldownTimer.Start();
    }

    /// <summary>
    /// Realiza los procesos de recivir daño del personaje. Los hijo pueden sobreescribirlo.
    /// </summary>
    /// <param name="incomingDamage">El daño recivido</param>
    public void TakeDamage(float incomingDamage)
    {
        if (!canBeDamaged || currentHealth <= 0) return;

        float damage = Mathf.Max(0f, incomingDamage - Stats.Defense);
        currentHealth -= damage;
        GD.Print($"[DAÑO] {Name} recibe {damage}. Vida: {currentHealth}/{Stats.MaxHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        canBeDamaged = false;
        invulnerabilityTimer.Start();

    }

    /// <summary>
    /// Muelto. Los hijo pueden sobreescribirlo.
    /// </summary>
    protected virtual void Die()
    {
        SetPhysicsProcess(false);
        QueueFree();
    }
}