using System;
using Godot;

public partial class Attack_Melee_Basic : BaseAttack
{
    [Export] public float distanceFromPlayer { get; set; } = 2f; //La distancia del centro del jugador a la que spawnea el ataque.
    [Signal] public delegate void Signal_ReturnToNormalityBasicEventHandler();

    /// <summary>
    /// Realiza calculos para que el ataque spawnee adelante del jugador, en las 8 direcciones disponibles.
    /// </summary>
    /// 
    public override void _Ready()
    {
        base._Ready();
    }
    private void onFinish()
    {
        EmitSignal(SignalName.Signal_ReturnToNormalityBasic);
        Die();
    }
    protected override void PlaceAttack(Vector3 attackDirection, Vector3 attackPosition)
    {
        if (attackDirection == Vector3.Zero) attackDirection = Vector3.Back;

        float angle = -(attackDirection.Y + Mathf.DegToRad(180));

        Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distanceFromPlayer;
        GlobalPosition = attackPosition + offset;
        Rotation = -attackDirection;
    }
    protected override void InitializeLifetimeTimers()
    {
        if (stats.AttackAlwaysActive)
            return;

        InitializeTimer(attackTimer, stats.AttackNodeAliveTimeStat, true, onFinish);
        attackTimer.Start();

        InitializeTimer(hitboxTimer, stats.HitboxNodeAliveTimeStat, true, () => hitboxAreaShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, true));
        hitboxTimer.Start();
    }
}

