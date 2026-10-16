using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class DynamicGenTest : Node3D
{
    // SIGNALS
    [Signal] public delegate void Signal_LevelGenerationCompletedEventHandler();
    [Signal] public delegate void Signal_RoomPlacedEventHandler(DynamicRoom room);

    [ExportGroup("Room Prefabs")]
    [Export] public Node3D roomFolder; 
    [Export] public PackedScene startRoomScene; 
    [Export] public PackedScene endRoomScene; 
    [Export] public Godot.Collections.Array<PackedScene> standardRoomScenes; 
    [Export] public Godot.Collections.Array<PackedScene> hallwayScenes; 

    [ExportGroup("Generation Settings")]
    [Export] public bool useSeed = false; 
    [Export] public ulong generationSeed = 0; 

    [ExportGroup("Path Length Settings")]
    [Export] public int minMainPathLength = 4;
    [Export] public int maxMainPathLength = 8;

    [ExportGroup("Branching Distance Settings (Accumulate Doors ON)")]
    [Export] public int minRoomDistance = 3;
    [Export] public int maxRoomDistance = 6;

    [ExportGroup("General Settings")]
    [Export] public float hallwayChance = 0.3f; 
    [Export] public float roomSpacing = 0.0f; 
    [Export] public bool accumulatePreviousDoors = true; 

    public List<DynamicRoom> placedRooms = new List<DynamicRoom>(); 
    private RandomNumberGenerator _rng; 
    private Dictionary<Vector2I, int> gridMap = new Dictionary<Vector2I, int>();
    
    public struct DoorRecord
    {
        public Vector2I position;
        public int tileType; 
        public DynamicRoom room;
        public RoomDoor doorComponent;
    }
    private List<DoorRecord> previousRoomDoors = new List<DoorRecord>();

    public struct EndCandidate
    {
        public DoorRecord prevDoor;
        public PackedScene endPrefab;
        public DynamicRoom.RoomRotationEnum rotation;
        public Vector2I roomOrigin;
        public Dictionary<Vector2I, int> tempGrid;
        public Vector2I localDoorPos;
        public int distance;
    }

    public override void _Ready()
    {
        _rng = new RandomNumberGenerator();
        if (useSeed) _rng.Seed = generationSeed;
        else _rng.Randomize();
    }

    public async Task<bool> GenerateLevel()
    {
        bool success = await Task.Run(() =>
        {
            gridMap.Clear();
            previousRoomDoors.Clear();
            placedRooms.Clear();

            DynamicRoom startRoom = startRoomScene.Instantiate<DynamicRoom>();
            startRoom.InitializeRoom(DynamicRoom.RoomTypeEnum.Start);
            startRoom.SetRotation(DynamicRoom.RoomRotationEnum.None);

            Dictionary<Vector2I, int> startGrid = startRoom.GetRotatedRoomGrid(DynamicRoom.RoomRotationEnum.None);
            Vector2I startOrigin = new Vector2I(-startRoom.gridWidth / 2, -startRoom.gridHeight / 2);
            
            foreach (var kvp in startGrid)
            {
                gridMap[startOrigin + kvp.Key] = kvp.Value;
            }

            startRoom.Position = new Vector3(startOrigin.X * 2.0f, 0, startOrigin.Y * 2.0f);
            placedRooms.Add(startRoom);

            foreach (RoomDoor door in startRoom.Doors)
            {
                int tileVal = startGrid.ContainsKey(door.RoomGridPosition) ? startGrid[door.RoomGridPosition] : DoorDirectionToTileType(door.cardinalDirection);
                previousRoomDoors.Add(new DoorRecord { position = startOrigin + door.RoomGridPosition, tileType = tileVal, room = startRoom, doorComponent = door });
            }

            int targetPathLength = _rng.RandiRange(minMainPathLength, maxMainPathLength);
            GD.Print($"[PMD Gen] Initialized Start Room. AccumulateDoors: {accumulatePreviousDoors}, Target Path Length: {targetPathLength} (Bounds: [{minMainPathLength} - {maxMainPathLength}])");

            if (!accumulatePreviousDoors)
            {
                GD.Print("[PMD Linear] Starting strict linear path generation...");
                for (int step = 1; step < targetPathLength; step++)
                {
                    List<PackedScene> shuffledRooms = new List<PackedScene>(standardRoomScenes);
                    ShuffleList(shuffledRooms);
                    bool placed = false;
                    foreach (var roomPrefab in shuffledRooms)
                    {
                        if (TryPlaceRandomStandardRoom(roomPrefab, DynamicRoom.RoomTypeEnum.Main, out _))
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
                    if (TryPlaceRandomStandardRoom(roomPrefab, DynamicRoom.RoomTypeEnum.End, out _))
                    {
                        endPlaced = true;
                        break;
                    }
                }
                return endPlaced;
            }
            else
            {
                GD.Print("[PMD Branching] Starting branching path generation...");
                int currentStandardRooms = 0;

                // Phase 1: Place standard rooms up to targetPathLength
                while (currentStandardRooms < targetPathLength && previousRoomDoors.Count > 0)
                {
                    List<PackedScene> shuffledRooms = new List<PackedScene>(standardRoomScenes);
                    ShuffleList(shuffledRooms);
                    bool roomAdded = false;
                    foreach (var roomPrefab in shuffledRooms)
                    {
                        if (TryPlaceRandomStandardRoom(roomPrefab, DynamicRoom.RoomTypeEnum.Main, out _))
                        {
                            currentStandardRooms++;
                            roomAdded = true;
                            break;
                        }
                    }
                    if (!roomAdded) break;
                }
                GD.Print($"[PMD Branching] Placed {currentStandardRooms} standard rooms.");

                // Phase 2: If the furthest open door is shorter than minRoomDistance, extend path from the furthest room step-by-step
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

                    // Find doors sorted descending by their room's distance from Start (Furthest first)
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
                            if (TryPlaceRoomPMD(roomPrefab, furthestDoor, DynamicRoom.RoomTypeEnum.Main, out _))
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

                // Phase 3: Evaluate and place End Room
                var candidates = FindAllEndCandidates();
                GD.Print($"[PMD Branching] Evaluated end candidates. Total valid spots: {candidates.Count}");

                if (candidates.Count > 0)
                {
                    var ideal = candidates.Where(c => c.distance >= minRoomDistance && c.distance <= maxRoomDistance).ToList();
                    if (ideal.Count > 0)
                    {
                        var chosen = ideal[_rng.RandiRange(0, ideal.Count - 1)];
                        GD.Print($"[PMD Branching] Placing End Room at ideal distance: {chosen.distance}");
                        CommitEndCandidate(chosen);
                        return true;
                    }
                    else
                    {
                        var best = candidates.OrderByDescending(c => c.distance).First();
                        GD.Print($"[PMD Branching] Placing End Room at fallback furthest distance: {best.distance}");
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
        });

        if (success) 
        {
            FinalizeLevel();
            EmitSignal(SignalName.Signal_LevelGenerationCompleted);
            GD.Print("[PMD Gen] Level generation finalized successfully.");
        }
        return success; 
    }

    private Dictionary<DynamicRoom, int> CalculateRoomDistances(DynamicRoom start)
    {
        Dictionary<DynamicRoom, int> distances = new Dictionary<DynamicRoom, int>();
        Queue<DynamicRoom> queue = new Queue<DynamicRoom>();

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

    private bool TryPlaceRandomStandardRoom(PackedScene prefab, DynamicRoom.RoomTypeEnum roomType, out DynamicRoom newRoom)
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

    private bool TryPlaceRoomPMD(PackedScene prefab, DoorRecord prevDoor, DynamicRoom.RoomTypeEnum roomType, out DynamicRoom newRoom)
    {
        newRoom = prefab.Instantiate<DynamicRoom>();
        newRoom.InitializeRoom(roomType);

        List<DynamicRoom.RoomRotationEnum> rotations = Enum.GetValues(typeof(DynamicRoom.RoomRotationEnum)).Cast<DynamicRoom.RoomRotationEnum>().ToList();
        ShuffleList(rotations);

        int requiredOppositeDoorType = GetOppositeDoorType(prevDoor.tileType);

        foreach (var rot in rotations)
        {
            newRoom.SetRotation(rot);
            Dictionary<Vector2I, int> rotatedGrid = newRoom.GetRotatedRoomGrid(rot);

            List<Vector2I> matchingDoorPositions = rotatedGrid.Where(kvp => kvp.Value == requiredOppositeDoorType).Select(kvp => kvp.Key).ToList();
            ShuffleList(matchingDoorPositions);

            foreach (Vector2I roomDoorLocalPos in matchingDoorPositions)
            {
                Vector2I roomOrigin = (prevDoor.position + GetGridDirectionFromTileType(prevDoor.tileType)) - roomDoorLocalPos;

                int expectedRooms = gridMap.Values.Count(v => v == 1) + rotatedGrid.Values.Count(v => v == 1);
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
                if (tempGrid.Values.Count(v => v == 1) != expectedRooms || tempGrid.Values.Count(v => IsDoor(v)) != expectedDoors) continue;

                // Success
                gridMap = tempGrid;

                int gridW = newRoom.gridWidth;
                int gridH = newRoom.gridHeight;
                RoomDoor newRoomDoor = newRoom.Doors.FirstOrDefault(d => RotateGridPosition(d.RoomGridPosition, rot, gridW, gridH) == roomDoorLocalPos);

                if (newRoomDoor != null && prevDoor.doorComponent != null)
                {
                    AlignRooms(prevDoor.room, prevDoor.doorComponent, newRoom, newRoomDoor);
                    prevDoor.doorComponent.SetConnected(true);
                    newRoomDoor.SetConnected(true);
                    prevDoor.doorComponent.ConnectedTo = newRoomDoor;
                    newRoomDoor.ConnectedTo = prevDoor.doorComponent;
                    prevDoor.room.ConnectedRooms.Add(newRoom);
                    newRoom.ConnectedRooms.Add(prevDoor.room);
                }

                placedRooms.Add(newRoom);

                if (!accumulatePreviousDoors) previousRoomDoors.Clear();
                else previousRoomDoors.RemoveAll(d => d.doorComponent == prevDoor.doorComponent);

                foreach (RoomDoor d in newRoom.Doors)
                {
                    if (d == newRoomDoor) continue;
                    Vector2I rotatedDoorPos = RotateGridPosition(d.RoomGridPosition, rot, newRoom.gridWidth, newRoom.gridHeight);
                    int tileVal = rotatedGrid.ContainsKey(rotatedDoorPos) ? rotatedGrid[rotatedDoorPos] : DoorDirectionToTileType(d.cardinalDirection);
                    previousRoomDoors.Add(new DoorRecord { position = roomOrigin + rotatedDoorPos, tileType = tileVal, room = newRoom, doorComponent = d });
                }

                return true;
            }
        }

        newRoom.QueueFree();
        newRoom = null;
        return false;
    }

    private List<EndCandidate> FindAllEndCandidates()
    {
        List<EndCandidate> candidates = new List<EndCandidate>();
        List<DoorRecord> shuffledDoors = new List<DoorRecord>(previousRoomDoors);
        ShuffleList(shuffledDoors);

        foreach (DoorRecord prevDoor in shuffledDoors)
        {
            DynamicRoom testEndRoom = endRoomScene.Instantiate<DynamicRoom>();
            testEndRoom.InitializeRoom(DynamicRoom.RoomTypeEnum.End);

            List<DynamicRoom.RoomRotationEnum> rotations = Enum.GetValues(typeof(DynamicRoom.RoomRotationEnum)).Cast<DynamicRoom.RoomRotationEnum>().ToList();
            ShuffleList(rotations);

            int requiredOppositeDoorType = GetOppositeDoorType(prevDoor.tileType);
            bool foundForThisDoor = false;

            foreach (var rot in rotations)
            {
                if (foundForThisDoor) break;
                testEndRoom.SetRotation(rot);
                Dictionary<Vector2I, int> rotatedGrid = testEndRoom.GetRotatedRoomGrid(rot);

                List<Vector2I> matchingDoorPositions = rotatedGrid.Where(kvp => kvp.Value == requiredOppositeDoorType).Select(kvp => kvp.Key).ToList();
                
                foreach (Vector2I roomDoorLocalPos in matchingDoorPositions)
                {
                    Vector2I roomOrigin = (prevDoor.position + GetGridDirectionFromTileType(prevDoor.tileType)) - roomDoorLocalPos;

                    int expectedRooms = gridMap.Values.Count(v => v == 1) + rotatedGrid.Values.Count(v => v == 1);
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
                    if (tempGrid.Values.Count(v => v == 1) != expectedRooms || tempGrid.Values.Count(v => IsDoor(v)) != expectedDoors) continue;

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
                        distance = dist
                    });

                    foundForThisDoor = true;
                    break;
                }
            }
            testEndRoom.QueueFree();
        }
        return candidates;
    }

    private bool CommitEndCandidate(EndCandidate candidate)
    {
        DynamicRoom endRoom = candidate.endPrefab.Instantiate<DynamicRoom>();
        endRoom.InitializeRoom(DynamicRoom.RoomTypeEnum.End);
        endRoom.SetRotation(candidate.rotation);

        gridMap = candidate.tempGrid;

        int gridW = endRoom.gridWidth;
        int gridH = endRoom.gridHeight;
        RoomDoor newRoomDoor = endRoom.Doors.FirstOrDefault(d => RotateGridPosition(d.RoomGridPosition, candidate.rotation, gridW, gridH) == candidate.localDoorPos);

        if (newRoomDoor != null && candidate.prevDoor.doorComponent != null)
        {
            AlignRooms(candidate.prevDoor.room, candidate.prevDoor.doorComponent, endRoom, newRoomDoor);
            candidate.prevDoor.doorComponent.SetConnected(true);
            newRoomDoor.SetConnected(true);
            candidate.prevDoor.doorComponent.ConnectedTo = newRoomDoor;
            newRoomDoor.ConnectedTo = candidate.prevDoor.doorComponent;
            candidate.prevDoor.room.ConnectedRooms.Add(endRoom);
            endRoom.ConnectedRooms.Add(candidate.prevDoor.room);
        }
        else
        {
            endRoom.Position = new Vector3(candidate.roomOrigin.X * 2.0f, 0, candidate.roomOrigin.Y * 2.0f);
        }

        placedRooms.Add(endRoom);
        return true;
    }

    private int GetIntermediateRoomCount(DynamicRoom start, DynamicRoom target)
    {
        if (start == target) return -1;
        Queue<DynamicRoom> queue = new Queue<DynamicRoom>();
        Dictionary<DynamicRoom, int> distances = new Dictionary<DynamicRoom, int>();
        
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

    private void AlignRooms(DynamicRoom exitRoom, RoomDoor exitDoor, DynamicRoom newRoom, RoomDoor entranceDoor)
    {
        Vector3 exitDoorWorldPos = (exitRoom.Transform * exitRoom.GetRelativeTransform(exitDoor)).Origin;
        newRoom.Position = Vector3.Zero;
        Vector3 entranceDoorWorldPos = (newRoom.Transform * newRoom.GetRelativeTransform(entranceDoor)).Origin;
        newRoom.Position = exitDoorWorldPos + GetSpacingOffset(exitDoor.cardinalDirection) - entranceDoorWorldPos;
    }

    private Vector3 GetSpacingOffset(RoomDoor.DoorDirection direction) => direction switch
    {
        RoomDoor.DoorDirection.North => new Vector3(0, 0, -roomSpacing),
        RoomDoor.DoorDirection.South => new Vector3(0, 0, roomSpacing),
        RoomDoor.DoorDirection.East => new Vector3(roomSpacing, 0, 0),
        RoomDoor.DoorDirection.West => new Vector3(-roomSpacing, 0, 0),
        _ => Vector3.Zero
    };

    private int DoorDirectionToTileType(RoomDoor.DoorDirection dir) => dir switch
    {
        RoomDoor.DoorDirection.North => 2,
        RoomDoor.DoorDirection.East => 3,
        RoomDoor.DoorDirection.South => 4,
        RoomDoor.DoorDirection.West => 5,
        _ => 2
    };

    private Vector2I RotateGridPosition(Vector2I pos, DynamicRoom.RoomRotationEnum rotation, int w, int h) => rotation switch
    {
        DynamicRoom.RoomRotationEnum.None => pos,
        DynamicRoom.RoomRotationEnum.OneQuarter => new Vector2I(h - 1 - pos.Y, pos.X),
        DynamicRoom.RoomRotationEnum.Half => new Vector2I(w - 1 - pos.X, h - 1 - pos.Y),
        DynamicRoom.RoomRotationEnum.ThreeQuarters => new Vector2I(pos.Y, w - 1 - pos.X),
        _ => pos
    };

    private bool IsDoor(int val) => val >= 2 && val <= 5;

    private int GetOppositeDoorType(int doorType) => doorType switch
    {
        2 => 4,
        3 => 5,
        4 => 2,
        5 => 3,
        _ => 2
    };

    private Vector2I GetGridDirectionFromTileType(int doorType) => doorType switch
    {
        2 => new Vector2I(0, -1),
        3 => new Vector2I(1, 0),
        4 => new Vector2I(0, 1),
        5 => new Vector2I(-1, 0),
        _ => Vector2I.Zero
    };

    private void FinalizeLevel()
    {
        foreach (DynamicRoom room in placedRooms)
        {
            roomFolder.AddChild(room);
            foreach (RoomDoor door in room.Doors) door.ResolveDoorState(roomSpacing);
        }
    }

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