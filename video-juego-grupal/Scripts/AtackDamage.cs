using Godot;

public partial class AtackDamage : Area3D
{
    [Export] public AtackStats Stats { get; set; }
    [Export] public Timer AtackTime { get; set; }
    [Export] public Timer HitboxTime { get; set; }
	[Export] public Sprite3D Sprite { get; set; } 
    public bool CanAtack { get; private set; } = true;
    private BaseCharacter owner;

    public override void _Ready()
    {
        owner = GetParent() as BaseCharacter;
        if (owner == null)
            GD.PushError($"{Name}: debe ser hijo directo de un BaseCharacter.");

        Monitoring = false;               // apagada hasta atacar
        BodyEntered += OnBodyEntered;

        if (Stats == null)
        {
            GD.PushError($"{Name}: no tiene Stats asignado.");
            return;
        }
		AtackTime = new Timer();
		AddChild(AtackTime);
		HitboxTime = new Timer();
		AddChild(HitboxTime);
        Stats = (AtackStats)Stats.Duplicate(true);

        if (Stats.HitboxFramesStat > Stats.AtackTimeStat)
        {
            GD.PushWarning($"{Name}: la hitbox dura más que el ataque. Se limita a {Stats.AtackTimeStat}.");
            Stats.HitboxFramesStat = Stats.AtackTimeStat;
        }

        if (AtackTime != null)
        {
            AtackTime.WaitTime = Mathf.Max(Stats.AtackTimeStat, 0.05f);
            AtackTime.OneShot = true;
            AtackTime.Timeout += () => CanAtack = true;
        }

        if (HitboxTime != null)
        {
            HitboxTime.WaitTime = Mathf.Max(Stats.HitboxFramesStat, 0.05f);
            HitboxTime.OneShot = true;
            HitboxTime.Timeout += () => SetDeferred(Area3D.PropertyName.Monitoring, false);
        }
    }

    public void StartAtack(Vector3 direction)
    {
        if (!CanAtack || Stats == null) return;
        
        AimAt(direction);

        CanAtack = false;
        AtackTime?.Start(); 
        Monitoring = true;
        HitboxTime?.Start();
		GD.Print("atacando");
        /* es lo mismo que:
		if (AtackTime != null)
		{
    		AtackTime.Start();
		}
		Lo de abajo lo mismo*/
    }
    private void AimAt(Vector3 direction) //Hay que ajustarlas mejor, pero por el momento lo dejo así
    {
        direction.Y = 0;
        if (direction == Vector3.Zero) return;

        float step = Mathf.Pi / 4f;
        float angle = Mathf.Round(Mathf.Atan2(direction.X, direction.Z) / step) * step;

        Position = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 2;

        Rotation = new Vector3(0, angle, 0);
    }

    private void OnBodyEntered(Node3D body) //filtros para que solo haga daño a lo que le tiene que hacer daño
    {
		GD.Print("body entered");
        if (owner == null || Stats == null) return;
        if (body == owner) return;
        if (body is not BaseCharacter target) return;
        //if (!target.IsInGroup(owner.HostileGroup)) return;

        target.TakeDamage(owner.Stats.Attack * Stats.Multiplier);
    }
	public override void _PhysicsProcess(double delta)
	{
		Sprite.Visible = Monitoring;
	}
}

