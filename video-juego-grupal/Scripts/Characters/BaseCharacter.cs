using System;
using Godot;

public partial class BaseCharacter : CharacterBody3D
{
    [Export] public CharacterStats stats { get; set; } // Las estadisticas del personaje.
    protected CharacterStats originalStats; // Copia de las estadisticas que guarda los valores originales.
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
    [Export] AnimatedSprite3D characterAnimation = new AnimatedSprite3D(); // Animacion del personaje.

    public override void _Ready()
    {  
        CollisionLayer = 0;
        CollisionMask = 0;

        SetCollisionLayerValue(2, true);  //Asigna a el personaje con la colision tipo "Character"
        SetCollisionMaskValue(1, true); //Asigna que puede interactuar con collisiones tipo "World"

        //Realiza comprobaciones de varias variables para ver si estan inicializadas.
        if (stats == null)
        {
            GD.PushError($"{Name}: No tiene Stats asignado.");
            SetPhysicsProcess(false);
            return;
        }

        originalStats = stats;

        currentHealth = stats.MaxHealth;

        //Inicializa los Timers.
        AddChild(invulnerabilityTimer);
        invulnerabilityTimer.WaitTime = Mathf.Max(stats.InvulnerabilityTimerStat, 0.05f);
        invulnerabilityTimer.OneShot = true;
        invulnerabilityTimer.Timeout += () => canBeDamaged = true;
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
        velocity.X = direction.X * stats.MovementSpeed;
        velocity.Z = direction.Z * stats.MovementSpeed;
        Velocity = velocity;
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

        BoxShape3D shape = (BoxShape3D) damageTriggerShape.Shape;
        Vector3 size = new Vector3((float) Math.Log(shape.Size.X), 0, (float) Math.Log(shape.Size.Z));
        
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
}