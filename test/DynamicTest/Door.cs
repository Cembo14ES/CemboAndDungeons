using Godot;

[Tool]
public partial class Door : StaticBody3D
{
    //Esta señal se usa para notificar una teletrasportacion del jugador
    [Signal] public delegate void Signal_TeleportPlayerEventHandler(Vector3 teleportPoint);

    [ExportGroup("Grid Information")]

    [Export] public DoorDirection cardinalDirection = DoorDirection.North; //La direccion cardinal que tiene la puerta
    /// <summary>
    /// La posicion que tiene la puerta en el Tablero de su Habitacion Padre.
    /// </summary>
    [Export] public Vector2I RoomGridPosition { get; set; } = Vector2I.Zero; //La posicion en la que la puerta se encuentra en el tablero de su habitacion.
    [Export] public Vector2I GlobalGridPosition { get; set; } = Vector2I.Zero;

    [ExportGroup("References")]
    [Export] public MeshInstance3D wallMesh; //El modelo 3D de la puerta
    [Export] public CollisionShape3D wallCollision; //La hitbox de la puerta
    [Export] public Area3D teleportArea; //Un area en la que si el jugador entra, sera teletransportado
    [Export] public Node3D teleportPoint; //El punto en el que el jugador sera teletrasportado


    /// <summary>
    /// Una lista de posibles direcciones que pueden tener la puertas.
    /// </summary>
    public enum DoorDirection { North = 0, South = 1, East = 2, West = 3 }  //Enum para las direcciones de la puerta

    /// <summary>
    /// Si la puerta esta conectada a otra puerta, la otra puerta se almacena en esta variable.
    /// </summary>
    private Door connectedTo { get; set; }

    /// <summary>
    /// La habitacion en la que se encuentra esta puerta.
    /// </summary>
    private Room parentRoom { get; set; }

    /// <summary>
    /// Determina si la puerta ya esta conectada a otra puerta.
    /// </summary>
    [Export] private bool isConnected { get; set; } = false;

    /// <summary>
    /// Determina si la puerta es valida para ser conectada a otra puerta. Una puerta es invalida cuando ya esta conectada, o cuando tiene un muro enfrente.
    /// </summary>
    [Export] private bool isValidForConnection { get; set; } = true;


    /// Getters y Setters

    public bool GetIsConnected() => isConnected;
    public bool GetAvailableForConnection() => isValidForConnection;
    public Room GetParentRoom() => parentRoom;
    


    /// <summary>
    /// Conecta esta puerta a otra y viceversa.
    /// </summary>
    /// <param name="doorThatIsConnectedTo">La otra puerta.</param>
    public void ConnectDoor(Door doorThatIsConnectedTo)
    {
        isConnected = true;
        isValidForConnection = false;
        connectedTo = doorThatIsConnectedTo;

        doorThatIsConnectedTo.isConnected = true;
        doorThatIsConnectedTo.isValidForConnection = false;
        doorThatIsConnectedTo.connectedTo = this;
    }

    /// <summary>
    /// Marks this door as not being able to be connected.
    /// </summary>
    public void SetInvalidForConnection()
    {
        isValidForConnection = false;
    }

    /// <summary>
    /// El metodo compara las direcciones cardinales de dos puertas, y comprueba si eson compatibles para conexion.
    /// Las puertas solo se pueden conectar si poseen direcciones cardinales opuestas.
    /// </summary>
    /// <param name="otherDoor">La otra puerta a comparar.</param>
    /// <returns>True si se pueden conectar, False si no.</returns>
    public bool CanConnectTo(Door otherDoor)
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
    /// Guarda la habitacion en la que se encuentra la puerta, y calcula el GlobalGridPosition.
    /// </summary>
    /// <param name="room"></param>
    public void SetParentRoom(Room room)
    {
        parentRoom = room;
        //RoomGridPosition es la posicion original de la puerta, asi que hay que rotarla con la habitacion antes de sumarle el origen.
        GlobalGridPosition = parentRoom.gridMapOrigin + parentRoom.RotateGridPosition(RoomGridPosition, parentRoom.GetCurrentRotation(), parentRoom.gridWidth, parentRoom.gridHeight);
    }

    /// <summary>
    /// Obtiene el tipo de tile numérico de la puerta aplicando la rotación de la habitación padre.
    /// </summary>
    public int GetRotatedTileType()
    {
        int baseDirType = cardinalDirection switch
        {
            DoorDirection.North => 2,
            DoorDirection.East => 3,
            DoorDirection.South => 4,
            DoorDirection.West => 5,
            _ => 2
        };
        if (parentRoom == null) return baseDirType;
        return Room.RotateTileValue(baseDirType, parentRoom.GetCurrentRotation());
    }

    /// <summary>
    /// Actualiza el estado fisico de la puerta, si deveria de desaparecver o no, y si hay que activar el teletransportador.
    /// </summary>
    /// <param name="activateTP">Si hay que activar el Teletransporte</param>
    public void InitializeDoorVisuals(bool activateTP)
    {
        if (wallMesh != null) wallMesh.Visible = !isConnected;
        if (wallCollision != null) wallCollision.SetDeferred("disabled", isConnected);

        if (teleportArea != null)
        {
            if (!isConnected || !activateTP)
            {
                teleportArea.QueueFree();
                teleportArea = null;
            }
            else
            {
                teleportArea.Visible = true;
                teleportArea.SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Always);
            }
        }
    }

    /// <summary>
    /// Un listener para cuando el jugador entra en el area de teletransporte, para emitir su señal.
    /// </summary>
    /// <param name="body">El Nodo que ha entrado en el area de TP</param>
    public void On_teleportEntered(Node3D body)
    {
        if (body.Name == "Character" && connectedTo != null && IsInstanceValid(connectedTo.teleportPoint))
        {
            EmitSignal(SignalName.Signal_TeleportPlayer, connectedTo.teleportPoint.GlobalPosition);
        }
    }
}