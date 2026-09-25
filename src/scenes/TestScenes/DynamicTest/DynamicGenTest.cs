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
    
    [ExportGroup("Branch Settings")]
    [Export] public float branchChance = 0.4f; 
    [Export] public int minBranchRooms = 1; 
    [Export] public int maxBranchRooms = 4; 

    [Export] public float loopBranchChance = 0.35f; 
    [Export] public int minLoopBranchRooms = 1; 
    [Export] public int maxLoopBranchRooms = 4; 

    [Export] public int minChanceLoopDistance = 3; 
    [Export] public int maxChanceLoopDistance = 6; 
    [Export] public int maxBranchFails = 5; 

    public List<DynamicRoom> placedRooms = new List<DynamicRoom>(); 
    private RandomNumberGenerator _rng; 

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
        GD.Print("   STARTING LEVEL GENERATION (BACKGROUND)");
        GD.Print("==========================================");

        bool success = await Task.Run(() =>
        {
            GD.Print("\n--- GENERATING START ROOM ---");
            DynamicRoom startRoom = startRoomScene.Instantiate<DynamicRoom>();
            startRoom.InitializeRoom(DynamicRoom.RoomTypeEnum.Start);
            startRoom.Transform = Transform3D.Identity;
            placedRooms.Add(startRoom);
            GD.Print("[Success] Start room placed at Origin.");

            GD.Print("\n--- GENERATING MAIN PATH ---");
            bool pathSuccess = BuildPathRecursive(startRoom, 1);
            if (!pathSuccess)
            {
                GD.PrintErr("[Error] Main Path generation failed entirely!");
                return false;
            }

            GenerateBranches();
            GenerateChanceLoops();
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

    private bool BuildPathRecursive(DynamicRoom currentRoom, int currentStep)
    {
        if (currentStep >= mainPathLength)
        {
            currentRoom.setRoomType(DynamicRoom.RoomTypeEnum.End);
            GD.Print($"[Main Path] TARGET REACHED! Converted room at step {currentStep} to END room.");
            return true;
        } 

        GD.Print($"\n[Main Path] Step {currentStep}/{mainPathLength} - Evaluating exits...");
        bool isFinalRoom = currentStep == mainPathLength - 1;
        List<RoomDoor> availableExits = currentRoom.GetAvailableDoors();
        ShuffleList(availableExits);

        foreach (RoomDoor exitDoor in availableExits)
        {
            GD.Print($"  -> [Main Path Step {currentStep}] Trying exit facing {exitDoor.cardinalDirection}");
            List<PackedScene> roomChoices = isFinalRoom ? new List<PackedScene> { endRoomScene } : standardRoomScenes.ToList();
            ShuffleList(roomChoices);

            foreach (PackedScene roomPrefab in roomChoices)
            {
                bool useHallway = !isFinalRoom && _rng.Randf() < hallwayChance;
                DynamicRoom roomToConnectFrom = currentRoom;
                RoomDoor activeExit = exitDoor;

                DynamicRoom placedHallway = null;
                RoomDoor hallwayEntranceUsed = null;

                if (useHallway && hallwayScenes.Count > 0)
                {
                    GD.Print($"    -> Hallway chance passed. Attempting to place hallway first...");
                    List<PackedScene> hallways = hallwayScenes.ToList();
                    ShuffleList(hallways);

                    foreach (PackedScene hallway in hallways)
                    {
                        placedHallway = TryPlaceRoom(roomToConnectFrom, activeExit, hallway, out hallwayEntranceUsed, DynamicRoom.RoomTypeEnum.Main);

                        if (placedHallway != null)
                        {
                            GD.Print($"    -> Hallway placed successfully!");
                            roomToConnectFrom = placedHallway;
                            var hallwayExits = placedHallway.GetAvailableDoors();
                            if (hallwayExits.Count > 0) activeExit = hallwayExits[_rng.RandiRange(0, hallwayExits.Count - 1)];
                            break;
                        }
                    }
                }

                GD.Print($"    -> Attempting to place Main Room (Step {currentStep + 1})...");
                DynamicRoom placedRoom = TryPlaceRoom(roomToConnectFrom, activeExit, roomPrefab, out RoomDoor roomEntranceUsed, DynamicRoom.RoomTypeEnum.Main);

                if (placedRoom != null)
                {
                    GD.Print($"    -> Main Room placed successfully! Moving to next step.");
                    if (BuildPathRecursive(placedRoom, currentStep + 1)) return true;
                    
                    GD.Print($"    -> [Backtrack] Dead end hit down the line! Undoing Room at step {currentStep + 1}.");
                    UndoPlacement(placedRoom, activeExit, roomEntranceUsed);
                }

                if (placedHallway != null) 
                {
                    GD.Print($"    -> [Backtrack] Undoing Hallway at step {currentStep}.");
                    UndoPlacement(placedHallway, exitDoor, hallwayEntranceUsed);
                }
            }
        }
        
        GD.Print($"[Main Path Step {currentStep}] All exits and prefabs failed. Falling back to previous step...");
        return false;
    }

    private DynamicRoom TryPlaceRoom(DynamicRoom exitRoom, RoomDoor exitDoor, PackedScene prefab, out RoomDoor entranceUsed, DynamicRoom.RoomTypeEnum roomType)
    {
        entranceUsed = null;
        DynamicRoom newRoom = prefab.Instantiate<DynamicRoom>();
        newRoom.InitializeRoom(roomType);

        List<DynamicRoom.RoomRotationEnum> rotationAngles = new List<DynamicRoom.RoomRotationEnum> 
        { 
            DynamicRoom.RoomRotationEnum.None, 
            DynamicRoom.RoomRotationEnum.OneQuarter, 
            DynamicRoom.RoomRotationEnum.Half, 
            DynamicRoom.RoomRotationEnum.ThreeQuarters 
        };
        ShuffleList(rotationAngles);

        foreach (DynamicRoom.RoomRotationEnum angle in rotationAngles)
        {
            newRoom.SetRotation(angle);

            List<RoomDoor> entrances = newRoom.GetAvailableDoors();
            ShuffleList(entrances);

            foreach (RoomDoor entrance in entrances)
            {
                if (!entrance.CanConnectTo(exitDoor)) continue;

                AlignRooms(exitRoom, exitDoor, newRoom, entrance);
                
                bool isOverlapping = CheckOverlap(newRoom, exitRoom, roomSpacing / 2f);
                if (!isOverlapping)
                {
                    exitDoor.SetConnected(true);
                    entrance.SetConnected(true);
                    exitDoor.ConnectedTo = entrance;
                    entrance.ConnectedTo = exitDoor;

                    exitRoom.ConnectedRooms.Add(newRoom);
                    newRoom.ConnectedRooms.Add(exitRoom);

                    placedRooms.Add(newRoom);
                    entranceUsed = entrance;
                    return newRoom;
                }
            }
        }

        newRoom.QueueFree();
        return null;
    }

    private void AlignRooms(DynamicRoom exitRoom, RoomDoor exitDoor, DynamicRoom newRoom, RoomDoor entranceDoor)
    {
        Vector3 exitDoorWorldPos = (exitRoom.Transform * exitRoom.GetRelativeTransform(exitDoor)).Origin;
        newRoom.Position = Vector3.Zero;
        Vector3 entranceDoorWorldPos = (newRoom.Transform * newRoom.GetRelativeTransform(entranceDoor)).Origin;
        Vector3 spacingOffset = GetSpacingOffset(exitDoor.cardinalDirection);
        newRoom.Position = exitDoorWorldPos + spacingOffset - entranceDoorWorldPos;
    }

    private Vector3 GetSpacingOffset(RoomDoor.DoorDirection direction)
    {
        return direction switch
        {
            RoomDoor.DoorDirection.North => new Vector3(0, 0, roomSpacing),
            RoomDoor.DoorDirection.South => new Vector3(0, 0, -roomSpacing),
            RoomDoor.DoorDirection.East => new Vector3(-roomSpacing, 0, 0),
            RoomDoor.DoorDirection.West => new Vector3(roomSpacing, 0, 0),
            _ => Vector3.Zero
        };
    }

    private bool CheckOverlap(DynamicRoom newRoom, DynamicRoom parentRoom, float margin)
    {
        foreach (DynamicRoom placedRoom in placedRooms)
        {
            if (newRoom.IntersectsWith(placedRoom, margin)) return true;
        }
        return false;
    }

    private void GenerateBranches()
    {
        GD.Print("\n--- STARTING BRANCH GENERATION ---");
        
        // ONLY gather rooms that belong to the main path, completely ignoring "Regular" branch rooms
        List<DynamicRoom> mainPathRooms = placedRooms.Where(r => r.roomType == DynamicRoom.RoomTypeEnum.Main).ToList();

        GD.Print($"Found {mainPathRooms.Count} main path rooms to evaluate for branches.");

        // Iterate ONLY through the filtered main path rooms
        foreach (DynamicRoom room in mainPathRooms)
        {
            GD.Print($"\nEvaluating unused doors for main path room: {room.Name}");
            foreach (RoomDoor branchDoor in room.GetAvailableDoors())
            {
                if (branchDoor.isConnected) continue;

                if (_rng.Randf() > branchChance)
                {
                    GD.Print($"- Door {branchDoor.cardinalDirection} failed branch chance.");
                    continue;
                }

                GD.Print($"- Door {branchDoor.cardinalDirection} PASSED branch chance. Initiating generation...");

                // 1. Loop Branch Logic
                bool tryLoopBranch = _rng.Randf() < loopBranchChance;
                bool loopBranchSuccess = false;

                if (tryLoopBranch)
                {
                    GD.Print("  -> Attempting Loop Branch...");
                    List<RoomDoor> targetDoors = new List<RoomDoor>();
                    
                    foreach (DynamicRoom pr in placedRooms)
                    {
                        if (pr == room) continue;
                        
                        int navDist = GetNavigationDistance(room, pr);
                        if ((navDist >= minLoopBranchRooms && navDist <= maxLoopBranchRooms) || navDist == -1)
                        {
                            foreach (RoomDoor d in pr.GetAvailableDoors())
                            {
                                if (d.cardinalDirection == branchDoor.cardinalDirection)
                                {
                                    targetDoors.Add(d);
                                }
                            }
                        }
                    }
                    ShuffleList(targetDoors);

                    int targetAttempts = 0;
                    foreach (RoomDoor targetDoor in targetDoors)
                    {
                        if (targetAttempts >= 2) break; 
                        targetAttempts++;

                        if (BuildLoopBranchRecursive(room, branchDoor, targetDoor, 1))
                        {
                            loopBranchSuccess = true;
                            break;
                        }
                    }

                    if (loopBranchSuccess) 
                    {
                        GD.Print("  -> Loop Branch SUCCESS!");
                        continue;
                    }
                    else
                    {
                        GD.Print("  -> Loop Branch FAILED. Falling back to Regular Branch...");
                    }
                }

                // 2. Standard Branch Logic (Variable Length)
                int targetBranchLength = _rng.RandiRange(minBranchRooms, maxBranchRooms);
                int branchFails = 0; 
                GD.Print($"  -> Attempting Regular Branch (Target Depth: {targetBranchLength} standard rooms)");
                
                bool success = BuildRegularBranchRecursive(room, branchDoor, 1, targetBranchLength, ref branchFails);
                
                if (success)
                    GD.Print($"  -> Regular Branch SUCCESS! Reached depth {targetBranchLength}.");
                else
                    GD.Print($"  -> Regular Branch CANCELED. Exhausted max fails ({maxBranchFails}).");
            }
        }
        GD.Print("\n--- BRANCH GENERATION COMPLETE ---\n");
    }

    private bool BuildRegularBranchRecursive(DynamicRoom currentRoom, RoomDoor activeExit, int step, int maxDepth, ref int rootFails)
    {
        if (rootFails >= maxBranchFails) return false;

        GD.Print($"      [Branch Step {step}/{maxDepth}] Generating candidates...");

        bool useHallway = _rng.Randf() < hallwayChance && hallwayScenes.Count > 0;
        
        List<PackedScene> primaryCandidates = useHallway ? hallwayScenes.ToList() : standardRoomScenes.ToList();
        List<PackedScene> secondaryCandidates = useHallway ? standardRoomScenes.ToList() : hallwayScenes.ToList();
        
        ShuffleList(primaryCandidates);
        ShuffleList(secondaryCandidates);
        
        List<PackedScene> candidates = new List<PackedScene>();
        candidates.AddRange(primaryCandidates);
        candidates.AddRange(secondaryCandidates);

        foreach (PackedScene prefab in candidates)
        {
            if (rootFails >= maxBranchFails) return false; 

            bool isHallway = hallwayScenes.Contains(prefab);
            DynamicRoom placedRoom = TryPlaceRoom(currentRoom, activeExit, prefab, out RoomDoor entranceUsed, DynamicRoom.RoomTypeEnum.Regular);
            
            if (placedRoom != null)
            {
                int nextStep = isHallway ? step : step + 1;
                GD.Print($"      [Branch Step {step}/{maxDepth}] {(isHallway ? "Hallway" : "Standard Room")} placed successfully!");

                if (nextStep > maxDepth) 
                {
                    GD.Print($"      [Target Reached] Branch depth of {maxDepth} completed.");
                    return true;
                }

                List<RoomDoor> nextExits = placedRoom.GetAvailableDoors();
                ShuffleList(nextExits);
                
                bool branchContinued = false;
                foreach (RoomDoor nextExit in nextExits)
                {
                    if (BuildRegularBranchRecursive(placedRoom, nextExit, nextStep, maxDepth, ref rootFails))
                    {
                        branchContinued = true;
                        break; 
                    }
                }

                if (branchContinued) return true;

                GD.Print($"      [Backtrack] Dead end from room placed at Branch Step {step}. Undoing placement...");
                UndoPlacement(placedRoom, activeExit, entranceUsed);
                rootFails++;
                if (step == 1) 
                {
                    
                    GD.Print($"      [Root Fail] Starting door failed pathing. Fail count: {rootFails}/{maxBranchFails}");
                }
            }
        }
        
        GD.Print($"      [Branch Step {step} Failed] All prefabs collided or lacked exits. Returning to previous room...");
        return false; 
    }

    private bool BuildLoopBranchRecursive(DynamicRoom currentRoom, RoomDoor activeExit, RoomDoor targetDoor, int step)
    {
        if (step > maxLoopBranchRooms) return false;

        GD.Print($"      [Loop Step {step}/{maxLoopBranchRooms}] Trying to path to target door...");
        List<PackedScene> candidateRooms = new List<PackedScene>();
        candidateRooms.AddRange(standardRoomScenes);
        candidateRooms.AddRange(hallwayScenes);
        ShuffleList(candidateRooms);

        foreach (PackedScene prefab in candidateRooms)
        {
            DynamicRoom placedRoom = TryPlaceRoom(currentRoom, activeExit, prefab, out RoomDoor entranceUsed, DynamicRoom.RoomTypeEnum.Regular);
            
            if (placedRoom != null)
            {
                bool reachedTarget = false;
                List<RoomDoor> newRoomDoors = placedRoom.GetAvailableDoors();

                foreach (RoomDoor door in newRoomDoors)
                {
                    if (!door.CanConnectTo(targetDoor)) continue;

                    Vector3 posA = (placedRoom.Transform * placedRoom.GetRelativeTransform(door)).Origin;
                    Vector3 posB = (targetDoor.ParentRoom.Transform * targetDoor.ParentRoom.GetRelativeTransform(targetDoor)).Origin;
                    Vector3 expectedOffset = GetSpacingOffset(door.cardinalDirection);

                    if (posA.DistanceTo(posB - expectedOffset) < 0.5f && step >= minLoopBranchRooms)
                    {
                        GD.Print($"      [Loop Success] Perfect physical connection made to target door!");
                        door.SetConnected(true);
                        targetDoor.SetConnected(true);
                        door.ConnectedTo = targetDoor;
                        targetDoor.ConnectedTo = door;
                        placedRoom.ConnectedRooms.Add(targetDoor.ParentRoom);
                        targetDoor.ParentRoom.ConnectedRooms.Add(placedRoom);
                        reachedTarget = true;
                        break;
                    }
                }

                if (reachedTarget) return true;

                if (step < maxLoopBranchRooms && newRoomDoors.Count > 0)
                {
                    Vector3 targetPos = (targetDoor.ParentRoom.Transform * targetDoor.ParentRoom.GetRelativeTransform(targetDoor)).Origin;
                    RoomDoor bestExit = newRoomDoors.OrderBy(d => 
                        ((placedRoom.Transform * placedRoom.GetRelativeTransform(d)).Origin).DistanceTo(targetPos)
                    ).FirstOrDefault();

                    if (bestExit != null)
                    {
                        if (BuildLoopBranchRecursive(placedRoom, bestExit, targetDoor, step + 1)) return true;
                    }
                }

                GD.Print($"      [Loop Backtrack] Undoing room placement at loop step {step}.");
                UndoPlacement(placedRoom, activeExit, entranceUsed);
                break; 
            }
        }
        return false; 
    }

    private void GenerateChanceLoops()
    {
        GD.Print("\n--- STARTING CHANCE LOOPS ---");
        float alignmentTolerance = 0.5f;
        int loopCount = 0;

        foreach (DynamicRoom roomA in placedRooms)
        {
            foreach (RoomDoor doorA in roomA.GetAvailableDoors())
            {
                foreach (DynamicRoom roomB in placedRooms)
                {
                    if (roomA == roomB) continue;

                    foreach (RoomDoor doorB in roomB.GetAvailableDoors())
                    {
                        if (!doorA.CanConnectTo(doorB)) continue;

                        Vector3 posA = (roomA.Transform * roomA.GetRelativeTransform(doorA)).Origin;
                        Vector3 posB = (roomB.Transform * roomB.GetRelativeTransform(doorB)).Origin;
                        Vector3 expectedOffset = GetSpacingOffset(doorA.cardinalDirection);

                        if (posA.DistanceTo(posB - expectedOffset) < alignmentTolerance)
                        {
                            int navDistance = GetNavigationDistance(roomA, roomB);

                            if ((navDistance >= minChanceLoopDistance && navDistance <= maxChanceLoopDistance) || navDistance == -1)
                            {
                                doorA.SetConnected(true);
                                doorB.SetConnected(true);
                                doorA.ConnectedTo = doorB;
                                doorB.ConnectedTo = doorA;
                                roomA.ConnectedRooms.Add(roomB);
                                roomB.ConnectedRooms.Add(roomA);
                                loopCount++;
                                GD.Print($"  -> [Chance Loop Formed] Connected a room to another room over a nav distance of {navDistance}");
                            }
                        }
                    }
                }
            }
        }
        GD.Print($"--- CHANCE LOOPS COMPLETE (Formed {loopCount} loops) ---\n");
    }

    private int GetNavigationDistance(DynamicRoom start, DynamicRoom target)
    {
        if (start == target) return 0;

        Queue<Tuple<DynamicRoom, int>> queue = new Queue<Tuple<DynamicRoom, int>>();
        HashSet<DynamicRoom> visited = new HashSet<DynamicRoom>();

        queue.Enqueue(new Tuple<DynamicRoom, int>(start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            DynamicRoom currentRoom = current.Item1;
            int currentDist = current.Item2;

            if (currentRoom == target) return currentDist;

            foreach (DynamicRoom neighbor in currentRoom.ConnectedRooms)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(new Tuple<DynamicRoom, int>(neighbor, currentDist + 1));
                }
            }
        }
        return -1;
    }

    private void UndoPlacement(DynamicRoom room, RoomDoor exitDoorUsed, RoomDoor entranceDoorUsed)
    {
        GD.Print($"[Undo] Severing door connections and freeing room from memory.");
        exitDoorUsed.SetConnected(false);
        entranceDoorUsed.SetConnected(false);

        exitDoorUsed.ConnectedTo = null;
        entranceDoorUsed.ConnectedTo = null;
        exitDoorUsed.ParentRoom.ConnectedRooms.Remove(room);

        placedRooms.Remove(room);
        room.QueueFree();
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