using Godot;
using System;

[GlobalClass]
public partial class AtackStats : Resource
{
    public float Attack { get; set; }
    [Export] public float Multiplier { get; set; }
    [Export] public float ProjectileSpeed { get; set; }
    [Export] public float Force { get; set; }
    [Export] public float Lifetime { get; set; }
    [Export] public int HitboxFrames { get; set; }

    public float FinalDamage => Attack * Multiplier;
}
