using Godot;
using System;
using System.Collections.Generic;

public partial class BaseAttack : CharacterBody3D
{
    [ExportGroup("Stats")]
    [Export] public AttackStats originalStats; //Las estadisticas del ataque.
    public AttackStats stats = new AttackStats();

    [ExportGroup("Attack Hitbox")]
    [Export] public Area3D hitboxArea; //El Area3D que contiene la hitbox del ataque.
    [Export] public CollisionShape3D hitboxAreaShape; //El CollisionShape3D de hitboxArea    

    protected BaseCharacter owner; //El BaseCharacter que ha spawneado el ataque.
    protected float attackDirectionAngle; //La direcion a la que va a apuntar el ataque.
    protected Vector3 attackPosition; //La posicion en la que va a spwanear el ataque.

    protected Timer hitboxTimer = new Timer(); //Timer para el tiempo de vida de la hitbox del ataque.
    protected Timer attackTimer = new Timer(); //Timer para el tiempo de vida del ataque.
    protected readonly List<BaseCharacter> alreadyHit = new(); //Contiene todos los personajes que el ataque ya haya golpeado.
    protected List<BaseCharacter> attackTargets;

    /// <summary>
    /// Inicializa varios valores del ataque.
    /// La llama el personaje justo después de Instantiate y ANTES de AddChild.
    /// </summary>
    /// <param name="newOwner">El personaje que crea este ataque.</param>
    /// <param name="newDirection">La direccion a la que el ataque apunta.</param>
    /// <param name="newPosition">La posicion en la que el ataque Spawnea.</param>
    public void Setup(BaseCharacter newOwner, List<BaseCharacter> targets, float newDirectionAngle, Vector3 newPosition, float attackDamage)
    {
        stats = (AttackStats)originalStats.Duplicate();

        owner = newOwner;
        attackTargets = targets;
        attackDirectionAngle = newDirectionAngle;
        attackPosition = newPosition;
        stats.AttackDamage = attackDamage;
    }

    public override void _Ready()
    {
        //Comprueba que todas las variables sean validas.
        CheckExportedValues();
        CheckStatsValues();

    
        if (!stats.AttackAlwaysActive) //Inicializa los Timers de vida.
        {
            InitializeTimer(attackTimer, stats.AttackNodeAliveTimeStat, true, Die);
            attackTimer.Start();

            InitializeTimer(hitboxTimer, stats.HitboxNodeAliveTimeStat, true, () => hitboxAreaShape.Disabled = false);
            hitboxTimer.Start();
        }

        if (stats.projectileSpeed != 0) //EN caso de que el ataque sea un proyectil, esto desconecta su posicion con la de el personaje.
        {
            TopLevel = true;
        }

        hitboxArea.AreaEntered += OnAreaEntered; //Asigna la señal al metodo

        PlaceAttack(new Vector3(0, attackDirectionAngle, 0), attackPosition); //Inicializa la posicion y direccion del ataque.
    }

    protected virtual void OnAreaEntered(Area3D area)
    {
        if (area.GetParent() is BaseCharacter target && target.damageTrigger == area && target != owner) //Comprueba que el objetivo sea la hitbox de otro personaje.
            if (attackTargets.Contains(target) || attackTargets.Count == 0) //Comprueba que el personaje se aun objetivo.
                
                target.TakeDamage(stats.FinalDamage, attackDirectionAngle);
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

    public virtual void Die()
    {
        QueueFree();
    }

    /// <summary>
    /// Inicializa un Timer. Lo añade a la escena y inicializa sus valores que siempre necesitan ser asignados.
    /// </summary>
    /// <param name="timer">El Timer a ser inicializado.</param>
    /// <param name="waitTime">El tiempo de duracion del timer en segundos.</param>
    /// <param name="oneshot">Si el Timer se ejecuta una sola vez, o si se reinicia cada vez que se acaba.</param>
    /// <param name="onTimeout">La accion que se debe de realizar cuando el Timer termina.</param>
    protected virtual void InitializeTimer(Timer timer, double waitTime, bool oneshot, Action onTimeout)
    {
        AddChild(timer);
        timer.WaitTime = waitTime;
        timer.OneShot = oneshot;
        timer.Timeout += onTimeout;
    }

    /// <summary>
    /// Comprueba los variables Exportados para ver si estan asignados, y pushea un Warning sino.
    /// Los hijos debed sobreescribir el metodo, añadiendo sus propias variables Exportadas.
    /// </summary>
    protected virtual void CheckExportedValues()
    {
        if (originalStats == null)
            GD.PushWarning($"{Name}: No tiene Stats asignado.");
        if (hitboxArea == null)
            GD.PushWarning($"{Name}: No tiene Hitbox Area asignado.");
        if (hitboxAreaShape == null)
            GD.PushWarning($"{Name}: No tiene Hitbox Area Shape asignado.");
    }

    /// <summary>
    /// Comprueba los valores de las estadisticas del ataque para ver si son validas.
    /// Asigna valores por defecto en caso de que haya alguna estadistica no valida.
    /// </summary>
    protected virtual void CheckStatsValues()
    {
        if (stats.AttackMultiplier < 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de AttackMultiplier es menor a 0.");
            stats.AttackMultiplier = 0;
        }
        if (stats.AttackNodeAliveTimeStat <= 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de AttackNodeAliveTimeStat es menor o igual a 0.");
            stats.AttackNodeAliveTimeStat = 1;
        }
        if (stats.HitboxNodeAliveTimeStat <= 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de HitboxNodeAliveTimeStat es menor o igual a 0.");
            stats.HitboxNodeAliveTimeStat = 1;
        }
        GD.Print($"{Name} Stats: AttackAlwaysActive is {stats.AttackAlwaysActive}");
        GD.Print($"{Name} Stats: attackTarget is {stats.attackTarget.ToString()}");
        if (stats.projectileSpeed < 0)
        {
            GD.PushWarning($"{Name} Stats: El valor de projectileSpeed es menor a 0.");
            stats.projectileSpeed = 0;
        }
    }
}