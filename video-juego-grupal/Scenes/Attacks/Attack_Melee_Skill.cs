using Godot;
using System;

public partial class Attack_Melee_Skill : BaseAttack
{
	// Called when the node enters the scene tree for the first time.
    [Export] public bool followOwner { get; set; } = true;     // si el ataque sigue al jugador mientras gira
    [Export] public bool clockwise { get; set; } = true;       // sentido del giro
    [Export] public float turns { get; set; } = 1f;            // vueltas completas (1 = 360°)
	[Export] public float distanceFromPlayer { get; set; } = 2f;

    private float spinSpeed;   // radianes por segundo

    /// <summary>
    /// Spawnea el ataque centrado en el jugador y calcula la velocidad de giro.
    /// </summary>
	/// 
    public override void _Ready()
    {
        base._Ready();

        // Aleja la hitbox del centro; el nodo raíz queda sobre el jugador
        if (hitboxArea != null)
            hitboxArea.Position = new Vector3(0, 0, distanceFromPlayer);
    }

	protected override void PlaceAttack(Vector3 attackDirection, Vector3 attackPosition)
    {
        GlobalPosition = attackPosition;
        Rotation = new Vector3(0, -attackDirection.Y + Mathf.Pi, 0);  // empieza mirando hacia donde mira el jugador

        // Una vuelta completa durante el tiempo de vida del ataque
        float duration = Mathf.Max(stats.AttackNodeAliveTimeStat, 0.05f);
        spinSpeed = Mathf.Tau * turns / duration;
        if (clockwise) spinSpeed = -spinSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);   // aquí se aplica el daño

        if (followOwner && IsInstanceValid(owner))
            GlobalPosition = owner.GlobalPosition;

        RotateY(spinSpeed * (float)delta);
    }
}
