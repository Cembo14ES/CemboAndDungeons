using Godot;

[GlobalClass]
public partial class CharacterStats : Resource
{
	// Called when the node enters the scene tree for the first time.
	[Export] public float MaxHealth { get; set; } //Maxima vida del personaje
	[Export] public float Damage { get; set; } //Daño de ataque
	[Export] public float Defense { get; set; } //Reduccion de daño
	[Export] public float MovementSpeed { get; set; } //Velocidad de movimiento
	[Export] public float MovementSpeedAcceleration { get; set; } //Velocidad de movimiento (hay que implementarlo)
    [Export] public float RegularAttackCooldown { get; set; } //El cooldown de uso del ataque.
	[Export] public float SkillAttackCooldown { get; set; } //El cooldown de uso del ataque.
	[Export] public float UltimateAttackCooldown { get; set; } //El cooldown de uso del ataque.
	[Export] public float PushResistance { get; set; } //Aguante del empuje de los ataques.
	[Export] public float InvulnerabilityTimerStat { get; set; } //Tiempo de invulnerabilidad
}
