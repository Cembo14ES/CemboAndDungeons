using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class DynamicGenTest : Node3D
{
    // SIGNALS
    [Signal] public delegate void Signal_LevelGenerationCompletedEventHandler();
    [Signal] public delegate void Signal_RoomPlacedEventHandler(Room room); //Señal que se emite cuando se coloca una habitacion.

    [ExportGroup("Room Scenes")]
    [Export] public Node3D roomFolder; //El Nodo que contiene todas las escenas de Habitaciones.
    [Export] public PackedScene startRoomScene; //La Habitacion de inicio.
    [Export] public PackedScene endRoomScene; //La Habitacion final.
    [Export] public Godot.Collections.Array<PackedScene> ExportStandardRoomScenes; //Un grupo de Habitaciones que pueden ser colocadas.
    public Godot.Collections.Array<PackedScene> standardRoomScenes = new Godot.Collections.Array<PackedScene>();

    [ExportGroup("Seed")]
    [Export] public bool useSeed = false; //Determina si se va a usar una Seed para la generacion.
    [Export] public ulong generationSeed = 0; //La Seed usada.

    [ExportGroup("Path Length")] //El numero minimo y maximo de habitaciones posible.
    [Export] public int minMainPathLength = 4;
    [Export] public int maxMainPathLength = 8;

    [ExportGroup("Accumulate Doors Settings")]
    [Export] public bool accumulatePreviousDoorsForRoomPlacement = true; //Esta variable determina si las puertas de habitaciones anteriores son validas para conexiones
    [Export] public int minRoomDistance = 3;
    [Export] public int maxRoomDistance = 6;

    [ExportGroup("Others")]
    [Export] public float roomSpacing = 0.0f;


    public List<Room> placedRooms = new List<Room>(); //Contiene todas las habitaciones colocadas.
    private RandomNumberGenerator _rng; //La variable RNG
    private Dictionary<Vector2I, int> gridMap = new Dictionary<Vector2I, int>(); //Un tablero que contiene una representacion visual simple de la mazmorra

    private Godot.Collections.Array<Door> previousRoomDoors = new Godot.Collections.Array<Door>();

    public struct EndCandidate
    {
        public Door prevDoor;
        public PackedScene endPrefab;
        public Room.RoomRotationEnum rotation;
        public Vector2I roomOrigin;
        public Dictionary<Vector2I, int> tempGrid;
        public Vector2I localDoorPos;
        private int distance;
        public int Distance { get => distance; set => distance = value; }
    }

    /// <summary>
    /// Método asíncrono principal para generar todo el nivel proceduralmente.
    /// </summary>
    /// <returns>Task indicando éxito o fracaso</returns>
    public async Task<bool> GenerateLevel()
    {
        bool success = await Task.Run(() =>
        {
            //Inicizlizacion del RNG
            _rng = new RandomNumberGenerator();
            if (useSeed) _rng.Seed = generationSeed;
            else _rng.Randomize();

            int targetPathLength = _rng.RandiRange(minMainPathLength, maxMainPathLength);

            foreach (var room in ExportStandardRoomScenes)
            {
                standardRoomScenes.Add(room);
            }

            //Creacion de la primera habitacion
            Room startRoom = startRoomScene.Instantiate<Room>();

            Vector2I startRoomOriginTile = new Vector2I(-startRoom.gridWidth / 2, -startRoom.gridHeight / 2);
            startRoom.gridMapOrigin = startRoomOriginTile; //Se asigna ANTES de InitializeRoom: las puertas calculan su GlobalGridPosition a partir de este valor.
            startRoom.InitializeRoom(Room.RoomTypeEnum.Start, null, null, roomSpacing != 0);

            startRoom.Position = new Vector3(startRoomOriginTile.X * 2.0f, 0, startRoomOriginTile.Y * 2.0f);
            placedRooms.Add(startRoom);

            foreach (var gridKeyValuePair in startRoom.gridMapData) //Añade la habitacion de inicio al tablero
            {
                gridMap[startRoomOriginTile + gridKeyValuePair.Key] = gridKeyValuePair.Value;
            }

            foreach (Door door in startRoom.Doors) //Añade las puertas de la habitacion de inicio a los candidatos de conexion.
            {
                previousRoomDoors.Add(door);
            }

            GD.Print($"[Generation Test] Initialized Start Room. AccumulateDoors Enabled: {accumulatePreviousDoorsForRoomPlacement}, Target Path Length: {targetPathLength} (Bounds: [{minMainPathLength} - {maxMainPathLength}])");


            return RunMainPathGeneration(targetPathLength);

        });

        if (success)
        {
            //Añade todas las habitaciones.
            foreach (Room room in placedRooms)
            {
                roomFolder.AddChild(room);
            }

            EmitSignal(SignalName.Signal_LevelGenerationCompleted);
            GD.Print("[PMD Gen] Level generation finalized successfully.");
        }
        return success;
    }

    /// <summary>
    /// Ejecuta la generación lineal de habitaciones.
    /// </summary>
    private bool RunMainPathGeneration(int targetPathLength)
    {
        GD.Print("[Generation Test] Starting Main Path Generation");

        for (int step = 1; step < targetPathLength; step++)
        {
            ShuffleList(standardRoomScenes);
            bool placed = false;
            foreach (var roomPrefab in standardRoomScenes)
            {
                if (TryPlaceRandomStandardRoom(roomPrefab, Room.RoomTypeEnum.Main, out _))
                {
                    placed = true;
                    break;
                }
                else
                {
                    GD.PrintErr("[Generation Test] Room Could Not Be Placed");
                }
            }
            if (!placed) return false;
        }

        endRoomTest(placedRooms[0]);

        bool endPlaced = false;

        if (!accumulatePreviousDoorsForRoomPlacement)
        {
            if (TryPlaceRandomStandardRoom(endRoomScene, Room.RoomTypeEnum.End, out _))
                endPlaced = true;
            else
                GD.PrintErr("[Generation Test] Final Room Could Not Be Placed");
        }
        else
        {
            var candidates = FindAllEndCandidates();
            GD.Print($"[PMD Branching] Evaluated end candidates. Total valid spots: {candidates.Count}");

            if (candidates.Count > 0)
            {
                var ideal = candidates.Where(c => c.Distance >= minRoomDistance && c.Distance <= maxRoomDistance).ToList();
                if (ideal.Count > 0)
                {
                    var chosen = ideal[_rng.RandiRange(0, ideal.Count - 1)];
                    GD.Print($"[PMD Branching] Placing End Room at ideal distance: {chosen.Distance}");
                    CommitEndCandidate(chosen);
                    endPlaced = true;
                }
                else
                {
                    var best = candidates.OrderByDescending(c => c.Distance).First();
                    GD.Print($"[PMD Branching] Placing End Room at fallback furthest distance: {best.Distance}");
                    CommitEndCandidate(best);
                    endPlaced = true;
                }
            }
            else
            {
                GD.PrintErr("[PMD Branching] Error: No valid End Room candidates found.");
                endPlaced = false;
            }
        }

        return endPlaced;
    }

    /// <summary>
    /// Testing method for a more optimal end room calculation (Not finished, using old system for now)
    /// </summary>
    /// <param name="startRoom"></param>
    private void endRoomTest(Room startRoom)
    {
        Dictionary<Room, int> validRooms = new Dictionary<Room, int>(); //Almacena las habitaciones validas, y su distancia.
        Dictionary<Room, int> invalidRooms = new Dictionary<Room, int>(); //Almacena las habitaciones que no son validas, en caso de que no exista ninguna valida.
        foreach (Room room in placedRooms)
        {
            if (room.GetAvailableDoorsForConnection().Count != 0)
            {
                int distance = GetIntermediateRoomCount(startRoom, room);
                GD.Print($"[endRoomTest]: Distance Between the Start Room and Room {room.Name}: {distance}");

                if (distance > minRoomDistance && distance < maxRoomDistance)
                {

                    GD.Print($"[endRoomTest]: {room.Name} is valid for end room placement.");
                    validRooms.Add(room, distance);
                }
                else
                {   
                    invalidRooms.Add(room, distance);                  
                    GD.Print($"[endRoomTest]: {room.Name} is NOT valid for end room placement. Saved just in case");
                }
            }
            else
            {
                GD.Print($"[endRoomTest]: {room.Name} has no doors available.");
            }

        }
    }

    /// <summary>
    /// Intenta colocar una habitación estándar de manera aleatoria entre las puertas disponibles.
    /// </summary>
    private bool TryPlaceRandomStandardRoom(PackedScene roomPrefab, Room.RoomTypeEnum roomType, out Room newRoom)
    {
        newRoom = null;
        if (previousRoomDoors.Count == 0) return false;

        ShuffleList(previousRoomDoors);

        foreach (Door doorCandidate in previousRoomDoors)
        {
            if (TryConnectRoomToDoor(roomPrefab, doorCandidate, roomType, out newRoom))
            {
                return true;
            }
        }
        GD.Print($"[Generation Test] TryPlaceRandomStandardRoom failed.");
        return false;
    }

    /// <summary>
    /// Intenta colocar y conectar una habitación en base a una puerta previa registrada.
    /// </summary>
    private bool TryConnectRoomToDoor(PackedScene roomPrefab, Door door, Room.RoomTypeEnum roomType, out Room newRoom)
    {
        newRoom = roomPrefab.Instantiate<Room>();

        //Crea una lista de orden aleatorio de todas las rotaciones posibles.
        List<Room.RoomRotationEnum> rotations = Enum.GetValues(typeof(Room.RoomRotationEnum)).Cast<Room.RoomRotationEnum>().ToList();
        Godot.Collections.Array<Room.RoomRotationEnum> arrayRotations = new Godot.Collections.Array<Room.RoomRotationEnum>(rotations);
        ShuffleList(arrayRotations);

        int requiredOppositeDoorType = GetOppositeDoorType(door.GetRotatedTileType()); //Obtiene la rotacion opuesta de la rotacion de la puerta a ser conectada.

        foreach (var rotation in arrayRotations) //Itera por cada rotacion (en orden aleatorio).
        {
            newRoom.RotateRoom(rotation);

            //Obtiene la posicion de tablero de todas las puertas de la habitcion que se pueden conectar a la otra puerta.
            List<Vector2I> matchingDoorPositions = newRoom.gridMapData.Where(gridKeyValuePair => gridKeyValuePair.Value == requiredOppositeDoorType).Select(gridKeyValuePair => gridKeyValuePair.Key).ToList();
            if (matchingDoorPositions.Count == 0) continue; //Si no hay puertas a conectar, aborta.

            //Aleatoriza el orden de las puertas posibles.
            Godot.Collections.Array<Vector2I> shuffledDoorPositions = new Godot.Collections.Array<Vector2I>(matchingDoorPositions);
            ShuffleList(shuffledDoorPositions);

            //Itera por cada puerta que se puede conectar.
            foreach (Vector2I roomDoorLocalPos in shuffledDoorPositions)
            {
                Vector2I roomOrigin = door.GlobalGridPosition + GetGridDirectionFromTileType(door.GetRotatedTileType()) - roomDoorLocalPos;

                //Crea una copia temporal del tablero para evitar romperlo.
                Dictionary<Vector2I, int> tempGridMapCopy = new Dictionary<Vector2I, int>(gridMap);
                bool hasOverlap = false;

                //Itera por cada casilla de la habitacion.
                foreach (var gridKeyValuePair in newRoom.gridMapData)
                {
                    Vector2I tileGridMapPos = roomOrigin + gridKeyValuePair.Key; //Calcula la posicion de la casilla de a habitacion en el tablero global
                    if (tempGridMapCopy.ContainsKey(tileGridMapPos) && tempGridMapCopy[tileGridMapPos] > 0) //Comprueba si esa casilla ya existe en el tablero global.
                    {
                        hasOverlap = true;
                        break;
                    }
                    tempGridMapCopy[tileGridMapPos] = gridKeyValuePair.Value; //Si la casilla no existe en el tablero global, la añade.
                }

                if (hasOverlap)
                {
                    GD.Print($"[Generation Test]: FAIL: Room could not be placed due to Overlap. '{roomPrefab.ResourcePath.GetFile()}' with rotation {rotation} at origin {roomOrigin} connecting to door at {door.GlobalGridPosition}.");
                    continue;
                }

                // En este punto, la habitacion ya es valida y sera colocada en esa posicion.
                GD.Print($"[Generation Test] Placed room '{roomPrefab.ResourcePath.GetFile()}' with rotation {rotation} at origin {roomOrigin} connecting to door at {door.GlobalGridPosition}.");
                gridMap = tempGridMapCopy;

                newRoom.gridMapOrigin = roomOrigin;
                newRoom.InitializeRoom(roomType, door, new List<Vector2I> { roomDoorLocalPos }, roomSpacing != 0);

                AlignAndConnectRooms(door.GetParentRoom(), door, newRoom, roomDoorLocalPos, rotation);

                placedRooms.Add(newRoom);

                //Dependiendo si queremos acumular puertas para conexiones o no, se vacia la lista o solo se elimina la recien conectada puerta.
                if (!accumulatePreviousDoorsForRoomPlacement) previousRoomDoors.Clear();
                else previousRoomDoors.Remove(door);

                //Añade las puertas de la nueva abitacion a las puertas disponibles (menos la que acaba de conectarse).
                foreach (Door newDoor in newRoom.Doors)
                {
                    if (newDoor.GetIsConnected()) continue;
                    previousRoomDoors.Add(newDoor);
                }

                //Descarta las puertas que ya no pueden conectarse a nada (tienen un muro enfrente).
                validateAllDoorsForConnection();

                return true;
            }
        }

        newRoom.QueueFree();
        newRoom = null;
        return false;
    }

    /// <summary>
    /// Alinea espacialmente una habitación con otra previa y conecta sus puertas.
    /// </summary>
    private void AlignAndConnectRooms(Room placedRoom, Door placedDoor, Room newRoom, Vector2I newLocalDoorPos, Room.RoomRotationEnum newRoomRotation)
    {
        Door newDoorObj = newRoom.Doors.FirstOrDefault(d => newRoom.RotateGridPosition(d.RoomGridPosition, newRoomRotation, newRoom.gridWidth, newRoom.gridHeight) == newLocalDoorPos);

        if (placedDoor != null && newDoorObj != null)
        {
            Vector3 prevDoorWorldPos = (placedRoom.Transform * placedRoom.GetRelativeTransform(placedDoor)).Origin;
            newRoom.Position = Vector3.Zero;
            Vector3 newDoorWorldPos = (newRoom.Transform * newRoom.GetRelativeTransform(newDoorObj)).Origin;

            Vector3 spacingOffset = placedDoor.cardinalDirection switch
            {
                Door.DoorDirection.North => new Vector3(0, 0, -roomSpacing),
                Door.DoorDirection.South => new Vector3(0, 0, roomSpacing),
                Door.DoorDirection.East => new Vector3(roomSpacing, 0, 0),
                Door.DoorDirection.West => new Vector3(-roomSpacing, 0, 0),
                _ => Vector3.Zero
            };

            newRoom.Position = prevDoorWorldPos + spacingOffset - newDoorWorldPos;

            //  GD.Print("Test Start");
            if (!placedRoom.ConnectedRooms.Contains(newRoom))
            {
                placedRoom.ConnectedRooms.Add(newRoom);
                //GD.Print("Test room 1 added.");
                //GD.Print("Number of conected rooms: ");
            }
            if (!newRoom.ConnectedRooms.Contains(placedRoom))
            {
                newRoom.ConnectedRooms.Add(placedRoom);
                //GD.Print("Test room 2 added.");
            }
            //GD.Print("Test End");
        }
    }

    /// <summary>
    /// Encuentra todos los candidatos posibles para ubicar la habitación final.
    /// </summary>
    private List<EndCandidate> FindAllEndCandidates()
    {
        List<EndCandidate> candidates = new List<EndCandidate>();
        ShuffleList(previousRoomDoors);

        foreach (Door prevDoor in previousRoomDoors)
        {
            Room testEndRoom = endRoomScene.Instantiate<Room>();

            List<Room.RoomRotationEnum> rotations = Enum.GetValues(typeof(Room.RoomRotationEnum)).Cast<Room.RoomRotationEnum>().ToList();
            Godot.Collections.Array<Room.RoomRotationEnum> arrayRotations = new Godot.Collections.Array<Room.RoomRotationEnum>(rotations);
            ShuffleList(arrayRotations);

            int requiredOppositeDoorType = GetOppositeDoorType(prevDoor.GetRotatedTileType());
            bool foundForThisDoor = false;

            foreach (var rot in arrayRotations)
            {
                if (foundForThisDoor) break;
                testEndRoom.RotateRoom(rot);
                Godot.Collections.Dictionary<Vector2I, int> rotatedGrid = testEndRoom.gridMapData;

                List<Vector2I> matchingDoorPositions = rotatedGrid.Where(kvp => kvp.Value == requiredOppositeDoorType).Select(kvp => kvp.Key).ToList();

                foreach (Vector2I roomDoorLocalPos in matchingDoorPositions)
                {
                    Vector2I roomOrigin = prevDoor.GlobalGridPosition + GetGridDirectionFromTileType(prevDoor.GetRotatedTileType()) - roomDoorLocalPos;

                    int expectedRooms = gridMap.Values.Count(v => IsRoomTile(v)) + rotatedGrid.Values.Count(v => IsRoomTile(v));
                    int expectedDoors = gridMap.Values.Count(v => IsDoorTile(v)) + rotatedGrid.Values.Count(v => IsDoorTile(v));

                    Dictionary<Vector2I, int> tempGrid = new Dictionary<Vector2I, int>(gridMap);
                    bool hasOverlap = false;

                    foreach (var kvp in rotatedGrid)
                    {
                        Vector2I globalPos = roomOrigin + kvp.Key;
                        if (tempGrid.ContainsKey(globalPos) && tempGrid[globalPos] > 0) { hasOverlap = true; break; }
                        tempGrid[globalPos] = kvp.Value;
                    }

                    if (hasOverlap) continue;
                    if (tempGrid.Values.Count(v => IsRoomTile(v)) != expectedRooms || tempGrid.Values.Count(v => IsDoorTile(v)) != expectedDoors) continue;

                    int dist = -1;
                    if (prevDoor.GetParentRoom() != null)
                    {
                        prevDoor.GetParentRoom().ConnectedRooms.Add(testEndRoom);
                        testEndRoom.ConnectedRooms.Add(prevDoor.GetParentRoom());

                        dist = GetIntermediateRoomCount(placedRooms[0], testEndRoom);

                        prevDoor.GetParentRoom().ConnectedRooms.Remove(testEndRoom);
                        testEndRoom.ConnectedRooms.Remove(prevDoor.GetParentRoom());
                    }

                    candidates.Add(new EndCandidate
                    {
                        prevDoor = prevDoor,
                        endPrefab = endRoomScene,
                        rotation = rot,
                        roomOrigin = roomOrigin,
                        tempGrid = tempGrid,
                        localDoorPos = roomDoorLocalPos,
                        Distance = dist
                    });

                    foundForThisDoor = true;
                    break;
                }
            }
            testEndRoom.QueueFree();
        }
        return candidates;
    }

    /// <summary>
    /// Confirma la colocación del candidato elegido para la habitación final.
    /// </summary>
    private bool CommitEndCandidate(EndCandidate candidate)
    {
        Room endRoom = endRoomScene.Instantiate<Room>();
        endRoom.RotateRoom(candidate.rotation);
        endRoom.gridMapOrigin = candidate.roomOrigin;
        endRoom.InitializeRoom(Room.RoomTypeEnum.End, candidate.prevDoor, new List<Vector2I> { candidate.localDoorPos }, roomSpacing != 0);

        gridMap = candidate.tempGrid;

        if (candidate.prevDoor.GetParentRoom() != null)
        {
            AlignAndConnectRooms(candidate.prevDoor.GetParentRoom(), candidate.prevDoor, endRoom, candidate.localDoorPos, candidate.rotation);
        }
        else
        {
            endRoom.Position = new Vector3(candidate.roomOrigin.X * 2.0f, 0, candidate.roomOrigin.Y * 2.0f);
        }

        placedRooms.Add(endRoom);
        return true;
    }

    /// <summary>
    /// Obtiene el número de habitaciones intermedias entre el inicio y el objetivo.
    /// </summary>
    private int GetIntermediateRoomCount(Room start, Room target)
    {
        if (start == target) return -1;
        Queue<Room> queue = new Queue<Room>();
        Dictionary<Room, int> distances = new Dictionary<Room, int>();

        queue.Enqueue(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
            {
                return distances[target] - 1;
            }

            foreach (var neighbor in current.ConnectedRooms)
            {
                if (!distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = distances[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Compruebas si una casilla es una puerta.
    /// </summary>
    /// <param name="val">El valor de la casilla</param>
    /// <returns></returns>
    private bool IsDoorTile(int val) => val >= 2 && val <= 5;

    /// <summary>
    /// Comprueba si una casilla es parte de una habitacion. Las casillas de una habitacion son los suelos, paredes y esquinas
    /// </summary>
    /// <param name="val">El valor de la casilla</param>
    /// <returns></returns>
    private bool IsRoomTile(int val) => val == 1 || (val >= 6 && val <= 13);

    /// <summary>
    /// Obtiene el tipo de puerta opuesto requerido para la conexión.
    /// </summary>
    private int GetOppositeDoorType(int doorType) => doorType switch
    {
        2 => 4,
        3 => 5,
        4 => 2,
        5 => 3,
        _ => 2
    };

    /// <summary>
    /// Obtiene el vector de dirección en cuadrícula asociado a un tipo de puerta.
    /// </summary>
    private Vector2I GetGridDirectionFromTileType(int doorType) => doorType switch
    {
        2 => new Vector2I(0, -1),
        3 => new Vector2I(1, 0),
        4 => new Vector2I(0, 1),
        5 => new Vector2I(-1, 0),
        _ => Vector2I.Zero
    };

    /// <summary>
    /// Elimina de las puertas disponibles las que ya no pueden conectarse a nada: si la casilla que tienen enfrente ya esta ocupada
    /// (por un muro u otra habitacion), cualquier habitacion nueva se solaparia con ella. Como el tablero solo crece, esas puertas no pueden volver a ser validas.
    /// </summary>
    private void validateAllDoorsForConnection()
    {
        for (int i = previousRoomDoors.Count - 1; i >= 0; i--) //Se recorre al reves para poder quitar puertas mientras se itera.
        {
            Door door = previousRoomDoors[i];
            Vector2I frontTile = door.GlobalGridPosition + GetGridDirectionFromTileType(door.GetRotatedTileType());

            if (gridMap.TryGetValue(frontTile, out int tileValue) && tileValue > 0)
            {
                door.SetInvalidForConnection();
                previousRoomDoors.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Desordena todos los elemento de una lista usando el RNG propio.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array">La lista a ser desordenada</param>
    private void ShuffleList<[MustBeVariant] T>(Godot.Collections.Array<T> array)
    {
        int n = array.Count;
        while (n > 1)
        {
            n--;
            int k = _rng.RandiRange(0, n);
            T value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
    }
}