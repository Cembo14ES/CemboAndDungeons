using Godot;
using System;

public partial class BaseAttackDamage : Area3D
{
    [Export] public AtackStats Stats { get; set; }
    [Export] public Sprite3D Sprite { get; set; }

    public bool CanAtack { get; private set; } = true;

    protected BaseCharacter owner;
    private Timer AtackTime;
    private Timer HitboxTime;

    protected virtual void AimAt(Vector3 direction) { }
    protected virtual bool AlwaysActive => false;
    public override void _Ready()
    {
        owner = GetParent() as BaseCharacter;
        if (owner == null)
            GD.PushError($"{Name}: debe ser hijo directo de un BaseCharacter.");

        Monitoring = AlwaysActive;
        BodyEntered += OnBodyEntered;

        if (Stats == null)
        {
            GD.PushError($"{Name}: no tiene Stats asignado.");
            return;
        }

        Stats = (AtackStats)Stats.Duplicate(true);

        if (Stats.HitboxFramesStat > Stats.AtackTimeStat)
        {
            GD.PushWarning($"{Name}: la hitbox dura más que el ataque. Se limita a {Stats.AtackTimeStat}.");
            Stats.HitboxFramesStat = Stats.AtackTimeStat;
        }

        AtackTime = new Timer { WaitTime = Mathf.Max(Stats.AtackTimeStat, 0.05f), OneShot = true };
        AtackTime.Timeout += () => CanAtack = true;
        AddChild(AtackTime);

        HitboxTime = new Timer { WaitTime = Mathf.Max(Stats.HitboxFramesStat, 0.05f), OneShot = true };
        HitboxTime.Timeout += () =>
        {
            if (!AlwaysActive)
                SetDeferred(Area3D.PropertyName.Monitoring, false);
        };
        AddChild(HitboxTime);
    }
    public void StartAtack(Vector3 direction)
    {
        if (!CanAtack || Stats == null) return;

        AimAt(direction); // lo decide el hijo

        if (AlwaysActive) return;

        CanAtack = false;
        AtackTime.Start();
        Monitoring = true;
        HitboxTime.Start();
    }
    protected virtual void OnBodyEntered(Node3D body)
    {
        if (owner == null || Stats == null) return;
        if (body == owner) return;
        if (body is not BaseCharacter target) return;
        // if (!target.IsInGroup(owner.HostileGroup)) return;

        if (AlwaysActive)
        {
            TryContactHit(target); // golpe al entrar, respetando el cooldown
            return;
        }

        target.TakeDamage(owner.Stats.Attack * Stats.Multiplier);
    }
    private void TryContactHit(BaseCharacter target)
    {
        if (!CanAtack || AtackTime == null) return;

        target.TakeDamage(owner.Stats.Attack * Stats.Multiplier);

        CanAtack = false;
        AtackTime.Start();
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Sprite != null)
        {
            Sprite.Visible = Monitoring;
        }

        if (AlwaysActive)
        {
            ApplyContinuousDamage();
        }
    }
    private void ApplyContinuousDamage()
    {
        if (!CanAtack || owner == null || Stats == null || AtackTime == null) return;

        foreach (var body in GetOverlappingBodies())
        {
            if (body == owner) continue;
            if (body is not BaseCharacter target) continue;
            // if (!target.IsInGroup(owner.HostileGroup)) continue;

            TryContactHit(target); // si sigue dentro, vuelve a golpear al acabar el cooldown
            break;
        }
    }
}