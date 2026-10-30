using Godot;
 
public partial class BaseCharacter : CharacterBody3D
{
    [Export] public CharacterStats Stats { get; set; }
    [Export] public Timer InvulnerabilityTimer { get; set; }
    [Export] public PackedScene AttackScene { get; set; }   // el .tscn del ataque
    [Export] public AttackStats AttackStats { get; set; }   // el .tres del ataque
    [Export] public StringName HostileGroup { get; set; }   // Enemies en el jugador, Player en enemigos
 
    public float Health { get; protected set; } //Vida del personaje
    public bool CanBeDamaged { get; private set; } = true;
    public bool CanAttack => attackCooldownLeft <= 0;
    public Vector3 Facing { get; private set; } = Vector3.Back;
 
    private double attackCooldownLeft;
    private bool hasPersistentAttack;
 
    protected virtual Vector3 GetMoveDirection() => Vector3.Zero;
    protected virtual bool WantsToAttack() => false;
    protected virtual void OnCollided(Node collider) { }
 
    public override void _Ready()
    {
        if (Stats == null)
        {
            GD.PushError($"{Name}: no tiene Stats asignado.");
            SetPhysicsProcess(false);
            return;
        }
 
        Stats = (CharacterStats)Stats.Duplicate(true);
        Health = Stats.MaxHealth;
 
        if (InvulnerabilityTimer != null)
        {
            InvulnerabilityTimer.WaitTime = Mathf.Max(Stats.InvulnerabilityTimerStat, 0.05f);
            InvulnerabilityTimer.OneShot = true;
            InvulnerabilityTimer.Timeout += () => CanBeDamaged = true;
        }
 
        SpawnPersistentAttackIfNeeded();
    }
 
    // Si el ataque es AlwaysActive, se crea UNA vez como hijo del personaje
    // (así se mueve con él) y ya no se usa Attack() para este personaje.
    private void SpawnPersistentAttackIfNeeded()
    {
        if (AttackScene == null || AttackStats == null) return;
 
        if (!AttackStats.HitboxAlwaysActive) return;   // ataque normal: se crea al atacar

        BaseAttack attack = AttackScene.Instantiate<BaseAttack>();
        attack.Setup(this, AttackStats, Facing);
        AddChild(attack);    // hijo del personaje; muere con él
        hasPersistentAttack = true;
    }
 
    public override void _PhysicsProcess(double delta)
    {
        Vector3 direction = GetMoveDirection();
        if (direction != Vector3.Zero)
            Facing = direction;
 
        Vector3 velocity = Velocity;
        velocity.X = direction.X * Stats.MoveSpeed;
        velocity.Z = direction.Z * Stats.MoveSpeed;
        Velocity = velocity;
        MoveAndSlide();
 
        if (attackCooldownLeft > 0)
            attackCooldownLeft -= delta;
 
        if (!hasPersistentAttack && CanAttack && WantsToAttack())
            Attack();
 
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider() is Node collider)
                OnCollided(collider);
        }
    }
 
    protected virtual void Attack()
    {
        if (AttackScene == null || AttackStats == null) return;
 
        BaseAttack attack = AttackScene.Instantiate<BaseAttack>();
        attack.Setup(this, AttackStats, Facing);
 
        // En la escena actual, para que no se mueva con el personaje
        AddChild(attack); 
 
        attackCooldownLeft = AttackStats.AttackTimeStat;
    }
 
    public void TakeDamage(float incomingDamage)
    {
        if (!CanBeDamaged || Health <= 0) return;
 
        float damage = Mathf.Max(0f, incomingDamage - Stats.Defense);
        Health -= damage;
        GD.Print($"[DAÑO] {Name} recibe {damage}. Vida: {Health}/{Stats.MaxHealth}");
 
        if (Health <= 0)
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
 
    protected virtual void Die()
    {
        SetPhysicsProcess(false);
        QueueFree();
    }
}