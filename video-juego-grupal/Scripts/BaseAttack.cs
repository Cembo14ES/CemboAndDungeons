using Godot;
using System.Collections.Generic;

public partial class BaseAttack : Area3D
{
    protected BaseCharacter owner;
    protected AttackStats stats;
    protected Vector3 direction;

    private double hitboxFrames;
    private double attackTime;
    private readonly HashSet<BaseCharacter> alreadyHit = new();

    // Los hijos sobrescriben esto: colocación y orientación
    protected virtual void AimAt(Vector3 aimDirection) { }

    // La llama el personaje justo después de Instantiate y ANTES de AddChild
    public void Setup(BaseCharacter newOwner, AttackStats newStats, Vector3 newDirection)
    {
        owner = newOwner;
        stats = newStats;
        direction = newDirection;
        hitboxFrames = newStats.HitboxFramesStat;
        attackTime = Mathf.Max(newStats.AttackTimeStat, 0.05f);
    }

    public override void _Ready()
    {
        if (owner == null || stats == null)
        {
            GD.PushError($"{Name}: se instanció sin llamar a Setup.");
            QueueFree();
            return;
        }

        // Este área detecta, pero nadie la detecta a ella:
        // así un ataque no puede golpear a otro ataque (ni a su dueño a través de él).
        Monitorable = false;

        AimAt(direction);   // ya está en el árbol, GlobalPosition funciona
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(owner))
        {
            QueueFree();
            return;
        }

        if (stats.HitboxAlwaysActive)
        {
            // Hitbox permanente: cada AttackTimeStat se "olvidan" los golpeados
            // para poder volver a dañar a quien siga dentro.
            attackTime -= delta;
            if (attackTime <= 0)
            {
                alreadyHit.Clear();
                attackTime = Mathf.Max(stats.AttackTimeStat, 0.05f);
            }
        }
        else
        {
            hitboxFrames -= delta;
            if (hitboxFrames <= 0)
            {
                QueueFree();   // pasó el tiempo: el ataque desaparece
                return;
            }
        }

        foreach (Area3D area in GetOverlappingAreas())
        {
            if (area is BaseAttack) continue;   // ignora otros ataques
            if (area.GetParent() is not BaseCharacter target) continue;
            if (target == owner) continue;
            if (!alreadyHit.Add(target)) continue;

            target.TakeDamage(owner.Stats.Damage * stats.Multiplier);
        }
    }
}