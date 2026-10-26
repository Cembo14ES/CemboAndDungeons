using Godot;

public partial class BaseCharacter : CharacterBody3D
{
    [Export] public CharacterStats Stats { get; set; }
    [Export] public Timer InvulnerabilityTimer { get; set; }
    [Export] public BaseAttackDamage Hitbox { get; set; }
    [Export] public StringName HostileGroup { get; set; }   // a quién ataca (Enemies en el jugador, Player en enemigos)

    public bool CanBeDamaged { get; private set; } = true;
    public Vector3 Facing { get; private set; } = Vector3.Back; 

    // Los hijos sobrescriben estos métodos
    protected virtual Vector3 GetMoveDirection() => Vector3.Zero;
    protected virtual bool WantsToAttack() => false; // el hijo decide si quiere atacar

    public override void _Ready()
    {
        if (Stats == null)
        {
            GD.PushError($"{Name}: no tiene Stats asignado.");
            SetPhysicsProcess(false);
            return;
        }

        // Copia propia, para no modificar el .tres ni afectar a otros personajes
        Stats = (CharacterStats)Stats.Duplicate(true);
        Stats.Health = Stats.MaxHealth; //en un futuro se tiene que cambiar, por el guardado de la partida

        if (InvulnerabilityTimer != null)
        {
            InvulnerabilityTimer.WaitTime = Mathf.Max(Stats.InvulnerabilityTimerStat, 0.05f);
            InvulnerabilityTimer.OneShot = true;
            InvulnerabilityTimer.Timeout += () => CanBeDamaged = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 direction = GetMoveDirection();
        if (direction != Vector3.Zero)
        {
            Facing = direction;   
        }
        Vector3 velocity = Velocity;

        velocity.X = direction.X * Stats.Speed;
        velocity.Z = direction.Z * Stats.Speed;

        Velocity = velocity;
        MoveAndSlide();

        if (WantsToAttack()) //Metodo del hijo que indica si quiere atacar
        {
            Attack();
        }           

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider() is Node collider)
                OnCollided(collider);
        }
    }

    protected virtual void OnCollided(Node collider) //...
    {
        
    }

    protected virtual void Attack() //revisions
    {
        Hitbox?.StartAtack(Facing);   // él mismo comprueba CanAtack
        /* es lo mismo que:
		if (Hitbox != null)
		{
    		Hitbox?.StartAtack(); 
		}
		Lo de abajo lo mismo*/
    }

    public void TakeDamage(float incomingDamage)
    {
        if (!CanBeDamaged) return;

        float damage = Mathf.Max(0f, incomingDamage - Stats.Defense);
        Stats.Health -= damage;
        GD.Print($"[DAÑO] {Name} recibe {damage}. Vida: {Stats.Health}/{Stats.MaxHealth}");

        if (Stats.Health <= 0)
        {
            Die();
            return;
        }

        if (InvulnerabilityTimer != null)
        {
            CanBeDamaged = false;
            InvulnerabilityTimer.Start();
        }
    }

    protected virtual void Die() //revisar
    {
        SetPhysicsProcess(false);
        QueueFree();
    }
}

