using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class RoomBuilder : Node3D
{
    public enum TileType
    {
        Empty = 0,
        Floor = 1,
        DoorNorth = 2, DoorEast = 3, DoorSouth = 4, DoorWest = 5,
        WallNorth = 6, WallEast = 7, WallSouth = 8, WallWest = 9,
        CornerNorthEast = 10, CornerSouthEast = 11, CornerSouthWest = 12, CornerNorthWest = 13
    }

    [ExportGroup("Grid Settings")]
    [Export]
    public int GridWidth
    {
        get => _gridWidth;
        set
        {
            _gridWidth = Mathf.Max(1, value);
            AutoSaveRoom();
            BuildRoom3D();
        }
    }
    [Export]
    public int GridHeight
    {
        get => _gridHeight;
        set
        {
            _gridHeight = Mathf.Max(1, value);
            AutoSaveRoom();
            BuildRoom3D();
        }
    }

    private int _gridWidth = 5;
    private int _gridHeight = 5;
    private const float GridTileSize = 5.0f;
    private const float HalfTileSize = 2.5f;

    [ExportGroup("Assets & Scenes")]
    [Export] public PackedScene FloorMeshScene;
    [Export] public PackedScene WallMeshScene;
    [Export] public PackedScene DoorScene;

    [ExportGroup("Actions")]
    [Export]
    public bool TriggerBuildPreview
    {
        get => false;
        set { if (value) BuildRoom3D(); }
    }

    private Dictionary<Vector2I, TileType> _roomGrid = new Dictionary<Vector2I, TileType>();
    private Dictionary<Vector2I, List<Node>> _tileNodes = new Dictionary<Vector2I, List<Node>>();

    private Node3D _meshesRoot;
    private Node3D _visualsFolder;
    private Node3D _doorsFolder;

    public override void _Ready()
    {
        // Only run automatic startup build in the editor inspector preview
        if (Engine.IsEditorHint())
        {
            LoadRoomFromParent();
            BuildRoom3D();
        }
    }

    public void LoadRoomFromParent()
    {
        Room roomScript = GetParent<Room>();
        if (roomScript == null) return;

        if (roomScript.gridWidth > 0) _gridWidth = roomScript.gridWidth;
        if (roomScript.gridHeight > 0) _gridHeight = roomScript.gridHeight;

        _roomGrid.Clear();
        if (roomScript.gridMapData != null)
        {
            foreach (var kvp in roomScript.gridMapData)
            {
                if (kvp.Value > 0 && Enum.IsDefined(typeof(TileType), kvp.Value))
                {
                    _roomGrid[kvp.Key] = (TileType)kvp.Value;
                }
            }
        }
    }

    public void SetTile(Vector2I gridPos, TileType type)
    {
        if (gridPos.X >= 0 && gridPos.X < _gridWidth && gridPos.Y >= 0 && gridPos.Y < _gridHeight)
        {
            TileType currentTile = GetTile(gridPos);
            if (currentTile == type) return; // Optimization: do nothing if the tile is unchanged

            if (type == TileType.Empty)
                _roomGrid.Remove(gridPos);
            else
                _roomGrid[gridPos] = type;

            AutoSaveRoom();

            // Incremental 3D update: update only this specific tile position instantly without stuttering!
            UpdateSingleTile3D(gridPos, type);
        }
    }

    public TileType GetTile(Vector2I gridPos)
    {
        return _roomGrid.ContainsKey(gridPos) ? _roomGrid[gridPos] : TileType.Empty;
    }

    public void BuildRoom3D()
    {
        EnsureFolderStructure();
        ClearExistingChildren();
        _tileNodes.Clear();

        float offsetX = (_gridWidth % 2 == 0) ? (_gridWidth / 2.0f) * GridTileSize - (GridTileSize / 2.0f) : (_gridWidth / 2) * GridTileSize;
        float offsetZ = (_gridHeight % 2 == 0) ? (_gridHeight / 2.0f) * GridTileSize - (GridTileSize / 2.0f) : (_gridHeight / 2) * GridTileSize;

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                Vector2I pos = new Vector2I(x, y);
                TileType tile = GetTile(pos);
                if (tile == TileType.Empty) continue;

                int invX = _gridWidth - 1 - x;
                int invY = _gridHeight - 1 - y;
                Vector3 worldPos = new Vector3((invX * GridTileSize) - offsetX, 0, (invY * GridTileSize) - offsetZ);

                List<Node> spawned = SpawnTileVisuals(pos, tile, worldPos);
                if (spawned.Count > 0)
                {
                    _tileNodes[pos] = spawned;
                }
            }
        }

        GD.Print("[RoomBuilder] 3D Room Preview Built Successfully.");
    }

    private void UpdateSingleTile3D(Vector2I gridPos, TileType newType)
    {
        EnsureFolderStructure();

        // 1. Remove existing nodes for this specific grid position
        if (_tileNodes.TryGetValue(gridPos, out var oldNodes))
        {
            Room roomScript = GetParent<Room>();
            foreach (var node in oldNodes)
            {
                if (node is Door door && roomScript != null && roomScript.Doors != null)
                {
                    roomScript.Doors.Remove(door);
                }
                if (IsInstanceValid(node))
                {
                    node.QueueFree();
                }
            }
            _tileNodes.Remove(gridPos);
        }

        // 2. If new type is not empty, spawn new visuals/doors for this position
        if (newType != TileType.Empty)
        {
            float offsetX = (_gridWidth % 2 == 0) ? (_gridWidth / 2.0f) * GridTileSize - (GridTileSize / 2.0f) : (_gridWidth / 2) * GridTileSize;
            float offsetZ = (_gridHeight % 2 == 0) ? (_gridHeight / 2.0f) * GridTileSize - (GridTileSize / 2.0f) : (_gridHeight / 2) * GridTileSize;

            int invX = _gridWidth - 1 - gridPos.X;
            int invY = _gridHeight - 1 - gridPos.Y;
            Vector3 worldPos = new Vector3((invX * GridTileSize) - offsetX, 0, (invY * GridTileSize) - offsetZ);

            List<Node> spawned = SpawnTileVisuals(gridPos, newType, worldPos);
            if (spawned.Count > 0)
            {
                _tileNodes[gridPos] = spawned;
            }
        }
    }

    private void EnsureFolderStructure()
    {
        Room roomScript = GetParent<Room>();

        if (_meshesRoot == null || !IsInstanceValid(_meshesRoot))
        {
            _meshesRoot = GetNodeOrNull<Node3D>("Meshes");
            if (_meshesRoot == null)
            {
                _meshesRoot = new Node3D { Name = "Meshes" };
                AddChild(_meshesRoot);
                SafeSetOwner(_meshesRoot);
            }
        }

        if (_visualsFolder == null || !IsInstanceValid(_visualsFolder))
        {
            _visualsFolder = _meshesRoot.GetNodeOrNull<Node3D>("Visuals");
            if (_visualsFolder == null)
            {
                _visualsFolder = new Node3D { Name = "Visuals" };
                _meshesRoot.AddChild(_visualsFolder);
                SafeSetOwner(_visualsFolder);
            }
        }

        if (roomScript != null)
        {
            if (roomScript.doorFolder != null && IsInstanceValid(roomScript.doorFolder))
            {
                _doorsFolder = roomScript.doorFolder;
            }
            else
            {
                if (_doorsFolder == null || !IsInstanceValid(_doorsFolder))
                {
                    _doorsFolder = roomScript.GetNodeOrNull<Node3D>("Doors");
                    if (_doorsFolder == null)
                    {
                        _doorsFolder = new Node3D { Name = "Doors" };
                        _meshesRoot.AddChild(_doorsFolder);
                        SafeSetOwner(_doorsFolder);
                    }
                    roomScript.doorFolder = _doorsFolder;
                }
            }
        }
        else
        {
            if (_doorsFolder == null || !IsInstanceValid(_doorsFolder))
            {
                _doorsFolder = _meshesRoot.GetNodeOrNull<Node3D>("Doors");
                if (_doorsFolder == null)
                {
                    _doorsFolder = new Node3D { Name = "Doors" };
                    _meshesRoot.AddChild(_doorsFolder);
                    SafeSetOwner(_doorsFolder);
                }
            }
        }
    }

    private void ClearExistingChildren()
    {
        if (_visualsFolder != null)
        {
            foreach (Node child in _visualsFolder.GetChildren()) child.QueueFree();
        }

        if (_doorsFolder != null)
        {
            foreach (Node child in _doorsFolder.GetChildren()) child.QueueFree();
        }

        Room roomScript = GetParent<Room>();
        if (roomScript != null && roomScript.Doors != null)
        {
            roomScript.Doors.Clear();
        }
    }

    private void SafeSetOwner(Node node)
    {
        if (!Engine.IsEditorHint()) return;
        Node sceneRoot = GetTree()?.EditedSceneRoot;
        if (sceneRoot != null && (sceneRoot == node.GetParent() || sceneRoot.IsAncestorOf(node)))
        {
            node.Owner = sceneRoot;
        }
    }

    private List<Node> SpawnTileVisuals(Vector2I gridPos, TileType tile, Vector3 worldPos)
    {
        List<Node> spawned = new List<Node>();
        string coordName = $"{tile}_{gridPos.X}_{gridPos.Y}";

        if (tile == TileType.Floor || (tile >= TileType.DoorNorth && tile <= TileType.DoorWest) || tile >= TileType.WallNorth)
        {
            if (FloorMeshScene != null)
            {
                Node3D floor = (Node3D)FloorMeshScene.Instantiate();
                _visualsFolder.AddChild(floor);
                SafeSetOwner(floor);
                floor.Position = worldPos;
                floor.Name = coordName;
                spawned.Add(floor);
            }
        }

        if (tile >= TileType.WallNorth && tile <= TileType.WallWest)
        {
            Node3D wall = SpawnWall(coordName, worldPos + GetEdgeOffset(tile), tile);
            if (wall != null) spawned.Add(wall);
        }
        else if (tile >= TileType.DoorNorth && tile <= TileType.DoorWest)
        {
            Door door = SpawnDoor(coordName, gridPos, tile, worldPos + GetEdgeOffset(tile));
            if (door != null) spawned.Add(door);
        }
        else if (tile >= TileType.CornerNorthEast && tile <= TileType.CornerNorthWest)
        {
            TileType wallA = TileType.WallNorth;
            TileType wallB = TileType.WallEast;
            switch (tile)
            {
                case TileType.CornerNorthEast: wallA = TileType.WallNorth; wallB = TileType.WallEast; break;
                case TileType.CornerSouthEast: wallA = TileType.WallSouth; wallB = TileType.WallEast; break;
                case TileType.CornerSouthWest: wallA = TileType.WallSouth; wallB = TileType.WallWest; break;
                case TileType.CornerNorthWest: wallA = TileType.WallNorth; wallB = TileType.WallWest; break;
            }
            Node3D wall1 = SpawnWall($"{coordName}_A", worldPos + GetEdgeOffset(wallA), wallA);
            Node3D wall2 = SpawnWall($"{coordName}_B", worldPos + GetEdgeOffset(wallB), wallB);
            if (wall1 != null) spawned.Add(wall1);
            if (wall2 != null) spawned.Add(wall2);
        }

        return spawned;
    }

    private Vector3 GetEdgeOffset(TileType tile)
    {
        return tile switch
        {
            TileType.WallNorth or TileType.DoorNorth => new Vector3(0, 0, HalfTileSize),
            TileType.WallEast or TileType.DoorEast => new Vector3(-HalfTileSize, 0, 0),
            TileType.WallSouth or TileType.DoorSouth => new Vector3(0, 0, -HalfTileSize),
            TileType.WallWest or TileType.DoorWest => new Vector3(HalfTileSize, 0, 0),
            _ => Vector3.Zero
        };
    }

    private float GetDirectionRotation(TileType tile)
    {
        float baseRot = tile switch
        {
            TileType.WallNorth or TileType.DoorNorth => 0f,
            TileType.WallEast or TileType.DoorEast => 90f,
            TileType.WallSouth or TileType.DoorSouth => 180f,
            TileType.WallWest or TileType.DoorWest => 270f,
            _ => 0f
        };
        return (baseRot + 90f) % 360f;
    }

    private Node3D SpawnWall(string nodeName, Vector3 edgePos, TileType wallTile)
    {
        if (WallMeshScene == null)
        {
            GD.PushWarning("No Wall Scene is Loaded");
            return null;
        }
        Node3D wall = (Node3D)WallMeshScene.Instantiate();
        _visualsFolder.AddChild(wall);
        SafeSetOwner(wall);
        wall.Position = edgePos;
        wall.RotationDegrees = new Vector3(0, GetDirectionRotation(wallTile), 0);
        wall.Name = nodeName;
        return wall;
    }

    private Door SpawnDoor(string nodeName, Vector2I gridPos, TileType tile, Vector3 edgePos)
    {
        if (DoorScene == null)
        {
            GD.PushWarning("No Door Scene is Loaded");
            return null;
        }

        Door doorNode = DoorScene.Instantiate<Door>();
        doorNode.RoomGridPosition = gridPos;

        if (tile == TileType.DoorNorth) doorNode.cardinalDirection = Door.DoorDirection.North;
        else if (tile == TileType.DoorEast) doorNode.cardinalDirection = Door.DoorDirection.East;
        else if (tile == TileType.DoorSouth) doorNode.cardinalDirection = Door.DoorDirection.South;
        else if (tile == TileType.DoorWest) doorNode.cardinalDirection = Door.DoorDirection.West;

        _doorsFolder.AddChild(doorNode);
        SafeSetOwner(doorNode);
        doorNode.Position = edgePos;
        doorNode.RotationDegrees = new Vector3(0, GetDirectionRotation(tile), 0);
        doorNode.Name = nodeName;

        Room roomScript = GetParent<Room>();
        if (roomScript != null && roomScript.Doors != null)
        {
            if (!roomScript.Doors.Contains(doorNode))
            {
                roomScript.Doors.Add(doorNode);
            }
        }
        return doorNode;
    }

    public void AutoSaveRoom()
    {
        Room roomScript = GetParent<Room>();
        if (roomScript == null) return;

        roomScript.gridWidth = _gridWidth;
        roomScript.gridHeight = _gridHeight;
        roomScript.gridMapData.Clear();

        foreach (var kvp in _roomGrid)
        {
            if (kvp.Value != TileType.Empty)
            {
                roomScript.gridMapData[kvp.Key] = (int)kvp.Value;
            }
        }
        roomScript.NotifyPropertyListChanged();
    }
}