using Godot;

/// <summary>
/// Este Script va en las puertas de una habitacion. Las habitaciones tienen varios huecos para puerta, y una
/// puerta es colocada en cada hueco. La puerta es visible o no dependiendo si hay una conexion con otra habitacion o no.
/// </summary>
public partial class RoomDoor : StaticBody3D
{
    // -------
    // SIGNALS
    // -------
    [Signal] public delegate void Signal_TeleportPlayerEventHandler(Vector3 teleportPoint); //Esta señal es emitida para transportar al jugador de habitacion a habitacion.

    // ------------------
    // EXPORTED VARIABLES
    // ------------------

    [Export] public DoorDirection cardinalDirection = DoorDirection.North; //La posicion en la que la puerta esta colocada.
    [Export] public MeshInstance3D wallMesh; //El Modelo 3D de la puerta
    [Export] public CollisionShape3D wallCollision; //La colision de la puerta
    [Export] public Area3D teleportArea; //El Area para iniciar el TP del jugador
    [Export] public Node3D teleportPoint; //El punto en el que el jugador es TPado


    // ---------------
    // OTHER VARIABLES
    // ---------------
    public enum DoorDirection { North, South, East, West }  //Enum para la posicion cardinal en la que la puerta esta colocada.
    public RoomDoor ConnectedTo { get; set; } //La puerta a la que esta conectada. (Si hay una conexion de habitaciones)
    public DynamicRoom ParentRoom { get; set; } //La habitacion en la que esta posicionada.

    public bool isConnected { get; set; } = false; //Si la puerta esta conectada a otra puerta/habitacion
    public bool isValidForConnection { get; set;} = true;

    public void SetConnected(bool state)
    {
        isConnected = state;
        isValidForConnection = !state;
    }

    /// <summary>
    /// Rota la puerta acorde a la direccion cardinal asignada.
    /// </summary>
    public void RotateDoorToDirection()
    {
        if (cardinalDirection == DoorDirection.North)
            Rotation = new Vector3(0, Mathf.DegToRad(-90), 0);
        else if (cardinalDirection == DoorDirection.South)
            Rotation = new Vector3(0, Mathf.DegToRad(90), 0);
        else if (cardinalDirection == DoorDirection.East)
            Rotation = new Vector3(0, Mathf.DegToRad(180), 0);
        else if (cardinalDirection == DoorDirection.West)
            Rotation = new Vector3(0, 0, 0);
    }

    /// <summary>
    /// El metodo compara dos puertas, y determina si es posible que se puedan conectar o no. Puertas solo se pueden
    /// conectar si tiene direcciones cardinales contrarias.
    /// </summary>
    /// <param name="otherDoor">La otra puerta con la que comparar la actual</param>
    /// <returns>Si la puerta se puede conectar o no</returns>
    public bool CanConnectTo(RoomDoor otherDoor)
    {
        return cardinalDirection switch
        {
            DoorDirection.North => otherDoor.cardinalDirection == DoorDirection.South,
            DoorDirection.South => otherDoor.cardinalDirection == DoorDirection.North,
            DoorDirection.East => otherDoor.cardinalDirection == DoorDirection.West,
            DoorDirection.West => otherDoor.cardinalDirection == DoorDirection.East,
            _ => false
        };
    }

    /// <summary>
    /// El metodo actualiza el estado de la puerta, para reflejar la generacion de habitaciones.
    /// </summary>
    public void ResolveDoorState(float roomSpacing)
    {
        wallMesh.Visible = !isConnected;
        wallCollision.SetDeferred("disabled", isConnected);

        if (roomSpacing == 0) //Si hay espacion entre las habitaciones o no
        {
            teleportArea.Visible = false;
            teleportArea.SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
        }
        else
        {
            teleportArea.Visible = isConnected;
            if (!isConnected)
                teleportArea.SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
        }
    }

    //Listener para cuando el personaje entra en el area de Teletransporte.
    public void On_teleportEntered(Node3D body)
    {
        if (body.Name == "Character")
        {
            EmitSignal(SignalName.Signal_TeleportPlayer, ConnectedTo.teleportPoint.GlobalPosition);
        }
    }
}