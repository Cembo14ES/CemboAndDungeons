using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class DynamicRoom : Node3D
{
    // SIGNALS
    [Signal] public delegate void Signal_SetRoomStatesEventHandler(DynamicRoom room);
    [Signal] public delegate void Signal_RoomInitializedEventHandler(DynamicRoom room);

    public enum TileType { Empty = 0, Room = 1, DoorNorth = 2, DoorEast = 3, DoorSouth = 4, DoorWest = 5 }
    public enum RoomRotationEnum { None = 0, OneQuarter = 1, Half = 2, ThreeQuarters = 3 } 
    public enum RoomStateEnum { Active, Inactive, Disabled } 
    public enum RoomTypeEnum { Regular, Start, End, Main, Branch } 

    [Export] public Node3D doorFolder; 
    [Export] public Sprite3D endSprite;
    [Export] public Sprite3D startSprite;
    [Export] public Sprite3D mainSprite;
    [Export] public Sprite3D loopSprite;
    [Export] public int gridWidth = 5;
    [Export] public int gridHeight = 5;

    public Dictionary<Vector2I, int> roomGrid = new Dictionary<Vector2I, int>();
    [Export] public Godot.Collections.Array<int> gridMapData = new Godot.Collections.Array<int>();

    public List<RoomDoor> Doors { get; private set; } = new List<RoomDoor>(); 
    public List<DynamicRoom> ConnectedRooms { get; private set; } = new List<DynamicRoom>(); 

    private RoomRotationEnum roomRotation = RoomRotationEnum.None; 
    private RoomStateEnum roomState = RoomStateEnum.Disabled; 
    public RoomTypeEnum roomType = RoomTypeEnum.Regular; 
    private bool isInitialized = false;

    public RoomRotationEnum GetCurrentRotation() => roomRotation;

    public static int RotateTileValue(int tileVal, RoomRotationEnum rotation)
    {
        if (tileVal < 2 || tileVal > 5) return tileVal;
        int shift = (int)rotation;
        int baseDir = tileVal - 2;
        return ((baseDir + shift) % 4) + 2;
    }

    public Dictionary<Vector2I, int> GetRotatedRoomGrid(RoomRotationEnum rotation)
    {
        Dictionary<Vector2I, int> rotatedGrid = new Dictionary<Vector2I, int>();
        int w = gridWidth;
        int h = gridHeight;

        foreach (var kvp in roomGrid)
        {
            Vector2I pos = kvp.Key;
            int x = pos.X;
            int y = pos.Y;

            Vector2I newPos = rotation switch
            {
                RoomRotationEnum.None => new Vector2I(x, y),
                RoomRotationEnum.OneQuarter => new Vector2I(h - 1 - y, x),
                RoomRotationEnum.Half => new Vector2I(w - 1 - x, h - 1 - y),
                RoomRotationEnum.ThreeQuarters => new Vector2I(y, w - 1 - x),
                _ => new Vector2I(x, y)
            };
            
            rotatedGrid[newPos] = RotateTileValue(kvp.Value, rotation);
        }
        return rotatedGrid;
    }

    public void InitializeRoom(RoomTypeEnum type)
    {
        if (isInitialized) return;
        isInitialized = true;

        foreach (Node child in doorFolder.GetChildren())
        {
            if (child is RoomDoor door)
            {
                Doors.Add(door);
                door.ParentRoom = this;
                door.RotateDoorToDirection();
            }
        }
        
        if (roomGrid.Count == 0 && gridMapData.Count == gridWidth * gridHeight)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int idx = (y * gridWidth) + x;
                    if (gridMapData[idx] > 0) roomGrid[new Vector2I(x, y)] = gridMapData[idx];
                }
            }
        }

        setRoomType(type);
        EmitSignal(SignalName.Signal_RoomInitialized, this);
    }

    public void SetRotation(RoomRotationEnum newRotation)
    {
        int steps = ((int)newRotation - (int)roomRotation + 4) % 4;

        foreach (RoomDoor door in Doors)
        {
            int currentIdx = RoomDoor.DirectionToIndex(door.cardinalDirection);
            door.cardinalDirection = RoomDoor.IndexToDirection(currentIdx + steps);
        }

        roomRotation = newRotation;
        RotationDegrees = new Vector3(0, -(int)newRotation * 90, 0);
    }

    public Transform3D GetRelativeTransform(Node3D childNode)
    {
        Transform3D localTrans = childNode.Transform;
        Node parent = childNode.GetParent();

        while (parent != null && parent != this && parent is Node3D parent3D)
        {
            localTrans = parent3D.Transform * localTrans; 
            parent = parent.GetParent();
        }
        return localTrans;
    }

    public void SetRoomState(RoomStateEnum state)
    {
        roomState = state;
        Visible = state != RoomStateEnum.Disabled;
        SetDeferred(Node.PropertyName.ProcessMode, state == RoomStateEnum.Active ? (int)ProcessModeEnum.Always : (int)ProcessModeEnum.Disabled);
    }
    
    public List<RoomDoor> GetAvailableDoors() => Doors.Where(d => !d.isConnected).ToList();

    public void setRoomType(RoomTypeEnum type)
    {
        roomType = type;
        endSprite.Visible = (type == RoomTypeEnum.End);
        startSprite.Visible = (type == RoomTypeEnum.Start);
        mainSprite.Visible = (type == RoomTypeEnum.Main);
        loopSprite.Visible = (type == RoomTypeEnum.Branch);
    }  

    /// <summary>
    /// Lisitener para cuando el personaje entra en el Area3D de la habitacion.
    /// </summary>
    /// <param name="body"></param>
    private void On_PlayerEnter(Node3D body)
    {
        if (body.Name == "Character")
        {
            EmitSignal(SignalName.Signal_SetRoomStates, this); //Emite una señal para actualizar el estado de la habitacion.
        }
    }
}