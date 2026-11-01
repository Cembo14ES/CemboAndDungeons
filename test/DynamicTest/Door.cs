using Godot;

[Tool]
public partial class Door : StaticBody3D
{
    //Esta señal se usa para notificar una teletrasportacion del jugador
    [Signal] public delegate void Signal_TeleportPlayerEventHandler(Vector3 teleportPoint);

    [ExportGroup("Grid Information")]
    [Export] public DoorDirection cardinalDirection = DoorDirection.North; //La direccion cardinal que tiene la puerta
    [Export] public Vector2I RoomGridPosition { get; set; } = Vector2I.Zero; //La posicion en la que la puerta se encuentra en el tablero


    [ExportGroup("References")]
    [Export] public MeshInstance3D wallMesh; //El modelo 3D de la puerta
    [Export] public CollisionShape3D wallCollision; //La hitbox de la puerta
    [Export] public Area3D teleportArea; //Un area en la que si el jugador entra, sera teletransportado
    [Export] public Node3D teleportPoint; //El punto en el que el jugador sera teletrasportado


    //Otras variables
    public enum DoorDirection { North = 0, South = 1, East = 2, West = 3 }  //Enum para las direcciones de la puerta

    public Door ConnectedTo { get; set; } //La puerta a la que esta conectada
    public Room ParentRoom { get; set; } //La habitacion en la que se encuentra la puerta
    [Export] public bool isConnected { get; set; } = false; //Si la puerta esta conectada con otra o no
    public bool isValidForConnection { get; set; } = true; //Si la puerta es valida para conexion (necesita trabajo.)

    /// <summary>
    /// Actualiza el estado de conexion la puerta.
    /// </summary>
    /// <param name="state">El nuevo estado de la puerta</param>
    public void SetConnected(bool state)
    {
        isConnected = state;
        isValidForConnection = !state;
    }

    /// <summary>
    /// Rota la puerta sobre el eje Y dependiendo de la direccion cardinal que es.
    /// </summary>
    public void RotateDoor()
    {
        Rotation = cardinalDirection switch
        {
            DoorDirection.North => new Vector3(0, Mathf.DegToRad(-90), 0),
            DoorDirection.South => new Vector3(0, Mathf.DegToRad(90), 0),
            DoorDirection.East => new Vector3(0, Mathf.DegToRad(180), 0),
            DoorDirection.West => new Vector3(0, 0, 0),
            _ => Vector3.Zero
        };
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
    /// Actualiza el estado fisico de la puerta, si deveria de desaparecver o no, y si hay que activar el teletransportador.
    /// </summary>
    /// <param name="activateTP">Si hay que activar el Teletransporte</param>
    public void InitializeDoor(bool activateTP)
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
        if (body.Name == "Character" && ConnectedTo != null && IsInstanceValid(ConnectedTo.teleportPoint))
        {
            EmitSignal(SignalName.Signal_TeleportPlayer, ConnectedTo.teleportPoint.GlobalPosition);
        }
    }
}