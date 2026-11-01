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

    //Contiene el tablero con la informacion del personaje
    [Export] public Godot.Collections.Array<int> gridMapData = new Godot.Collections.Array<int>();

    public List<Door> Doors { get; private set; } = new List<Door>(); //Las puertas de la habitacion
    public List<Room> ConnectedRooms { get; private set; } = new List<Room>(); //Las habitaciones conectadas a esta.

    private RoomRotationEnum roomRotation = RoomRotationEnum.None; //La rotacion que posee la habitacion.
    private RoomStateEnum roomState = RoomStateEnum.Disabled; //El estado de actividad de la habitacion.
    public RoomTypeEnum roomType = RoomTypeEnum.Regular; //El tipo de habitacion

    public enum RoomRotationEnum { None = 0, OneQuarter = 1, Half = 2, ThreeQuarters = 3 } //La rotacion que posee la habitacion.
    public enum RoomStateEnum { Active, Inactive, Disabled } //El estado de actividad de la habitacion.
    public enum RoomTypeEnum { Regular, Start, End, Main, Branch } //El tipo de habitacion
    private bool isInitialized = false; //Determina si la habitacion ya ha sido inicializada.

    /// <summary>
    /// Getter para la rotacion de la habitacion
    /// </summary>
    /// <returns>Rotación actual de la habitación</returns>
    public RoomRotationEnum GetCurrentRotation() => roomRotation;

    /// <summary>
    /// Rota el valor numérico de una casilla de puerta según la rotación de la habitación.
    /// </summary>
    /// <param name="tileVal">Valor numérico de la baldosa</param>
    /// <param name="rotation">Rotación aplicada</param>
    /// <returns>Valor de baldosa rotado</returns>
    public static int RotateTileValue(int tileVal, RoomRotationEnum rotation)
    {
        if (tileVal < 2 || tileVal > 5) return tileVal;
        int shift = (int)rotation;
        int baseDir = tileVal - 2;
        return ((baseDir + shift) % 4) + 2;
    }

    /// <summary>
    /// Este metodo calcula y devuelve el roomGridMap de la habitacion rotado a la rotacion necesaria.
    /// </summary>
    /// <param name="rotation">La rotacion a calcular</param>
    /// <returns>Un roomGridMap con la rotacion aplicada</returns>
    public Dictionary<Vector2I, int> GetRotatedRoomGrid(RoomRotationEnum rotation)
    {
        Dictionary<Vector2I, int> rotatedGrid = new Dictionary<Vector2I, int>();
        int localWidth = gridWidth;
        int localHeight = gridHeight;

        if (gridMapData == null || gridMapData.Count != localWidth * localHeight) return rotatedGrid;

        for (int y = 0; y < localHeight; y++)
        {
            for (int x = 0; x < localWidth; x++)
            {
                int idx = (y * localWidth) + x;
                int tileVal = gridMapData[idx];
                if (tileVal <= 0) continue;

                Vector2I newPos = rotation switch
                {
                    RoomRotationEnum.None => new Vector2I(x, y),
                    RoomRotationEnum.OneQuarter => new Vector2I(localHeight - 1 - y, x),
                    RoomRotationEnum.Half => new Vector2I(localWidth - 1 - x, localHeight - 1 - y),
                    RoomRotationEnum.ThreeQuarters => new Vector2I(y, localWidth - 1 - x),
                    _ => new Vector2I(x, y)
                };

                rotatedGrid[newPos] = RotateTileValue(tileVal, rotation);
            }
        }
        return rotatedGrid;
    }

    /// <summary>
    /// Inicializa la habitacion, sus puertas y configura cuáles están conectadas inicialmente.
    /// </summary>
    /// <param name="type">El tipo de habitacion que es.</param>
    /// <param name="connectedDoors">Lista opcional de posiciones locales rotadas de puertas que deben iniciar conectadas.</param>
    /// <param name="activateTP">Si el teletransporte debe activarse.</param>
    public void InitializeRoom(RoomTypeEnum type, List<Vector2I> connectedDoors = null, bool activateTP = false)
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
                    door.ParentRoom = this;

                    Vector2I rotatedPos = RotateGridPosition(door.RoomGridPosition, roomRotation, gridWidth, gridHeight);
                    bool isConnected = connectedDoors != null && connectedDoors.Contains(rotatedPos);
                    door.SetConnected(isConnected);
                    door.InitializeDoor(activateTP);
                }
            }
        }

        SetRoomType(type);



        EmitSignal(SignalName.Signal_RoomInitialized, this);
    }

    /// <summary>
    /// Actualiza una sola puerta de la habitación que ya ha sido inicializada previamente.
    /// </summary>
    /// <param name="localDoorPos">Posición local rotada de la puerta a actualizar</param>
    /// <param name="connected">Nuevo estado de conexión</param>
    /// <param name="connectedToDoor">Puerta de destino conectada</param>
    /// <param name="activateTP">Si se debe activar el teletransporte</param>
    public void UpdateDoor(Vector2I localDoorPos, bool connected, Door connectedToDoor, bool activateTP)
    {
        Door door = Doors.FirstOrDefault(d => RotateGridPosition(d.RoomGridPosition, roomRotation, gridWidth, gridHeight) == localDoorPos);
        if (door != null)
        {
            door.SetConnected(connected);
            door.ConnectedTo = connectedToDoor;
            door.InitializeDoor(activateTP);
        }
    }

    /// <summary>
    /// Este metodo rota la habitacion sobre su propio eje.
    /// </summary>
    /// <param name="newRotation">La nueva rotacion de la habitacion.</param>
    public void RotateRoom(RoomRotationEnum newRotation)
    {
        roomRotation = newRotation;
        RotationDegrees = new Vector3(0, -(int)newRotation * 90, 0);
    }

    /// <summary>
    /// Obtiene la transformación relativa de un nodo hijo respecto a la habitación.
    /// </summary>
    /// <param name="childNode">Nodo hijo a evaluar</param>
    /// <returns>Transform3D resultante</returns>
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
    /// <param name="state">Nuevo estado de la habitación</param>
    public void SetRoomState(RoomStateEnum state)
    {
        roomState = state;
        Visible = state != RoomStateEnum.Disabled;
        SetDeferred(Node.PropertyName.ProcessMode, state == RoomStateEnum.Active ? (int)ProcessModeEnum.Always : (int)ProcessModeEnum.Disabled);
    }

    /// <summary>
    /// Devuelve una lista de puertas disponibles que no están conectadas.
    /// </summary>
    /// <returns>Lista de puertas disponibles</returns>
    public List<Door> GetAvailableDoors() => Doors.Where(d => !d.isConnected).ToList();

    /// <summary>
    /// Configura el tipo de habitación y actualiza su visibilidad visual.
    /// </summary>
    /// <param name="type">Tipo de habitación a establecer</param>
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
    /// <param name="pos">Posición a rotar</param>
    /// <param name="rotation">Rotación aplicada</param>
    /// <param name="w">Ancho de la cuadrícula</param>
    /// <param name="h">Alto de la cuadrícula</param>
    /// <returns>Posición rotada</returns>
    private Vector2I RotateGridPosition(Vector2I pos, RoomRotationEnum rotation, int w, int h)
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
    /// <param name="body">Cuerpo que entra al área</param>
    private void On_PlayerEnter(Node3D body)
    {
        if (body.Name == "Character")
        {
            EmitSignal(SignalName.Signal_SetRoomStates, this); //Emite una señal para actualizar el estado de la habitacion.
        }
    }
}