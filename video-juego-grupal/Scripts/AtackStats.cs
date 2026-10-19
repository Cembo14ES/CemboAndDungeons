using Godot;
using System;

[GlobalClass]
public partial class AtackStats : Resource
{
    [Export] public float Attack { get; set; }
    [Export] public float Multiplier { get; set; }
    [Export] public float ProjectileSpeed { get; set; }
    [Export] public float Force { get; set; }
    [Export] public float AtackTimeStat { get; set; } //tiempo que dura el ataque
    [Export] public float HitboxFramesStat { get; set; } //tiempo de la hitbox
    public float FinalDamage => Attack * Multiplier;
}
