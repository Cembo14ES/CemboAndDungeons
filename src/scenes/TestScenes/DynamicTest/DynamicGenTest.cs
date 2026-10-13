using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class DynamicGenTest : Node3D
{
    [ExportGroup("Room Prefabs")]
    [Export] public Node3D roomFolder; 
    [Export] public PackedScene startRoomScene; 
    [Export] public PackedScene endRoomScene; 
    [Export] public Godot.Collections.Array<PackedScene> standardRoomScenes; 
    [Export] public Godot.Collections.Array<PackedScene> hallwayScenes; 

    [ExportGroup("Generation Settings")]
    [Export] public bool useSeed = false; 
    [Export] public ulong generationSeed = 0; 
    [Export] public int mainPathLength = 6; 
    [Export] public float hallwayChance = 0.3f; 
    [Export] public float roomSpacing = 0.0f; 

    public List<DynamicRoom> placedRooms = new List<DynamicRoom>(); 
    private RandomNumberGenerator _rng; 

    // PMD Grid Generation Variables
    private Dictionary<Vector2I, int> gridMap = new Dictionary<Vector2I, int>();
    
    public struct DoorRecord
    {
        public Vector2I position;
        public int tileType; // 2: North, 3: East, 4: South, 5: West
        public DynamicRoom room;
    }
    private List<DoorRecord> previousRoomDoors = new List<DoorRecord>();

    public override void _Ready()
    {
        _rng = new RandomNumberGenerator();
        if (useSeed)
        {
            _rng.Seed = generationSeed;
            GD.Print($"\n[Init] Generator initialized with Fixed Seed: " + generationSeed);
        }
        else
        {
            _rng.Randomize();
            GD.Print($"\n[Init] Generator initialized with Random Seed.");
        }
    }

    public async Task<bool> GenerateLevel()
    {
        GD.Print("\n==========================================");
        GD.Print("   STARTING LEVEL GENERATION (PMD GRID)");
        GD.Print("==========================================");

        bool success = await Task.Run(() =>
        {
            gridMap.Clear();
            previousRoomDoors.Clear();
            placedRooms.Clear();

            GD.Print("\n--- [PMD Grid] Placing Start Room ---");
            DynamicRoom startRoom = startRoomScene.Instantiate<DynamicRoom>();
            startRoom.InitializeRoom(DynamicRoom.RoomTypeEnum.Start);
            startRoom.SetRotation(DynamicRoom.RoomRotationEnum.None);

            Dictionary<Vector2I, int> startGrid = startRoom.GetRotatedRoomGrid(DynamicRoom.RoomRotationEnum.None);
            
            // Center start room at grid origin (0,0)
            Vector2I startOrigin = new Vector2I(-startRoom.gridWidth / 2, -startRoom.gridHeight / 2);
            
            foreach (var kvp in startGrid)
            {
                Vector2I globalPos = startOrigin + kvp.Key;
                gridMap[globalPos] = kvp.Value;
                if (IsDoor(kvp.Value))
                {
                    previousRoomDoors.Add(new DoorRecord { position = globalPos, tileType = kvp.Value, room = startRoom });
                }
            }

            startRoom.Position = new Vector3(startOrigin.X * 2.0f, 0, startOrigin.Y * 2.0f);
            placedRooms.Add(startRoom);
            GD.Print($"[PMD Grid] Start room placed at origin {startOrigin} with {previousRoomDoors.Count} initial doors registered.");

            GD.Print("\n--- [PMD Grid] Starting Main Path Generation ---");
            bool pathSuccess = BuildPMDPathRecursive(1);
            if (!pathSuccess)
            {
                GD.PrintErr("[PMD Grid] Error: Main Path generation failed entirely!");
                return false;
            }

            SaveShadowGridFile();
            return true;
        });

        if (success)
        {
            FinalizeLevel();
            GD.Print("\n==========================================");
            GD.Print("   GENERATION COMPLETE AND FINALIZED!");
            GD.Print("==========================================\n");
        }
    
        return success; 
    }

    private bool BuildPMDPathRecursive(int currentStep)
    {
        if (currentStep >= mainPathLength)
        {
            GD.Print($"[PMD Grid] Target main path length {mainPathLength} reached!");
            return true;
        }

        GD.Print($"\n[PMD Grid] Step {currentStep}/{mainPathLength} - Choosing room and available doors...");

        if (previousRoomDoors.Count == 0)
        {
            GD.PrintErr("[PMD Grid] Error: previousRoomDoors is empty! Cannot continue path.");
            return false;
        }

        // Shuffle available doors from previous steps
        List<DoorRecord> shuffledDoors = new List<DoorRecord>(previousRoomDoors);
        ShuffleList(shuffledDoors);

        List<PackedScene> roomPool = (currentStep == mainPathLength - 1) ? new List<PackedScene> { endRoomScene } : standardRoomScenes.ToList();

        foreach (DoorRecord prevDoor in shuffledDoors)
        {
            List<PackedScene> shuffledRooms = new List<PackedScene>(roomPool);
            ShuffleList(shuffledRooms);

            foreach (PackedScene roomPrefab in shuffledRooms)
            {
                if (TryPlaceRoomPMD(roomPrefab, prevDoor, currentStep == mainPathLength - 1 ? DynamicRoom.RoomTypeEnum.End : DynamicRoom.RoomTypeEnum.Main))
                {
                    GD.Print($"[PMD Grid] Step {currentStep}: Room successfully placed! Moving to next step.");
                    if (BuildPMDPathRecursive(currentStep + 1)) return true;
                    
                    // If recursive path fails further down, we backtrack (simplified for demonstration)
                }
            }
        }

        GD.Print($"[PMD Grid] Step {currentStep}: All door and room combinations failed.");
        return false;
    }

    private bool TryPlaceRoomPMD(PackedScene prefab, DoorRecord prevDoor, DynamicRoom.RoomTypeEnum roomType)
    {
        DynamicRoom newRoom = prefab.Instantiate<DynamicRoom>();
        newRoom.InitializeRoom(roomType);

        List<DynamicRoom.RoomRotationEnum> rotations = new List<DynamicRoom.RoomRotationEnum>
        {
            DynamicRoom.RoomRotationEnum.None,
            DynamicRoom.RoomRotationEnum.OneQuarter,
            DynamicRoom.RoomRotationEnum.Half,
            DynamicRoom.RoomRotationEnum.ThreeQuarters
        };
        ShuffleList(rotations);

        int requiredOppositeDoorType = GetOppositeDoorType(prevDoor.tileType);

        foreach (var rot in rotations)
        {
            newRoom.SetRotation(rot);
            Dictionary<Vector2I, int> rotatedGrid = newRoom.GetRotatedRoomGrid(rot);

            // Find all doors in the rotated room grid that match the required opposite door type
            List<Vector2I> matchingDoorPositions = rotatedGrid.Where(kvp => kvp.Value == requiredOppositeDoorType).Select(kvp => kvp.Key).ToList();
            ShuffleList(matchingDoorPositions);

            foreach (Vector2I roomDoorLocalPos in matchingDoorPositions)
            {
                // Calculate room origin so that roomDoorLocalPos aligns with prevDoor.position + direction offset
                Vector2I dirOffset = GetGridDirectionFromTileType(prevDoor.tileType);
                Vector2I targetCell = prevDoor.position + dirOffset;
                Vector2I roomOrigin = targetCell - roomDoorLocalPos;

                // 1. Count baseline room & door tiles before placement
                int currentRoomTiles = gridMap.Values.Count(v => v == 1);
                int currentDoorTiles = gridMap.Values.Count(v => IsDoor(v));

                int newRoomTilesCount = rotatedGrid.Values.Count(v => v == 1);
                int newRoomDoorsCount = rotatedGrid.Values.Count(v => IsDoor(v));

                int expectedTotalRooms = currentRoomTiles + newRoomTilesCount;
                int expectedTotalDoors = currentDoorTiles + newRoomDoorsCount;

                // 2. Build temporary grid copy to test overlap & counts
                Dictionary<Vector2I, int> tempGrid = new Dictionary<Vector2I, int>(gridMap);
                bool hasOverlap = false;

                foreach (var kvp in rotatedGrid)
                {
                    Vector2I globalPos = roomOrigin + kvp.Key;
                    int tileVal = kvp.Value;

                    if (tempGrid.ContainsKey(globalPos) && tempGrid[globalPos] > 0)
                    {
                        // Overlap detected on non-empty cell
                        hasOverlap = true;
                        break;
                    }
                    tempGrid[globalPos] = tileVal;
                }

                if (hasOverlap) continue;

                // 3. Validate tile counts
                int actualTotalRooms = tempGrid.Values.Count(v => v == 1);
                int actualTotalDoors = tempGrid.Values.Count(v => IsDoor(v));

                if (actualTotalRooms != expectedTotalRooms || actualTotalDoors != expectedTotalDoors)
                {
                    GD.Print($"[PMD Overlap Check] Tile count mismatch! Expected Rooms: {expectedTotalRooms}, Actual: {actualTotalRooms} | Expected Doors: {expectedTotalDoors}, Actual: {actualTotalDoors}. Rejecting placement.");
                    continue;
                }

                // --- VALID PLACEMENT SUCCESS ---
                gridMap = tempGrid;
                newRoom.Position = new Vector3(roomOrigin.X * 2.0f, 0, roomOrigin.Y * 2.0f);
                placedRooms.Add(newRoom);

                // Register new room's doors into previousRoomDoors (excluding the one connected to prevDoor)
                foreach (var kvp in rotatedGrid)
                {
                    if (IsDoor(kvp.Value))
                    {
                        Vector2I doorGlobalPos = roomOrigin + kvp.Key;
                        if (doorGlobalPos != targetCell) // Don't re-add the connected door entry
                        {
                            previousRoomDoors.Add(new DoorRecord { position = doorGlobalPos, tileType = kvp.Value, room = newRoom });
                        }
                    }
                }

                GD.Print($"[PMD Placement] Successfully placed room {newRoom.Name} at origin {roomOrigin} with rotation {rot}. Total placed rooms: {placedRooms.Count}");
                return true;
            }
        }

        newRoom.QueueFree();
        return false;
    }

    private bool IsDoor(int val) => val >= 2 && val <= 5;

    private int GetOppositeDoorType(int doorType)
    {
        // 2: North, 3: East, 4: South, 5: West
        return doorType switch
        {
            2 => 4, // North -> South
            3 => 5, // East -> West
            4 => 2, // South -> North
            5 => 3, // West -> East
            _ => 2
        };
    }

    private Vector2I GetGridDirectionFromTileType(int doorType)
    {
        return doorType switch
        {
            2 => new Vector2I(0, -1), // North opens up, so neighbor is North (-Y)
            3 => new Vector2I(1, 0),  // East opens right (+X)
            4 => new Vector2I(0, 1),  // South opens down (+Y)
            5 => new Vector2I(-1, 0), // West opens left (-X)
            _ => Vector2I.Zero
        };
    }

    private void SaveShadowGridFile()
    {
        if (gridMap.Count == 0) return;

        int minX = gridMap.Keys.Min(k => k.X);
        int maxX = gridMap.Keys.Max(k => k.X);
        int minY = gridMap.Keys.Min(k => k.Y);
        int maxY = gridMap.Keys.Max(k => k.Y);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== POKEMON MYSTERY DUNGEON SHADOW GRID MAP ===");

        for (int y = minY; y <= maxY; y++)
        {
            string rowStr = "";
            for (int x = minX; x <= maxX; x++)
            {
                Vector2I pos = new Vector2I(x, y);
                if (gridMap.TryGetValue(pos, out int val))
                {
                    rowStr += GetTileSymbol(val);
                }
                else
                {
                    rowStr += " . ";
                }
            }
            sb.AppendLine(rowStr);
        }

        string filePath = "res://addons/room_grid_editor/dungeon_grid_map.txt";
        var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(sb.ToString());
            file.Close();
            GD.Print($"\n[PMD Grid] Successfully saved master grid map to: {filePath}\n");
        }
    }

    private string GetTileSymbol(int val)
    {
        return val switch
        {
            1 => "[#]", // Room
            2 => "[N]", // DoorNorth
            3 => "[E]", // DoorEast
            4 => "[S]", // DoorSouth
            5 => "[W]", // DoorWest
            _ => " . "
        };
    }

    private void FinalizeLevel()
    {
        GD.Print($"[Finalize] Adding {placedRooms.Count} placed rooms to the scene tree...");
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