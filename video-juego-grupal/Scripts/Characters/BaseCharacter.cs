using System;
using Godot;

public partial class BaseCharacter : CharacterBody3D
{
    [Export] public CharacterStats originalStats { get; set; } // Las estadisticas del personaje.
    protected CharacterStats stats; // Copia de las estadisticas que guarda los valores originales.
    [Export] public EnumCharacterType characterType { get; set; } // El Tipo de entidad que es.
    public enum EnumCharacterType { Player = 0, Enemy = 1 }

    [Export] public PackedScene damageNumber;
    [Export] public Area3D damageTrigger;
    [Export] public CollisionShape3D damageTriggerShape;

    public float currentHealth { get; protected set; } // Vida actual del personaje.
    public bool canBeDamaged { get; protected set; } = true; // Booleano que determina si el personaje esta en un estado de invulnerabilidad o no.
    public Vector3 characterFacingDirection { get; protected set; } = Vector3.Back; // Direccion en la que mira el personaje.

    protected Timer invulnerabilityTimer = new Timer(); //Timer para la invulnerabilidad del personaje.

    protected virtual Vector3 GetMovementDirection() => Vector3.Zero; //Los personajes lo sobreescriben para determinar la direccion de movimiento.
    protected virtual void OnCollided(Node collider) { }
    [Export] protected AnimatedSprite3D characterAnimation = new AnimatedSprite3D(); // Animacion del personaje.

    public override void _Ready()
    {
        CheckExportedValues();
        stats = (CharacterStats)originalStats.Duplicate();
        CheckStatsValues();

        CollisionLayer = 0;
        CollisionMask = 0;

        SetCollisionLayerValue(2, true);  //Asigna a el personaje con la colision tipo "Character"
        SetCollisionMaskValue(1, true); //Asigna que puede interactuar con collisiones tipo "World"

        currentHealth = stats.MaxHealth;

        InitializeTimer(invulnerabilityTimer, stats.InvulnerabilityTime, true, () => canBeDamaged = true);
    }

    

    /// <summary>
    /// Inicializa un Timer. Lo añade a la escena y inicializa sus valores que siempre necesitan ser asignados.
    /// </summary>
    /// <param name="timer">El Timer a ser inicializado.</param>
    /// <param name="waitTime">El tiempo de duracion del timer en segundos.</param>
    /// <param name="oneshot">Si el Timer se ejecuta una sola vez, o si se reinicia cada vez que se acaba.</param>
    /// <param name="onTimeout">La accion que se debe de realizar cuando el Timer termina.</param>
    protected virtual void InitializeTimer(Timer timer, double waitTime, bool oneshot, Action onTimeout)
    {
        AddChild(timer);
        timer.WaitTime = waitTime;
        timer.OneShot = oneshot;
        timer.Timeout += onTimeout;
    }

    public override void _PhysicsProcess(double delta)
    {
        Movement(GetMovementDirection(), stats.MovementSpeed);

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider() is Node collider)
                OnCollided(collider);
        }
    }

    /// <summary>
    /// Realiza el movimiento del personaje. Utiliza el valor de GetMovementDirection(). Los hijos pueden sobreescribirlo.
    /// </summary>
    protected virtual void Movement(Vector3 direction, float speed)
    {
        if (direction != Vector3.Zero)
        {
            characterFacingDirection = direction;
            PlayAnimation(GetWalkAnimation(direction));
        }
        else
        {
            characterAnimation.Play("Idle");
        }


        Velocity = new Vector3(direction.X * speed, Velocity.Y, direction.Z * speed);

        MoveAndSlide();
    }
    protected void PlayAnimation(string animationName)
    {
        if (characterAnimation == null || characterAnimation.SpriteFrames == null) return;
        // Evita reiniciar la animación en cada frame
        if (characterAnimation.Animation != animationName)
        {
            characterAnimation.Play(animationName);
        }
    }
    protected string GetWalkAnimation(Vector3 direction)
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
    /// Realiza los procesos de recivir daño del personaje. Los hijo pueden sobreescribirlo.
    /// </summary>
    /// <param name="incomingDamage">El daño recivido</param>
    public void TakeDamage(float incomingDamage, float incomingAttackDirection)
    {
        if (!canBeDamaged || currentHealth <= 0) return;

        float damage = Mathf.Max(0f, incomingDamage - stats.Defense);
        currentHealth -= damage;
        GD.Print($"[DAÑO] {Name} recibe {damage}. Vida: {currentHealth}/{stats.MaxHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        canBeDamaged = false;
        invulnerabilityTimer.Start();

        DamageNumber damageUI = damageNumber.Instantiate<DamageNumber>();

        BoxShape3D shape = (BoxShape3D)damageTriggerShape.Shape;
        Vector3 size = new Vector3((float)Math.Log(shape.Size.X), 0, (float)Math.Log(shape.Size.Z));

        damageUI.Setup(damage, incomingAttackDirection, size * 5);
        AddChild(damageUI);
    }

    /// <summary>
    /// Muelto. Los hijo pueden sobreescribirlo.
    /// </summary>
    protected virtual void Die()
    {
        SetPhysicsProcess(false);
        QueueFree();
    }

    /// <summary>
    /// Comprueba los variables Exportados para ver si estan asignados, y pushea un Warning sino.
    /// Los hijos debed sobreescribir el metodo, añadiendo sus propias variables Exportadas.
    /// </summary>
    protected virtual void CheckExportedValues()
    {
        GD.Print($"{Name}: CharacterType: {characterType}");
        if (originalStats == null)
            GD.PushWarning($"{Name}: No tiene Stats asignado.");
        if (damageNumber == null)
            GD.PushWarning($"{Name}: No tiene Damage Number asignado.");
        if (damageTrigger == null)
            GD.PushWarning($"{Name}: No tiene Damage Trigger asignado.");
        if (damageTriggerShape == null)
            GD.PushWarning($"{Name}: No tiene Damage Trigger Shape asignado.");
    }

    /// <summary>
    /// Comprueba los valores de las estadisticas del personaje para ver si son validas.
    /// Asigna valores por defecto en caso de que haya alguna estadistica no valida.
    /// </summary>
    protected virtual void CheckStatsValues()
    {
        if (stats.MaxHealth < 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de MaxHealth es menor a 0.");
            stats.MaxHealth = 10;
        }
        if (stats.Damage < 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de Damage es menor a 0.");
            stats.Damage = 0;
        }
        if (stats.Defense < 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de Defense es menor a 0.");
            stats.Defense = 0;
        }
        if (stats.MovementSpeed < 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de MovementSpeed es menor a 0.");
            stats.MovementSpeed = 0;
        }
        if (stats.MovementSpeedAcceleration < 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de MovementSpeedAcceleration es menor a 0.");
            stats.MovementSpeedAcceleration = 0;
        }
        if (stats.RegularAttackCooldown <= 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de RegularAttackCooldown es menor o igual a 0.");
            stats.RegularAttackCooldown = 0.1f;
        }
        if (stats.SkillAttackCooldown <= 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de SkillAttackCooldown es menor o igual a 0.");
            stats.SkillAttackCooldown = 0.1f;
        }
        if (stats.movementSkillCooldown <= 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de movementSkillCooldown es menor o igual a 0.");
            stats.movementSkillCooldown = 0.1f;
        }
        if (stats.UltimateAttackCooldown <= 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de UltimateAttackCooldown es menor o igual a 0.");
            stats.UltimateAttackCooldown = 0.1f;
        }
        if (stats.InvulnerabilityTime <= 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de InvulnerabilityTime es menor o igual a 0.");
            stats.InvulnerabilityTime = 0.1f;
        }
    }
}