using Godot;
using System;

[GlobalClass]
public partial class AttackStats : Resource
{
    [Export] public float Damage { get; set; } //Daño al ataque
    [Export] public float Multiplier { get; set; } //Multiplicador de daño
    [Export] public float ProjectileSpeed { get; set; } //Velocidad del proyectil
    [Export] public float Force { get; set; } //Empuje de ataque
    [Export] public float AttackTimeStat { get; set; } //Tiempo que dura el ataque
    [Export] public float HitboxFramesStat { get; set; } //Tiempo de la hitbox
    [Export] public AttackGroup attackGroup { get; set; } //Grupo perteneciente del ataque
    [Export] public bool HitboxAlwaysActive { get; set; }
    public enum AttackGroup {Player = 0, Enemy = 1, EnemyFriendlyFire = 2}
    public float FinalDamage => Damage * Multiplier;
}
