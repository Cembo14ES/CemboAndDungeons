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
    [Export] public Godot.Collections.Array<PackedScene> standardRoomScenes; //Un grupo de Habitaciones que pueden ser colocadas.

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
    
    public struct DoorRecord
    {
        public Vector2I position;
        public int tileType; 
        public Room room;
        public Vector2I localDoorPos;
    }
    private List<DoorRecord> previousRoomDoors = new List<DoorRecord>();

    public struct EndCandidate
    {
        public DoorRecord prevDoor;
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
            //Inicializacion de la variable RNG
            _rng = new RandomNumberGenerator();
            if (useSeed) _rng.Seed = generationSeed;
            else _rng.Randomize();

            //Coloca la habitacion de Inicio en el origen del mundo
            Room startRoom = startRoomScene.Instantiate<Room>();
            startRoom.RotateRoom(Room.RoomRotationEnum.None);
            startRoom.InitializeRoom(Room.RoomTypeEnum.Start, null, roomSpacing != 0);

            Dictionary<Vector2I, int> startGrid = startRoom.GetRotatedRoomGrid(Room.RoomRotationEnum.None);
            Vector2I startOrigin = new Vector2I(-startRoom.gridWidth / 2, -startRoom.gridHeight / 2);
            
            foreach (var kvp in startGrid)
            {
                gridMap[startOrigin + kvp.Key] = kvp.Value;
            }

            startRoom.Position = new Vector3(startOrigin.X * 2.0f, 0, startOrigin.Y * 2.0f);
            placedRooms.Add(startRoom);

            foreach (Door door in startRoom.Doors)
            {
                Vector2I doorLocalPos = door.RoomGridPosition;
                int tileVal = startGrid.ContainsKey(doorLocalPos) ? startGrid[doorLocalPos] : DoorDirectionToTileType(door.cardinalDirection);
                previousRoomDoors.Add(new DoorRecord { 
                    position = startOrigin + doorLocalPos, 
                    tileType = tileVal, 
                    room = startRoom, 
                    localDoorPos = doorLocalPos 
                });
            }

            int targetPathLength = _rng.RandiRange(minMainPathLength, maxMainPathLength);
            GD.Print($"[PMD Gen] Initialized Start Room. AccumulateDoors: {accumulatePreviousDoorsForRoomPlacement}, Target Path Length: {targetPathLength} (Bounds: [{minMainPathLength} - {maxMainPathLength}])");

            if (!accumulatePreviousDoorsForRoomPlacement)
            {
                return RunLinearGeneration(targetPathLength);
            }
            else
            {
                return RunBranchingGeneration(targetPathLength);
            }
        });

        if (success) 
        {
            FinalizeLevel();
            EmitSignal(SignalName.Signal_LevelGenerationCompleted);
            GD.Print("[PMD Gen] Level generation finalized successfully.");
        }
        return success; 
    }

    /// <summary>
    /// Ejecuta la generación lineal de habitaciones.
    /// </summary>
    /// <param name="targetPathLength">Longitud objetivo del camino</param>
    /// <returns>True si se completó con éxito</returns>
    private bool RunLinearGeneration(int targetPathLength)
    {
        GD.Print("[PMD Linear] Starting strict linear path generation...");
        for (int step = 1; step < targetPathLength; step++)
        {
            List<PackedScene> shuffledRooms = new List<PackedScene>(standardRoomScenes);
            ShuffleList(shuffledRooms);
            bool placed = false;
            foreach (var roomPrefab in shuffledRooms)
            {
                if (TryPlaceRandomStandardRoom(roomPrefab, Room.RoomTypeEnum.Main, out _))
                {
                    placed = true;
                    break;
                }
            }
            if (!placed) return false;
        }

        List<PackedScene> endPool = new List<PackedScene> { endRoomScene };
        bool endPlaced = false;
        foreach (var roomPrefab in endPool)
        {
            if (TryPlaceRandomStandardRoom(roomPrefab, Room.RoomTypeEnum.End, out _))
            {
                endPlaced = true;
                break;
            }
        }
        return endPlaced;
    }

    /// <summary>
    /// Ejecuta la generación ramificada de habitaciones (PMD).
    /// </summary>
    /// <param name="targetPathLength">Longitud objetivo del camino principal</param>
    /// <returns>True si se completó con éxito</returns>
    private bool RunBranchingGeneration(int targetPathLength)
    {
        GD.Print("[PMD Branching] Starting branching path generation...");
        int currentStandardRooms = 0;

        while (currentStandardRooms < targetPathLength && previousRoomDoors.Count > 0)
        {
            List<PackedScene> shuffledRooms = new List<PackedScene>(standardRoomScenes);
            ShuffleList(shuffledRooms);
            bool roomAdded = false;
            foreach (var roomPrefab in shuffledRooms)
            {
                if (TryPlaceRandomStandardRoom(roomPrefab, Room.RoomTypeEnum.Main, out _))
                {
                    currentStandardRooms++;
                    roomAdded = true;
                    break;
                }
            }
            if (!roomAdded) break;
        }
        GD.Print($"[PMD Branching] Placed {currentStandardRooms} standard rooms.");

        int extensionSafety = 0;
        while (extensionSafety < 30)
        {
            var roomDistances = CalculateRoomDistances(placedRooms[0]);
            int maxDoorDist = 0;
            foreach (var doorRec in previousRoomDoors)
            {
                if (roomDistances.TryGetValue(doorRec.room, out int dist) && dist > maxDoorDist)
                {
                    maxDoorDist = dist;
                }
            }

            GD.Print($"[PMD Branching] Current max open door distance from Start: {maxDoorDist} (Required Min: {minRoomDistance})");
            if (maxDoorDist >= minRoomDistance) break;

            var sortedDoors = previousRoomDoors
                .OrderByDescending(d => roomDistances.ContainsKey(d.room) ? roomDistances[d.room] : 0)
                .ToList();

            bool extended = false;
            List<PackedScene> shuffledRooms = new List<PackedScene>(standardRoomScenes);
            foreach (var furthestDoor in sortedDoors)
            {
                ShuffleList(shuffledRooms);
                foreach (var roomPrefab in shuffledRooms)
                {
                    if (TryPlaceRoomPMD(roomPrefab, furthestDoor, Room.RoomTypeEnum.Main, out _))
                    {
                        currentStandardRooms++;
                        extended = true;
                        GD.Print($"[PMD Branching] Extended path from furthest room '{furthestDoor.room.Name}'. Total standard rooms: {currentStandardRooms}");
                        break;
                    }
                }
                if (extended) break;
            }

            if (!extended) break;
            extensionSafety++;
        }

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
                return true;
            }
            else
            {
                var best = candidates.OrderByDescending(c => c.Distance).First();
                GD.Print($"[PMD Branching] Placing End Room at fallback furthest distance: {best.Distance}");
                CommitEndCandidate(best);
                return true;
            }
        }
        else
        {
            GD.PrintErr("[PMD Branching] Error: No valid End Room candidates found.");
            return false;
        }
    }

    /// <summary>
    /// Calcula las distancias en número de habitaciones desde el nodo de inicio.
    /// </summary>
    /// <param name="start">Habitación inicial</param>
    /// <returns>Diccionario con las distancias de cada habitación</returns>
    private Dictionary<Room, int> CalculateRoomDistances(Room start)
    {
        Dictionary<Room, int> distances = new Dictionary<Room, int>();
        Queue<Room> queue = new Queue<Room>();

        queue.Enqueue(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentDist = distances[current];

            foreach (var neighbor in current.ConnectedRooms)
            {
                if (!distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = currentDist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return distances;
    }

    /// <summary>
    /// Intenta colocar una habitación estándar de manera aleatoria entre las puertas disponibles.
    /// </summary>
    /// <param name="prefab">Escena de la habitación</param>
    /// <param name="roomType">Tipo de habitación</param>
    /// <param name="newRoom">Habitación resultante (out)</param>
    /// <returns>True si se colocó con éxito</returns>
    private bool TryPlaceRandomStandardRoom(PackedScene prefab, Room.RoomTypeEnum roomType, out Room newRoom)
    {
        newRoom = null;
        if (previousRoomDoors.Count == 0) return false;

        List<DoorRecord> shuffledDoors = new List<DoorRecord>(previousRoomDoors);
        ShuffleList(shuffledDoors);

        foreach (DoorRecord prevDoor in shuffledDoors)
        {
            if (TryPlaceRoomPMD(prefab, prevDoor, roomType, out newRoom))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Intenta colocar y conectar una habitación en base a una puerta previa registrada.
    /// </summary>
    /// <param name="prefab">Escena de la habitación</param>
    /// <param name="prevDoor">Registro de la puerta anterior</param>
    /// <param name="roomType">Tipo de habitación</param>
    /// <param name="newRoom">Habitación resultante (out)</param>
    /// <returns>True si se posicionó y conectó correctamente</returns>
    private bool TryPlaceRoomPMD(PackedScene prefab, DoorRecord prevDoor, Room.RoomTypeEnum roomType, out Room newRoom)
    {
        newRoom = prefab.Instantiate<Room>();
        string prefabName = prefab.ResourcePath.GetFile();

        List<Room.RoomRotationEnum> rotations = Enum.GetValues(typeof(Room.RoomRotationEnum)).Cast<Room.RoomRotationEnum>().ToList();
        ShuffleList(rotations);

        int requiredOppositeDoorType = GetOppositeDoorType(prevDoor.tileType);

        foreach (var rot in rotations)
        {
            newRoom.RotateRoom(rot);
            Dictionary<Vector2I, int> rotatedGrid = newRoom.GetRotatedRoomGrid(rot);

            List<Vector2I> matchingDoorPositions = rotatedGrid.Where(kvp => kvp.Value == requiredOppositeDoorType).Select(kvp => kvp.Key).ToList();
            if (matchingDoorPositions.Count == 0) continue;

            ShuffleList(matchingDoorPositions);

            foreach (Vector2I roomDoorLocalPos in matchingDoorPositions)
            {
                Vector2I roomOrigin = (prevDoor.position + GetGridDirectionFromTileType(prevDoor.tileType)) - roomDoorLocalPos;

                int expectedRooms = gridMap.Values.Count(v => IsRoomTile(v)) + rotatedGrid.Values.Count(v => IsRoomTile(v));
                int expectedDoors = gridMap.Values.Count(v => IsDoor(v)) + rotatedGrid.Values.Count(v => IsDoor(v));

                Dictionary<Vector2I, int> tempGrid = new Dictionary<Vector2I, int>(gridMap);
                bool hasOverlap = false;

                foreach (var kvp in rotatedGrid)
                {
                    Vector2I globalPos = roomOrigin + kvp.Key;
                    if (tempGrid.ContainsKey(globalPos) && tempGrid[globalPos] > 0) { hasOverlap = true; break; }
                    tempGrid[globalPos] = kvp.Value;
                }

                if (hasOverlap) continue;

                if (tempGrid.Values.Count(v => IsRoomTile(v)) != expectedRooms || tempGrid.Values.Count(v => IsDoor(v)) != expectedDoors) continue;

                // Success: una vez calculado y validado todo, inicializamos la sala y conectamos
                GD.Print($"[PMD Debug] SUCCESS: Placed room '{prefabName}' with rotation {rot} at origin {roomOrigin} connecting to door at {prevDoor.position}.");
                gridMap = tempGrid;

                // Paso 2: Inicializar la nueva habitación pasando su puerta conectada inicial
                newRoom.InitializeRoom(roomType, new List<Vector2I> { roomDoorLocalPos }, roomSpacing != 0);

                if (prevDoor.room != null)
                {
                    AlignAndConnectRooms(prevDoor.room, prevDoor.localDoorPos, newRoom, roomDoorLocalPos, rot, roomSpacing);
                }

                placedRooms.Add(newRoom);

                if (!accumulatePreviousDoorsForRoomPlacement) previousRoomDoors.Clear();
                else previousRoomDoors.RemoveAll(d => d.room == prevDoor.room && d.localDoorPos == prevDoor.localDoorPos);

                foreach (var doorData in GetRoomDoorData(newRoom, rot))
                {
                    if (doorData.localPos == roomDoorLocalPos) continue;
                    previousRoomDoors.Add(new DoorRecord { 
                        position = roomOrigin + doorData.localPos, 
                        tileType = doorData.tileType, 
                        room = newRoom, 
                        localDoorPos = doorData.localPos 
                    });
                }

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
    /// <param name="prevRoom">Habitación anterior ya colocada</param>
    /// <param name="prevLocalDoorPos">Posición local de la puerta en la habitación anterior</param>
    /// <param name="newRoom">Nueva habitación</param>
    /// <param name="newLocalDoorPos">Posición local de la puerta en la nueva habitación</param>
    /// <param name="newRotation">Rotación de la nueva habitación</param>
    /// <param name="roomSpacing">Espaciado opcional entre salas</param>
    private void AlignAndConnectRooms(Room prevRoom, Vector2I prevLocalDoorPos, Room newRoom, Vector2I newLocalDoorPos, Room.RoomRotationEnum newRotation, float roomSpacing)
    {
        Door prevDoorObj = prevRoom.Doors.FirstOrDefault(d => RotateGridPosition(d.RoomGridPosition, prevRoom.GetCurrentRotation(), prevRoom.gridWidth, prevRoom.gridHeight) == prevLocalDoorPos);
        Door newDoorObj = newRoom.Doors.FirstOrDefault(d => RotateGridPosition(d.RoomGridPosition, newRotation, newRoom.gridWidth, newRoom.gridHeight) == newLocalDoorPos);

        if (prevDoorObj != null && newDoorObj != null)
        {
            Vector3 prevDoorWorldPos = (prevRoom.Transform * prevRoom.GetRelativeTransform(prevDoorObj)).Origin;
            newRoom.Position = Vector3.Zero;
            Vector3 newDoorWorldPos = (newRoom.Transform * newRoom.GetRelativeTransform(newDoorObj)).Origin;

            Vector3 spacingOffset = prevDoorObj.cardinalDirection switch
            {
                Door.DoorDirection.North => new Vector3(0, 0, -roomSpacing),
                Door.DoorDirection.South => new Vector3(0, 0, roomSpacing),
                Door.DoorDirection.East => new Vector3(roomSpacing, 0, 0),
                Door.DoorDirection.West => new Vector3(-roomSpacing, 0, 0),
                _ => Vector3.Zero
            };

            newRoom.Position = prevDoorWorldPos + spacingOffset - newDoorWorldPos;

            // Paso 3: Actualizar la puerta de la habitación previa (que ya estaba inicializada) usando UpdateDoor
            prevRoom.UpdateDoor(prevLocalDoorPos, true, newDoorObj, roomSpacing != 0);
            newDoorObj.ConnectedTo = prevDoorObj;

            if (!prevRoom.ConnectedRooms.Contains(newRoom)) prevRoom.ConnectedRooms.Add(newRoom);
            if (!newRoom.ConnectedRooms.Contains(prevRoom)) newRoom.ConnectedRooms.Add(prevRoom);
        }
    }

    /// <summary>
    /// Encuentra todos los candidatos posibles para ubicar la habitación final.
    /// </summary>
    /// <returns>Lista de candidatos para la habitación final</returns>
    private List<EndCandidate> FindAllEndCandidates()
    {
        List<EndCandidate> candidates = new List<EndCandidate>();
        List<DoorRecord> shuffledDoors = new List<DoorRecord>(previousRoomDoors);
        ShuffleList(shuffledDoors);

        foreach (DoorRecord prevDoor in shuffledDoors)
        {
            Room testEndRoom = endRoomScene.Instantiate<Room>();

            List<Room.RoomRotationEnum> rotations = Enum.GetValues(typeof(Room.RoomRotationEnum)).Cast<Room.RoomRotationEnum>().ToList();
            ShuffleList(rotations);

            int requiredOppositeDoorType = GetOppositeDoorType(prevDoor.tileType);
            bool foundForThisDoor = false;

            foreach (var rot in rotations)
            {
                if (foundForThisDoor) break;
                testEndRoom.RotateRoom(rot);
                Dictionary<Vector2I, int> rotatedGrid = testEndRoom.GetRotatedRoomGrid(rot);

                List<Vector2I> matchingDoorPositions = rotatedGrid.Where(kvp => kvp.Value == requiredOppositeDoorType).Select(kvp => kvp.Key).ToList();
                
                foreach (Vector2I roomDoorLocalPos in matchingDoorPositions)
                {
                    Vector2I roomOrigin = (prevDoor.position + GetGridDirectionFromTileType(prevDoor.tileType)) - roomDoorLocalPos;

                    int expectedRooms = gridMap.Values.Count(v => IsRoomTile(v)) + rotatedGrid.Values.Count(v => IsRoomTile(v));
                    int expectedDoors = gridMap.Values.Count(v => IsDoor(v)) + rotatedGrid.Values.Count(v => IsDoor(v));

                    Dictionary<Vector2I, int> tempGrid = new Dictionary<Vector2I, int>(gridMap);
                    bool hasOverlap = false;

                    foreach (var kvp in rotatedGrid)
                    {
                        Vector2I globalPos = roomOrigin + kvp.Key;
                        if (tempGrid.ContainsKey(globalPos) && tempGrid[globalPos] > 0) { hasOverlap = true; break; }
                        tempGrid[globalPos] = kvp.Value;
                    }

                    if (hasOverlap) continue;
                    if (tempGrid.Values.Count(v => IsRoomTile(v)) != expectedRooms || tempGrid.Values.Count(v => IsDoor(v)) != expectedDoors) continue;

                    int dist = -1;
                    if (prevDoor.room != null)
                    {
                        prevDoor.room.ConnectedRooms.Add(testEndRoom);
                        testEndRoom.ConnectedRooms.Add(prevDoor.room);

                        dist = GetIntermediateRoomCount(placedRooms[0], testEndRoom);

                        prevDoor.room.ConnectedRooms.Remove(testEndRoom);
                        testEndRoom.ConnectedRooms.Remove(prevDoor.room);
                    }

                    candidates.Add(new EndCandidate {
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
    /// <param name="candidate">Candidato seleccionado</param>
    /// <returns>True si se colocó correctamente</returns>
    private bool CommitEndCandidate(EndCandidate candidate)
    {
        Room endRoom = endRoomScene.Instantiate<Room>();
        endRoom.RotateRoom(candidate.rotation);
        endRoom.InitializeRoom(Room.RoomTypeEnum.End, new List<Vector2I> { candidate.localDoorPos }, roomSpacing != 0);

        gridMap = candidate.tempGrid;

        if (candidate.prevDoor.room != null)
        {
            AlignAndConnectRooms(candidate.prevDoor.room, candidate.prevDoor.localDoorPos, endRoom, candidate.localDoorPos, candidate.rotation, roomSpacing);
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
    /// <param name="start">Habitación inicial</param>
    /// <param name="target">Habitación objetivo</param>
    /// <returns>Número de pasos intermedios</returns>
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
    /// Extrae los datos de las puertas de una habitación aplicando la rotación especificada.
    /// </summary>
    /// <param name="room">Habitación a evaluar</param>
    /// <param name="rotation">Rotación aplicada</param>
    /// <returns>Lista de tuplas con posición local y tipo de tile</returns>
    private List<(Vector2I localPos, int tileType)> GetRoomDoorData(Room room, Room.RoomRotationEnum rotation)
    {
        List<(Vector2I, int)> doorDataList = new List<(Vector2I, int)>();
        Dictionary<Vector2I, int> rotatedGrid = room.GetRotatedRoomGrid(rotation);

        foreach (Door door in room.Doors)
        {
            Vector2I rotatedDoorPos = RotateGridPosition(door.RoomGridPosition, rotation, room.gridWidth, room.gridHeight);
            int tileVal = rotatedGrid.ContainsKey(rotatedDoorPos) ? rotatedGrid[rotatedDoorPos] : DoorDirectionToTileType(door.cardinalDirection);
            doorDataList.Add((rotatedDoorPos, tileVal));
        }
        return doorDataList;
    }

    /// <summary>
    /// Convierte la dirección de una puerta a su valor numérico de tile.
    /// </summary>
    /// <param name="dir">Dirección de la puerta</param>
    /// <returns>Valor entero del tile</returns>
    private int DoorDirectionToTileType(Door.DoorDirection dir) => dir switch
    {
        Door.DoorDirection.North => 2,
        Door.DoorDirection.East => 3,
        Door.DoorDirection.South => 4,
        Door.DoorDirection.West => 5,
        _ => 2
    };

    /// <summary>
    /// Rota una posición en la cuadrícula en base a las dimensiones de la sala y la rotación.
    /// </summary>
    /// <param name="pos">Posición a rotar</param>
    /// <param name="rotation">Rotación aplicada</param>
    /// <param name="w">Ancho</param>
    /// <param name="h">Alto</param>
    /// <returns>Posición rotada</returns>
    private Vector2I RotateGridPosition(Vector2I pos, Room.RoomRotationEnum rotation, int w, int h) => rotation switch
    {
        Room.RoomRotationEnum.None => pos,
        Room.RoomRotationEnum.OneQuarter => new Vector2I(h - 1 - pos.Y, pos.X),
        Room.RoomRotationEnum.Half => new Vector2I(w - 1 - pos.X, h - 1 - pos.Y),
        Room.RoomRotationEnum.ThreeQuarters => new Vector2I(pos.Y, w - 1 - pos.X),
        _ => pos
    };

    /// <summary>
    /// Comprueba si un valor de tile corresponde a una puerta.
    /// </summary>
    /// <param name="val">Valor del tile</param>
    /// <returns>True si es puerta</returns>
    private bool IsDoor(int val) => val >= 2 && val <= 5;

    /// <summary>
    /// Comprueba si un valor de tile corresponde a una parte estructural de la habitación.
    /// </summary>
    /// <param name="val">Valor del tile</param>
    /// <returns>True si es tile de habitación</returns>
    private bool IsRoomTile(int val) => val == 1 || (val >= 6 && val <= 13);

    /// <summary>
    /// Obtiene el tipo de puerta opuesto requerido para la conexión.
    /// </summary>
    /// <param name="doorType">Tipo de puerta actual</param>
    /// <returns>Tipo de puerta opuesto</returns>
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
    /// <param name="doorType">Tipo de puerta</param>
    /// <returns>Vector2I de dirección</returns>
    private Vector2I GetGridDirectionFromTileType(int doorType) => doorType switch
    {
        2 => new Vector2I(0, -1),
        3 => new Vector2I(1, 0),
        4 => new Vector2I(0, 1),
        5 => new Vector2I(-1, 0),
        _ => Vector2I.Zero
    };

    /// <summary>
    /// Finaliza la generación agregando las habitaciones al árbol de escena.
    /// </summary>
    private void FinalizeLevel()
    {
        foreach (Room room in placedRooms)
        {
            roomFolder.AddChild(room);
        }
    }

    /// <summary>
    /// Mezcla aleatoriamente los elementos de una lista usando el generador RNG.
    /// </summary>
    /// <param name="list">Lista a mezclar</param>
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _rng.RandiRange(0, n);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}