using Godot;

public partial class RoomDoor : StaticBody3D
{
    [Signal] public delegate void Signal_TeleportPlayerEventHandler(Vector3 teleportPoint);

    [Export] public DoorDirection cardinalDirection = DoorDirection.North; 
    [Export] public MeshInstance3D wallMesh; 
    [Export] public CollisionShape3D wallCollision; 
    [Export] public Area3D teleportArea; 
    [Export] public Node3D teleportPoint; 
    [Export] public Vector2I RoomGridPosition { get; set; } = Vector2I.Zero;

    public enum DoorDirection { North, South, East, West }  

    public RoomDoor ConnectedTo { get; set; } 
    public DynamicRoom ParentRoom { get; set; } 
    public bool isConnected { get; set; } = false; 
    public bool isValidForConnection { get; set; } = true;

    public static int DirectionToIndex(DoorDirection dir) => dir switch
    {
        DoorDirection.North => 0,
        DoorDirection.East => 1,
        DoorDirection.South => 2,
        DoorDirection.West => 3,
        _ => 0
    };

    public static DoorDirection IndexToDirection(int idx) => (idx % 4) switch
    {
        0 => DoorDirection.North,
        1 => DoorDirection.East,
        2 => DoorDirection.South,
        3 => DoorDirection.West,
        _ => DoorDirection.North
    };

    public void SetConnected(bool state)
    {
        isConnected = state;
        isValidForConnection = !state;
    }

    public void RotateDoorToDirection()
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

    public void ResolveDoorState(float roomSpacing)
    {
        wallMesh.Visible = !isConnected;
        wallCollision.SetDeferred("disabled", isConnected);

        if (roomSpacing == 0) 
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

    public void On_teleportEntered(Node3D body)
    {
        if (body.Name == "Character")
        {
            EmitSignal(SignalName.Signal_TeleportPlayer, ConnectedTo.teleportPoint.GlobalPosition);
        }
    }
}