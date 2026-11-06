using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class Room : Node3D
{
    // SIGNALS
    [Signal] public delegate void Signal_SetRoomStatesEventHandler(Room room);
    [Signal] public delegate void Signal_RoomInitializedEventHandler(Room room);

    [Export] public Node3D doorFolder; //El Nodo3D que contiene todas las puertas de la habitacion
    [Export] public Sprite3D endSprite;
    [Export] public Sprite3D startSprite;
    [Export] public Sprite3D mainSprite;
    [Export] public Sprite3D loopSprite;

    //El tamaño de tablero de la habitacion.
    [Export] public int gridWidth = 5;
    [Export] public int gridHeight = 5;

    
    public Vector2I gridMapOrigin { get; set; } = Vector2I.Zero; // Origen (Esquina superir izquierda) de la habitación en la cuadrícula global de la mazmorra (gridMap)

    // Diccionario exportado: Godot lo guarda y carga de la escena automáticamente
    [Export] public Godot.Collections.Dictionary<Vector2I, int> gridMapData { get; set; } = new Godot.Collections.Dictionary<Vector2I, int>();

    // Copia del tablero original (sin rotar). Se guarda la primera vez que se rota, para que cada rotacion se calcule siempre desde el original.
    private Godot.Collections.Dictionary<Vector2I, int> baseGridMapData;

    public List<Door> Doors { get; private set; } = new List<Door>(); //Las puertas de la habitacion
    public List<Room> ConnectedRooms { get; private set; } = new List<Room>(); //Las habitaciones conectadas a esta.

    private RoomRotationEnum roomRotation = RoomRotationEnum.None; //La rotacion que posee la habitacion.
    private RoomStateEnum roomState = RoomStateEnum.Disabled; //El estado de actividad de la habitacion.
    public RoomTypeEnum roomType = RoomTypeEnum.Regular; //El tipo de habitacion

    public enum RoomRotationEnum { None = 0, OneQuarter = 1, Half = 2, ThreeQuarters = 3 }
    public enum RoomStateEnum { Active, Inactive, Disabled }
    public enum RoomTypeEnum { Regular, Start, End, Main, Branch }
    private bool isInitialized = false;

    /// <summary>
    /// Getter para la rotacion de la habitacion
    /// </summary>
    public RoomRotationEnum GetCurrentRotation() => roomRotation;

    /// <summary>
    /// Rota el valor numérico de una casilla de puerta según la rotación de la habitación.
    /// </summary>
    public static int RotateTileValue(int tileVal, RoomRotationEnum rotation)
    {
        if (tileVal < 2 || tileVal > 5) return tileVal;
        int shift = (int)rotation;
        int baseDir = tileVal - 2;
        return ((baseDir + shift) % 4) + 2;
    }

    /// <summary>
    /// Este metodo calcula y devuelve el roomGridMap de la habitacion rotado a la rotacion necesaria.
    /// Siempre parte del tablero original, asi que se puede llamar varias veces con distintas rotaciones sin que se acumulen.
    /// </summary>
    public void RotateRoomGrid()
    {
        Godot.Collections.Dictionary<Vector2I, int> rotatedGrid = new Godot.Collections.Dictionary<Vector2I, int>();
        int localWidth = gridWidth;
        int localHeight = gridHeight;

        baseGridMapData ??= gridMapData; //Guarda el tablero original la primera vez.

        foreach (var kvp in baseGridMapData)
        {
            Vector2I pos = kvp.Key;
            int tileVal = kvp.Value;
            if (tileVal <= 0) continue;

            Vector2I newPos = roomRotation switch
            {
                RoomRotationEnum.None => pos,
                RoomRotationEnum.OneQuarter => new Vector2I(localHeight - 1 - pos.Y, pos.X),
                RoomRotationEnum.Half => new Vector2I(localWidth - 1 - pos.X, localHeight - 1 - pos.Y),
                RoomRotationEnum.ThreeQuarters => new Vector2I(pos.Y, localWidth - 1 - pos.X),
                _ => pos
            };

            rotatedGrid[newPos] = RotateTileValue(tileVal, roomRotation);
        }
        gridMapData = rotatedGrid;
    }

    /// <summary>
    /// Inicializa la habitacion, sus puertas y configura cuáles están conectadas inicialmente.
    /// </summary>
    public void InitializeRoom(RoomTypeEnum type, Door otherDoor, List<Vector2I> connectedDoors = null, bool activateTP = false)
    {
        if (isInitialized) return;
        isInitialized = true;

        if (doorFolder != null)
        {
            foreach (Node child in doorFolder.GetChildren())
            {
                if (child is Door door)
                {
                    Doors.Add(door);
  
                    Vector2I rotatedPos = RotateGridPosition(door.RoomGridPosition, roomRotation, gridWidth, gridHeight);
                    if (connectedDoors != null && otherDoor != null && connectedDoors.Contains(rotatedPos))
                    {                      
                        door.ConnectDoor(otherDoor);
                        otherDoor.InitializeDoorVisuals(activateTP);
                    }
                    door.SetParentRoom(this);
                    door.InitializeDoorVisuals(activateTP);
                }
            }
        }

        SetRoomType(type);
        EmitSignal(SignalName.Signal_RoomInitialized, this);
    }

    /// <summary>
    /// Este metodo rota la habitacion sobre su propio eje.
    /// </summary>
    public void RotateRoom(RoomRotationEnum newRotation)
    {
        roomRotation = newRotation;
        RotateRoomGrid();

        RotationDegrees = new Vector3(0, -90f * (int)newRotation, 0);
    }

    /// <summary>
    /// Obtiene la transformación relativa de un nodo hijo respecto a la habitación.
    /// </summary>
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

    /// <summary>
    /// Establece el estado de actividad de la habitación.
    /// </summary>
    public void SetRoomState(RoomStateEnum state)
    {
        roomState = state;
        Visible = state != RoomStateEnum.Disabled;
        SetDeferred(Node.PropertyName.ProcessMode, state == RoomStateEnum.Active ? (int)ProcessModeEnum.Always : (int)ProcessModeEnum.Disabled);
    }

    /// <summary>
    /// Devuelve una lista de puertas disponibles que no están conectadas.
    /// </summary>
    public List<Door> GetAvailableDoorsForConnection() => Doors.Where(d => d.GetAvailableForConnection()).ToList();

    /// <summary>
    /// Configura el tipo de habitación y actualiza su visibilidad visual.
    /// </summary>
    public void SetRoomType(RoomTypeEnum type)
    {
        roomType = type;
        if (endSprite != null) endSprite.Visible = (type == RoomTypeEnum.End);
        if (startSprite != null) startSprite.Visible = (type == RoomTypeEnum.Start);
        if (mainSprite != null) mainSprite.Visible = (type == RoomTypeEnum.Main);
        if (loopSprite != null) loopSprite.Visible = (type == RoomTypeEnum.Branch);
    }

    /// <summary>
    /// Rota una posición de cuadrícula local según la rotación especificada.
    /// </summary>
    public Vector2I RotateGridPosition(Vector2I pos, RoomRotationEnum rotation, int w, int h)
    {
        return rotation switch
        {
            RoomRotationEnum.None => pos,
            RoomRotationEnum.OneQuarter => new Vector2I(h - 1 - pos.Y, pos.X),
            RoomRotationEnum.Half => new Vector2I(w - 1 - pos.X, h - 1 - pos.Y),
            RoomRotationEnum.ThreeQuarters => new Vector2I(pos.Y, w - 1 - pos.X),
            _ => pos
        };
    }

    /// <summary>
    /// Lisitener para cuando el personaje entra en el Area3D de la habitacion.
    /// </summary>
    private void On_PlayerEnter(Node3D body)
    {
        if (body.Name == "Character")
        {
            EmitSignal(SignalName.Signal_SetRoomStates, this);
        }
    }
}