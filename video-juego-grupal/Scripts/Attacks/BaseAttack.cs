using Godot;
using System;
using System.Collections.Generic;

public partial class BaseAttack : CharacterBody3D
{
    [Export] public AttackStats stats; //Las estadisticas del ataque.
    [Export] public CollisionShape3D hitboxShape; //El Area3D que contiene la hitbox del ataque.
    [Export] public Area3D hitboxArea;

    protected BaseCharacter owner; //El BaseCharacter que ha spawneado el ataque.
    protected float attackDirectionAngle; //La direcion a la que va a apuntar el ataque.
    protected Vector3 attackPosition; //La posicion en la que va a spwanear el ataque.

    protected Timer hitboxTimer = new Timer(); //Timer para el tiempo de vida de la hitbox del ataque.
    protected Timer attackTimer = new Timer(); //Timer para el tiempo de vida del ataque.
    protected readonly HashSet<BaseCharacter> alreadyHit = new(); //Contiene todos los personajes que el ataque ya haya golpeado.
    protected List<BaseCharacter> targets;


    /// <summary>
    /// Inicializa varios valores del ataque.
    /// La llama el personaje justo después de Instantiate y ANTES de AddChild.
    /// </summary>
    /// <param name="newOwner">El personaje que crea este ataque.</param>
    /// <param name="newDirection">La direccion a la que el ataque apunta.</param>
    /// <param name="newPosition">La posicion en la que el ataque Spawnea.</param>
    public void Setup(BaseCharacter newOwner, List<BaseCharacter> targets, float newDirectionAngle, Vector3 newPosition, float attackDamage)
    {
        GD.Print("targets counts: " + targets.Count);
        owner = newOwner;
        this.targets = targets;
        attackDirectionAngle = newDirectionAngle;
        attackPosition = newPosition;
        hitboxTimer.WaitTime = stats.HitboxNodeAliveTimeStat;
        attackTimer.WaitTime = Mathf.Max(stats.AttackNodeAliveTimeStat, 0.05f);
        stats.AttackDamage = attackDamage; //Obtiene el daño del ataque.
        GD.Print("targets counts: " + this.targets.Count);
    }

    public override void _Ready()
    {
        //Realiza comprobaciones de varias variables para ver si estan inicializadas.
        if (owner == null || stats == null)
        {
            GD.PushError($"{Name}: Se instanció sin llamar a Setup, o no tiene Stats asignado.");
            Die();
            return;
        }
        if (hitboxShape == null)
        {
            GD.PushError($"{Name}: Se instanció sin asignar la hitbox en el editor");
            Die();
            return;
        }

        //Inicializa los Timers de vida.

        if (!stats.AttackAlwaysActive)
        {
            AddChild(attackTimer);
            attackTimer.OneShot = true;
            attackTimer.Timeout += Die;
            attackTimer.Start();

            AddChild(hitboxTimer);
            hitboxTimer.OneShot = true;
            hitboxTimer.Timeout += () => hitboxShape.SetDeferred("disabled", false);
            hitboxTimer.Start();
        }  
            

        // Este área detecta, pero nadie la detecta a ella:
        // Así un ataque no puede golpear a otro ataque (ni a su dueño a través de él).
        hitboxArea.Monitorable = false;

        PlaceAttack(new Vector3(0, attackDirectionAngle, 0), attackPosition); //Inicializa la posicion y direccion del ataque.
    }

    public override void _PhysicsProcess(double delta)
    {
        if (stats.AttackAlwaysActive)
        {
            if (!IsInstanceValid(owner)) //Si el padre desaparece, el ataque permanente tambien
            {
                Die();
                return;
            }
        }     

        foreach (Area3D area in hitboxArea.GetOverlappingAreas())
        {
            if (area.GetParent() is BaseAttack) continue;   // Ignora otros ataques.
            if (area.GetParent() is not BaseCharacter target) continue; // Ignora todo lo que no sea personajes.
            if (target == owner) continue; // Ignora el personaje que creo este ataque.
           // GD.Print("Number targets: " + targets);
            if (targets.Count == 0 || targets.Contains(target)) continue;

            if (target.damageTrigger != area) continue; //Ignora los Area3D que no sean la hitbox de ataques.
            if (!target.canBeDamaged) continue; //Ignora a los personajes que estan en un estado de invulnerabilidad.
            if (!stats.AttackAlwaysActive)
                if (!alreadyHit.Add(target)) continue; //Ignora los personajes que el ataque ya haya atacado.

            target.TakeDamage(stats.FinalDamage, attackDirectionAngle);
        }
    }

    /// <summary>
    /// Coloca el ataque en el espacio 3D. Los hijos deberian sobreecribirlo.
    /// </summary>
    /// <param name="attackDirection">La direccion a donde apunta el ataque</param>
    /// <param name="attackPosition">La posicion en la que Spawnea el ataque</param>
    protected virtual void PlaceAttack(Vector3 attackDirection, Vector3 attackPosition)
    {
        Position = attackPosition;
        Rotation = attackDirection;
    }

    protected virtual void Die()
    {
        QueueFree();
    }
}