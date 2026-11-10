using Godot;
using System;

[GlobalClass]
public partial class AttackStats : Resource
{
    public float AttackDamage { get; set; } //Daño al ataque (Viene de la estadistica del jugador).
    [Export] public float AttackMultiplier { get; set; } //Multiplicador de daño.
    [Export] public float AttackNodeAliveTimeStat { get; set; } //Tiempo que dura el ataque (en segundos).
    [Export] public float HitboxNodeAliveTimeStat { get; set; } //Tiempo de la hitbox (en segundos).
    [Export] public bool AttackAlwaysActive { get; set; } // Determina si el ataque dura permanentemente o no
    [Export] public EnumAttackTarget attackTarget { get; set; } //El objetivo de el ataque.
    [Export] public float projectileSpeed { get; set; } //Determina si el ataque es un proyectil, y su velocidad.
    
    public enum EnumAttackTarget {Player = 0, Enemy = 1, All = 2}
    public float FinalDamage => AttackDamage * AttackMultiplier;
}
