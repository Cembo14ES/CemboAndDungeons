using Godot;
using System;

[GlobalClass]
public partial class CharacterStats : Resource
{
	// Called when the node enters the scene tree for the first time.
	[Export] public float MaxHealth { get; set; } //Maxima vida del personaje
	[Export] public float Damage { get; set; } //Daño de ataque
	[Export] public float Defense { get; set; } //Reduccion de daño
	[Export] public float MoveSpeed { get; set; } //Velocidad de movimiento
	[Export] public float AttackSpeed { get; set; } //Velocidad de ataque
	[Export] public float Knockback { get; set; } //Fuerza de Aguante de furza
	[Export] public float InvulnerabilityTimerStat { get; set; } //Tiempo de invulnerabilidad
	//[Export] public AtackStats BasicAttack { get; set; }
}
