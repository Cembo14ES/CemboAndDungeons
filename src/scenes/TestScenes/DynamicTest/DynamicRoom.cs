using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Este Script va a una habitacion. Es usado principalmente para la generacion del nivel, y tambien 
/// alternar estados de visibilidad y actividad.
/// </summary>
public partial class DynamicRoom : Node3D
{
    [Signal] public delegate void Signal_SetRoomStatesEventHandler(DynamicRoom room); //Esta Señal se emite para actualizar el estado de la habitacion

    [Export] public Godot.Collections.Array<MeshInstance3D> HitboxNodes;
    [Export] public Area3D roomArea;

    public List<RoomDoor> Doors { get; private set; } = new List<RoomDoor>();
    public List<DynamicRoom> ConnectedRooms { get; private set; } = new List<DynamicRoom>();

    private int _currentRotation = 0;
    private bool _isInitialized = false;

    public override void _Ready()
    {
        InitializeRoom();
    }

    public void InitializeRoom()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        FindDoors(this);
        foreach (RoomDoor door in Doors)
        {
            door.ParentRoom = this;
            door.RotateDoorToDirection(); // Lock in visual orientation immediately
        }

        if (HitboxNodes == null || HitboxNodes.Count == 0)
        {
            GD.PrintErr("No mesh in room: " + this.Name);
        }
    }

    public void SetRotation(int degrees)
    {
        int diff = degrees - _currentRotation;
        diff = (diff % 360 + 360) % 360;
        int steps = diff / 90;

        for (int i = 0; i < steps; i++)
        {
            foreach (RoomDoor door in Doors)
            {
                // Only logically update the enum for generation math. 
                // The hierarchy automatically handles visual rotation.
                door.Direction = RotateDirectionCounterClockwise(door.Direction);
            }
        }
        _currentRotation = degrees;
        this.RotationDegrees = new Vector3(0, degrees, 0);
    }

    private RoomDoor.DoorDirection RotateDirectionCounterClockwise(RoomDoor.DoorDirection dir)
    {
        return dir switch
        {
            RoomDoor.DoorDirection.North => RoomDoor.DoorDirection.West,
            RoomDoor.DoorDirection.West => RoomDoor.DoorDirection.South,
            RoomDoor.DoorDirection.South => RoomDoor.DoorDirection.East,
            RoomDoor.DoorDirection.East => RoomDoor.DoorDirection.North,
            _ => dir
        };
    }

    private void FindDoors(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is RoomDoor door) Doors.Add(door);
            FindDoors(child);
        }
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

    public List<Aabb> GetGlobalAABBs()
    {
        List<Aabb> globalAABBs = new List<Aabb>();
        if (HitboxNodes == null) return globalAABBs;

        foreach (Node3D node in HitboxNodes)
        {
            if (node is VisualInstance3D visualMesh)
            {
                Transform3D nodeWorldTransform = this.Transform * GetRelativeTransform(node);
                Aabb globalAABB = nodeWorldTransform * visualMesh.GetAabb();
                globalAABBs.Add(globalAABB);
            }
        }
        return globalAABBs;
    }

    public bool IntersectsWith(DynamicRoom otherRoom, float margin)
    {
        float tolerance = 0.1f;
        List<Aabb> myAABBs = GetGlobalAABBs();
        List<Aabb> otherAABBs = otherRoom.GetGlobalAABBs();

        foreach (Aabb myAABB in myAABBs)
        {
            Aabb shrunkMine = myAABB.Grow(margin - tolerance);
            foreach (Aabb otherAABB in otherAABBs)
            {
                if (shrunkMine.Intersects(otherAABB.Grow(margin - tolerance))) return true;
            }
        }
        return false;
    }

    public List<RoomDoor> GetAvailableDoors()
    {
        return Doors.Where(d => !d.isConnected).ToList();
    }

    public void On_PlayerEnter(Node3D body)
    {
        GD.Print("body entered");
        if (body.Name == "Character")
        {
            GD.Print("character entered");
            EmitSignal(SignalName.Signal_SetRoomStates, this);
        }
    }

    public void SetActiveRoom()
    {
        Visible = true;
        SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Always);
    }

    public void SetInactiveRoom()
    {
        Visible = true;
        SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
    }

    public void SetDisabledRoom()
    {
        Visible = false;
        SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
    }
}
