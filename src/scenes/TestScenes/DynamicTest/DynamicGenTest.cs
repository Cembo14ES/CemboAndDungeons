using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DynamicGenTest : Node3D
{
    [ExportGroup("Room Prefabs")]
    [Export] public PackedScene StartRoomScene;
    [Export] public PackedScene EndRoomScene;
    [Export] public Godot.Collections.Array<PackedScene> StandardRoomScenes;
    [Export] public Godot.Collections.Array<PackedScene> HallwayScenes;

    [ExportGroup("Generation Settings")]
    [Export] public bool useSeed = false;
    [Export] public int GenerationSeed = 0;
    [Export] public int MainPathLength = 6;
    [Export] public float HallwayChance = 0.3f;
    [Export] public float BranchChance = 0.4f;

    [ExportGroup("Spacing Settings")]
    [Export] public float RoomSpacing = 0.0f;
    [Export] public int MinLoopDistance = 3;

    public List<DynamicRoom> placedRooms = new List<DynamicRoom>();
    private Random _rng;

    public override void _Ready()
    {
        if (useSeed)
        {
            _rng = new Random(GenerationSeed);
            GD.Print($"Seed: " + GenerationSeed);
        }
        else
            _rng = new Random();
    }

    public void GenerateLevel()
    {
        DynamicRoom startRoom = StartRoomScene.Instantiate<DynamicRoom>();
        startRoom.InitializeRoom(); // FIX: Safely runs initialization setup
        startRoom.Transform = Transform3D.Identity;
        placedRooms.Add(startRoom);

        bool success = BuildPathRecursive(startRoom, 1);
        if (!success)
        {
            GD.PrintErr("Failed to generate main path.");
            return;
        }

        GenerateBranches();
        GenerateLoops();
        FinalizeLevel();
    }

    private bool BuildPathRecursive(DynamicRoom currentRoom, int currentStep)
    {
        if (currentStep >= MainPathLength) return true;

        bool isFinalRoom = (currentStep == MainPathLength - 1);
        List<RoomDoor> availableExits = currentRoom.GetAvailableDoors();
        ShuffleList(availableExits);

        foreach (RoomDoor exitDoor in availableExits)
        {
            List<PackedScene> roomChoices = isFinalRoom ? new List<PackedScene> { EndRoomScene } : StandardRoomScenes.ToList();
            ShuffleList(roomChoices);

            foreach (PackedScene roomPrefab in roomChoices)
            {
                bool useHallway = (!isFinalRoom && _rng.NextDouble() < HallwayChance);
                DynamicRoom roomToConnectFrom = currentRoom;
                RoomDoor activeExit = exitDoor;

                DynamicRoom placedHallway = null;
                RoomDoor hallwayEntranceUsed = null;

                if (useHallway && HallwayScenes.Count > 0)
                {
                    List<PackedScene> hallways = HallwayScenes.ToList();
                    ShuffleList(hallways);

                    foreach (PackedScene hallway in hallways)
                    {
                        placedHallway = TryPlaceRoom(roomToConnectFrom, activeExit, hallway, out hallwayEntranceUsed);

                        if (placedHallway != null)
                        {
                            roomToConnectFrom = placedHallway;
                            var hallwayExits = placedHallway.GetAvailableDoors();
                            if (hallwayExits.Count > 0) activeExit = hallwayExits[_rng.Next(hallwayExits.Count)];
                            break;
                        }
                    }
                }

                DynamicRoom placedRoom = TryPlaceRoom(roomToConnectFrom, activeExit, roomPrefab, out RoomDoor roomEntranceUsed);

                if (placedRoom != null)
                {
                    if (BuildPathRecursive(placedRoom, currentStep + 1)) return true;
                    UndoPlacement(placedRoom, activeExit, roomEntranceUsed);
                }

                if (placedHallway != null) UndoPlacement(placedHallway, exitDoor, hallwayEntranceUsed);
            }
        }
        return false;
    }

    private DynamicRoom TryPlaceRoom(DynamicRoom exitRoom, RoomDoor exitDoor, PackedScene prefab, out RoomDoor entranceUsed)
    {
        entranceUsed = null;
        DynamicRoom newRoom = prefab.Instantiate<DynamicRoom>();
        newRoom.InitializeRoom(); // FIX: Safely runs initialization setup

        List<int> rotationAngles = new List<int> { 0, 90, 180, 270 };
        ShuffleList(rotationAngles);

        foreach (int angle in rotationAngles)
        {
            newRoom.SetRotation(angle);

            List<RoomDoor> entrances = newRoom.GetAvailableDoors();
            ShuffleList(entrances);

            foreach (RoomDoor entrance in entrances)
            {
                if (!entrance.CanConnectTo(exitDoor)) continue;

                AlignRooms(exitRoom, exitDoor, newRoom, entrance);

                if (!CheckOverlap(newRoom, exitRoom, RoomSpacing / 2f))
                {
                    exitDoor.isConnected = true;
                    entrance.isConnected = true;
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

        Vector3 spacingOffset = GetSpacingOffset(exitDoor.Direction);
        newRoom.Position = exitDoorWorldPos + spacingOffset - entranceDoorWorldPos;
    }

    private Vector3 GetSpacingOffset(RoomDoor.DoorDirection direction)
    {
        return direction switch
        {
            RoomDoor.DoorDirection.North => new Vector3(0, 0, RoomSpacing),
            RoomDoor.DoorDirection.South => new Vector3(0, 0, -RoomSpacing),
            RoomDoor.DoorDirection.East => new Vector3(-RoomSpacing, 0, 0),
            RoomDoor.DoorDirection.West => new Vector3(RoomSpacing, 0, 0),
            _ => Vector3.Zero
        };
    }

    private bool CheckOverlap(DynamicRoom newRoom, DynamicRoom parentRoom, float margin)
    {
        foreach (DynamicRoom placedRoom in placedRooms)
        {
            if (placedRoom == parentRoom) continue;
            if (newRoom.IntersectsWith(placedRoom, margin)) return true;
        }
        return false;
    }

    private void GenerateLoops()
    {
        float alignmentTolerance = 0.5f;

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

                        Vector3 expectedOffset = GetSpacingOffset(doorA.Direction);

                        if (posA.DistanceTo(posB - expectedOffset) < alignmentTolerance)
                        {
                            int navDistance = GetNavigationDistance(roomA, roomB);

                            if (navDistance >= MinLoopDistance || navDistance == -1)
                            {
                                doorA.isConnected = true;
                                doorB.isConnected = true;
                                doorA.ConnectedTo = doorB;
                                doorB.ConnectedTo = doorA;
                                roomA.ConnectedRooms.Add(roomB);
                                roomB.ConnectedRooms.Add(roomA);
                            }
                        }
                    }
                }
            }
        }
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

    private void GenerateBranches()
    {
        foreach (DynamicRoom room in placedRooms.ToList())
        {
            foreach (RoomDoor branchDoor in room.GetAvailableDoors())
            {
                if (branchDoor.isConnected || _rng.NextDouble() > BranchChance) continue;

                bool useHallway = _rng.NextDouble() < HallwayChance && HallwayScenes.Count > 0;
                DynamicRoom roomToConnectFrom = room;
                RoomDoor activeExit = branchDoor;

                DynamicRoom placedHallway = null;
                RoomDoor hallwayEntranceUsed = null;

                if (useHallway)
                {
                    List<PackedScene> hallways = HallwayScenes.ToList();
                    ShuffleList(hallways);

                    foreach (PackedScene hallway in hallways)
                    {
                        placedHallway = TryPlaceRoom(roomToConnectFrom, activeExit, hallway, out hallwayEntranceUsed);
                        if (placedHallway != null)
                        {
                            roomToConnectFrom = placedHallway;
                            var hallwayExits = placedHallway.GetAvailableDoors();
                            if (hallwayExits.Count > 0) activeExit = hallwayExits[_rng.Next(hallwayExits.Count)];
                            break;
                        }
                    }
                }

                List<PackedScene> branchChoices = StandardRoomScenes.ToList();
                ShuffleList(branchChoices);
                DynamicRoom placedRoom = null;

                foreach (PackedScene branchPrefab in branchChoices)
                {
                    placedRoom = TryPlaceRoom(roomToConnectFrom, activeExit, branchPrefab, out _);
                    if (placedRoom != null) break;
                }

                if (placedRoom == null && placedHallway != null)
                {
                    UndoPlacement(placedHallway, branchDoor, hallwayEntranceUsed);
                }
            }
        }
    }

    private void UndoPlacement(DynamicRoom room, RoomDoor exitDoorUsed, RoomDoor entranceDoorUsed)
    {
        exitDoorUsed.isConnected = false;
        entranceDoorUsed.isConnected = false;

        exitDoorUsed.ConnectedTo = null;
        entranceDoorUsed.ConnectedTo = null;
        exitDoorUsed.ParentRoom.ConnectedRooms.Remove(room);

        placedRooms.Remove(room);
        room.QueueFree();
    }

    private void FinalizeLevel()
    {
        foreach (DynamicRoom room in placedRooms)
        {
            AddChild(room);
            // FIX: Pass the RoomSpacing configuration down to the door
            foreach (RoomDoor door in room.Doors) door.ResolveDoorState(RoomSpacing);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    
}