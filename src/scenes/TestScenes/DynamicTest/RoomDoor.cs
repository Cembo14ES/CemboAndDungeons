using Godot;

/// <summary>
/// Este Script va en las puertas de una habitacion. Las habitaciones tienen varios huecos para puerta, y una
/// puerta es colocada en cada hueco. La puerta es visible o no dependiendo si hay una conexion con otra habitacion o no.
/// </summary>
public partial class RoomDoor : StaticBody3D
{
    //Esta señal es emitida para transportar al jugador de habitacion a habitacion.
    [Signal] public delegate void Signal_TeleportPlayerEventHandler(Vector3 teleportPoint);

    //Enum para la posicion cardinal en la que la puerta esta colocada.
    public enum DoorDirection { North, South, East, West }

    [Export] public DoorDirection Direction = DoorDirection.North; //La posicion en la que la puerta esta colocada.
    [Export] public MeshInstance3D wallMesh; //El Modelo 3D de la puerta
    [Export] public CollisionShape3D wallCollision; //La colision de la puerta
    [Export] public Area3D teleportArea; //El Area para iniciar el TP del jugador
    [Export] public Node3D teleportPoint; //El punto en el que el jugador es TPado

    public bool isConnected { get; set; } = false; //Si la puerta esta en posicion donde hay una conexion de habitaciones.
    public RoomDoor ConnectedTo { get; set; } //La puerta a la que esta conectada. (Si hay una conexion de habitaciones)
    public DynamicRoom ParentRoom { get; set; } //La habitacion en la que esta posicionada.

    /// <summary>
    /// Rota la puerta acorde a la direccion cardinal asignada.
    /// </summary>
    public void RotateDoorToDirection()
    {
        if (Direction == DoorDirection.North)
            Rotation = new Vector3(0, Mathf.DegToRad(-90), 0);
        else if (Direction == DoorDirection.South)
            Rotation = new Vector3(0, Mathf.DegToRad(90), 0);
        else if (Direction == DoorDirection.East)
            Rotation = new Vector3(0, Mathf.DegToRad(180), 0);
        else if (Direction == DoorDirection.West)
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
        return Direction switch
        {
            DoorDirection.North => otherDoor.Direction == DoorDirection.South,
            DoorDirection.South => otherDoor.Direction == DoorDirection.North,
            DoorDirection.East => otherDoor.Direction == DoorDirection.West,
            DoorDirection.West => otherDoor.Direction == DoorDirection.East,
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