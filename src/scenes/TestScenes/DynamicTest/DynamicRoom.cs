using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Este Script va a una habitacion. Es usado principalmente para la generacion del nivel, y tambien 
/// alternar estados de visibilidad y actividad.
/// </summary>
public partial class DynamicRoom : Node3D
{
    // -------
    // SIGNALS
    // -------
    [Signal] public delegate void Signal_SetRoomStatesEventHandler(DynamicRoom room); //This signal is emited to notify the Player entering this room's area.

    // ------------------
    // EXPORTED VARIABLES
    // ------------------
    [Export] public Area3D roomArea; //The Area3D containing the Collisions of the rooms bounding boxes.
    [Export] public Node3D doorFolder; //This Node3D contains all the Doors of the room.

    [Export] public Sprite3D endSprite;
    [Export] public Sprite3D startSprite;
    [Export] public Sprite3D mainSprite;
    [Export] public Sprite3D loopSprite;

    // -----
    // ENUMS
    // -----
    public enum RoomRotationEnum {None = 0, OneQuarter = 1, Half = 2, ThreeQuarters = 3 } //This Enum Contains the rotation Posibilities
    public enum RoomStateEnum {Active, Inactive, Disabled } //The posible state of the room
    public enum RoomTypeEnum {Regular, Start, End, Main, Loop} //The Type of room.

    // ---------------
    // ROOM CONECTIONS
    // ---------------
    public List<RoomDoor> Doors { get; private set; } = new List<RoomDoor>(); //This List contains all the Doors of the room.
    public List<DynamicRoom> ConnectedRooms { get; private set; } = new List<DynamicRoom>(); //This List contains all the Rooms conected to this room.

    // -----------------
    // PRIVATE VARIABLES
    // -----------------
    private RoomRotationEnum roomRotation = RoomRotationEnum.None; //The room's current rotation.
    private RoomStateEnum roomState = RoomStateEnum.Disabled; //The room's current rotation.
    public RoomTypeEnum roomType = RoomTypeEnum.Regular; //The room's current Type.
    private List<CollisionShape3D> roomAreaShapes = new List<CollisionShape3D>();
    private bool isInitialized = false;



    //Inicializa la habitacion.
    public void InitializeRoom(RoomTypeEnum type)
    {
        if (isInitialized) return;
        isInitialized = true;

        //Obtiene todas las puertas y las guarda
        foreach (Node child in doorFolder.GetChildren())
        {
            if (child is RoomDoor door)
            {
                Doors.Add(door);
                door.ParentRoom = this;
                door.RotateDoorToDirection();
            }
        }

        //Obtiene las collisiones del Area de la habitacion
        foreach (Node child in roomArea.GetChildren())
        {
            if (child is CollisionShape3D colShape) roomAreaShapes.Add(colShape);
        }
        
        setRoomType(type);
    }

    /// <summary>
    /// Gira la habitacion y las puertas de esta sobre el eje Y.
    /// </summary>
    /// <param name="newRotation">La nueva rotacion</param>
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

    /// <summary>
    /// El metodo se encarga de recoger todos los AABBs del Area3D de la habitacion. Un AABB es
    /// una representacion de una dimension en 3D usado para hacer calculos rapidos.
    /// </summary>
    /// <returns>Una lista con todas las AABBs</returns>
    private List<Aabb> GetRoomAABBs()
    {
        List<Aabb> roomAABBs = new List<Aabb>();

        //Itera por cada collisionShape en el Area3D
        foreach (CollisionShape3D colShape in roomAreaShapes)
        {
            Transform3D nodeWorldTransform = Transform * GetRelativeTransform(colShape);

            Aabb globalAABB = new Aabb();
            bool hasValidShape = true;

            // Realiza diferentes calculos, dependiendo el tipo de forma que tiene el CollisionShape3D
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
                GD.PrintErr($"Unsupported shape type in {this.Name}: {colShape.Name}");
                hasValidShape = false;
            }

            if (hasValidShape)
            {
                roomAABBs.Add(globalAABB);
            }
        }
        return roomAABBs;
    }

    /// <summary>
    /// El Metodo comprueba si dos habitaciones se sobreponen, iterando a traves de sus AABBs.
    /// </summary>
    /// <param name="otherRoom">La otra habitacion.</param>
    /// <param name="roomSpacing">El espacio de separacion que hay entre habitaciones.</param>
    /// <returns>Si las habitaciones se sobreponen o no.</returns>
    public bool IntersectsWith(DynamicRoom otherRoom, float roomSpacing)
    {
        //Obtiene los AABBs de las habitaciones.
        List<Aabb> thisRoomAABBs = GetRoomAABBs();
        List<Aabb> otherRoomAABBs = otherRoom.GetRoomAABBs();

        //Itera por todas las AABBs.
        foreach (Aabb myAABB in thisRoomAABBs)
        {
            Aabb shrunkMine = myAABB.Grow(roomSpacing); //Agranda el tamaño de la habitacion acorde a la separacion

            foreach (Aabb otherAABB in otherRoomAABBs)
            {
                Aabb shrunkOther = otherAABB.Grow(roomSpacing); //Agranda el tamaño de la habitacion acorde a la separacion

                if (shrunkMine.Intersects(shrunkOther)) //Comprueba si las habitaciones se sobreponen.
                    return true; 
            }
        }

        return false; //Si no se ha detectado ninguna sobreposicion, se retorna False
    }

    /// <summary>
    /// Este metodo realiza calculos para obtener el GlobalTransform de un Nodo, ya que al no estar
    /// inizializado, no se puede usar GlobalTransform.
    /// </summary>
    /// <param name="childNode">El Nodo a obtener el Transform</param>
    /// <returns>El Transform Global del Nodo</returns>
    public Transform3D GetRelativeTransform(Node3D childNode)
    {
        Transform3D localTrans = childNode.Transform;
        Node parent = childNode.GetParent();

        while (parent != null && parent != this && parent is Node3D parent3D)
        {
            localTrans = parent3D.Transform * localTrans; //Calculo del Global Tranform
            parent = parent.GetParent();
        }
        return localTrans;
    }

    /// <summary>
    /// Actualiza el estado de la habitacion. Un a habitacion puede tener 3 estados:
    /// -Activo: La habitacion se renderiza y sus elementos son procesados.
    /// -Inactivo: La habitacion se renderiza, pero sus elementos no son procesados (Congelado).
    /// -Desactivado: La habitacion no se renderiza y sus elementos no son procesados.
    /// 
    /// El Area3D de la habitacion se procesa independientemente del estado de la habitacion.
    /// </summary>
    /// <param name="state">El nuevo estado de la habitacion.</param>
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
    }
    
    /// <summary>
    /// Devuelve todas las puertas que no estan conectadas a otra habitacion.
    /// </summary>
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
        
        if (type == RoomTypeEnum.End)
        {
            endSprite.Visible = true;
        }
        if (type == RoomTypeEnum.Start)
        {
            startSprite.Visible = true;
        }
        if (type == RoomTypeEnum.Main)
        {
            mainSprite.Visible = true;
        }
        if (type == RoomTypeEnum.Loop)
        {
            loopSprite.Visible = true;
        }  
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
