using Godot;
using System;

[GlobalClass]
public partial class CharacterStats : Resource
{
	// Called when the node enters the scene tree for the first time.
	[Export] public float MaxHealth { get; set; }
	[Export] public float Health { get; set; }
	[Export] public float Attack { get; set; }
	[Export] public float Defense { get; set; }
	[Export] public float Speed { get; set; }
	[Export] public float AttackSpeed { get; set; }
	[Export] public float Knockback { get; set; }
	[Export] public float InvulnerabilityTimer { get; set; }
	[Export] public bool CanBeDamaged { get; set; }
	[Export] public AtackStats BasicAttack { get; set; }
}
