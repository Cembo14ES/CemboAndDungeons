using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class DynamicRoom : Node3D
{
    [Signal] public delegate void Signal_SetRoomStatesEventHandler(DynamicRoom room);

    public enum TileType { Empty = 0, Room = 1, DoorNorth = 2, DoorEast = 3, DoorSouth = 4, DoorWest = 5 }
    public enum RoomRotationEnum { None = 0, OneQuarter = 1, Half = 2, ThreeQuarters = 3 } 
    public enum RoomStateEnum { Active, Inactive, Disabled } 
    public enum RoomTypeEnum { Regular, Start, End, Main, Loop } 

    [Export] public Area3D roomArea; 
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
    private List<CollisionShape3D> roomAreaShapes = new List<CollisionShape3D>();
    private bool isInitialized = false;

    public RoomRotationEnum GetCurrentRotation() => roomRotation;

    public static int RotateTileValue(int tileVal, RoomRotationEnum rotation)
    {
        if (tileVal < 2 || tileVal > 5) return tileVal;
        int shift = (int)rotation; // 0=None, 1=90CW, 2=180, 3=270CW
        int baseDir = tileVal - 2; // 0=N, 1=E, 2=S, 3=W
        int newDir = (baseDir + shift) % 4;
        return newDir + 2;
    }

    public Dictionary<Vector2I, int> GetRotatedRoomGrid(RoomRotationEnum rotation)
    {
        Dictionary<Vector2I, int> rotatedGrid = new Dictionary<Vector2I, int>();
        int w = gridWidth;
        int h = gridHeight;

        foreach (var kvp in roomGrid)
        {
            Vector2I pos = kvp.Key;
            int val = kvp.Value;
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
            
            rotatedGrid[newPos] = RotateTileValue(val, rotation);
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

        foreach (Node child in roomArea.GetChildren())
        {
            if (child is CollisionShape3D colShape) roomAreaShapes.Add(colShape);
        }
        
        if (roomGrid.Count == 0 && gridMapData.Count == gridWidth * gridHeight)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int idx = (y * gridWidth) + x;
                    if (gridMapData[idx] > 0)
                    {
                        roomGrid[new Vector2I(x, y)] = gridMapData[idx];
                    }
                }
            }
        }

        setRoomType(type);
    }

    public void SetRotation(RoomRotationEnum newRotation)
    {
        int steps = ((int)newRotation - (int)roomRotation + 4) % 4;

        foreach (RoomDoor door in Doors)
        {
            door.cardinalDirection = (RoomDoor.DoorDirection)(((int)door.cardinalDirection + steps) % 4);
        }

        roomRotation = newRotation;
        RotationDegrees = new Vector3(0, (int)newRotation * 90, 0);
    }

    private List<Aabb> GetRoomAABBs()
    {
        List<Aabb> roomAABBs = new List<Aabb>();
        foreach (CollisionShape3D colShape in roomAreaShapes)
        {
            Transform3D nodeWorldTransform = Transform * GetRelativeTransform(colShape);
            Aabb globalAABB = new Aabb();
            bool hasValidShape = true;

            if (colShape.Shape is ConvexPolygonShape3D convex)
            {
                Vector3[] points = convex.Points;
                if (points != null && points.Length > 0)
                {
                    globalAABB = new Aabb(nodeWorldTransform * points[0], Vector3.Zero);
                    for (int i = 1; i < points.Length; i++)
                        globalAABB = globalAABB.Expand(nodeWorldTransform * points[i]);
                }
                else hasValidShape = false;
            }
            else if (colShape.Shape is BoxShape3D box)
            {
                Vector3 half = box.Size / 2;
                Vector3[] corners = {
                    new Vector3(-half.X, -half.Y, -half.Z), new Vector3(half.X, -half.Y, -half.Z),
                    new Vector3(-half.X, half.Y, -half.Z), new Vector3(half.X, half.Y, -half.Z),
                    new Vector3(-half.X, -half.Y, half.Z), new Vector3(half.X, -half.Y, half.Z),
                    new Vector3(-half.X, half.Y, half.Z), new Vector3(half.X, half.Y, half.Z)
                };

                globalAABB = new Aabb(nodeWorldTransform * corners[0], Vector3.Zero);
                for (int i = 1; i < 8; i++)
                    globalAABB = globalAABB.Expand(nodeWorldTransform * corners[i]);
            }
            else
            {
                hasValidShape = false;
            }

            if (hasValidShape) roomAABBs.Add(globalAABB);
        }
        return roomAABBs;
    }

    public bool IntersectsWith(DynamicRoom otherRoom, float roomSpacing)
    {
        List<Aabb> thisRoomAABBs = GetRoomAABBs();
        List<Aabb> otherRoomAABBs = otherRoom.GetRoomAABBs();

        foreach (Aabb myAABB in thisRoomAABBs)
        {
            Aabb shrunkMine = myAABB.Grow(roomSpacing); 
            foreach (Aabb otherAABB in otherRoomAABBs)
            {
                Aabb shrunkOther = otherAABB.Grow(roomSpacing); 
                if (shrunkMine.Intersects(shrunkOther)) return true; 
            }
        }
        return false; 
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
        switch (state)
        {
            case RoomStateEnum.Active:
                Visible = true;
                SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Always);
                break;
            case RoomStateEnum.Inactive:
                Visible = true;
                SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
                break;
            case RoomStateEnum.Disabled:
                Visible = false;
                SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
                break;
        }
        EmitSignal(SignalName.Signal_SetRoomStates, this);
    }
    
    public List<RoomDoor> GetAvailableDoors()
    {
        return Doors.Where(d => !d.isConnected).ToList();
    }

    public void setRoomType(RoomTypeEnum type)
    {
        roomType = type;
        if (type == RoomTypeEnum.Regular)
        {
            endSprite.Visible = false;
            startSprite.Visible = false;
            mainSprite.Visible = false;
            loopSprite.Visible = false;
        }
        if (type == RoomTypeEnum.End) endSprite.Visible = true;
        if (type == RoomTypeEnum.Start) startSprite.Visible = true;
        if (type == RoomTypeEnum.Main) mainSprite.Visible = true;
        if (type == RoomTypeEnum.Loop) loopSprite.Visible = true;
    }
}