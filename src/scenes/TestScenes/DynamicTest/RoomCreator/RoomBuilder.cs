using Godot;
using System.Collections.Generic;

[Tool]
public partial class RoomBuilder : Node3D
{
	// Tile Types Mapping
	public enum TileType
	{
		Empty = 0,
		Floor = 1,
		WallNorth = 2, WallEast = 3, WallSouth = 4, WallWest = 5,
		CornerNorthEast = 6, CornerSouthEast = 7, CornerSouthWest = 8, CornerNorthWest = 9,
		DoorNorth = 10, DoorEast = 11, DoorSouth = 12, DoorWest = 13
	}

	[ExportGroup("Grid Settings")]
	[Export]
	public int GridWidth
	{
		get => _gridWidth;
		set { _gridWidth = Mathf.Max(1, value); }
	}
	[Export]
	public int GridHeight
	{
		get => _gridHeight;
		set { _gridHeight = Mathf.Max(1, value); }
	}

	private int _gridWidth = 5;
	private int _gridHeight = 5;

	[ExportGroup("Assets & Scenes")]
	[Export] public PackedScene FloorMeshScene;
	[Export] public PackedScene WallMeshScene;
	[Export] public PackedScene DoorScene; // Your existing RoomDoor scene

	[ExportGroup("Actions")]
	[Export]
	public bool TriggerBuildPreview
	{
		get => false;
		set { if (value) BuildRoom3D(); }
	}

	[Export]
	public bool TriggerSaveAndExport
	{
		get => false;
		set { if (value) SaveAndExportRoom(); }
	}

	// Internal grid data storage: Key = Vector2I(x, y), Value = TileType
	private Dictionary<Vector2I, TileType> _roomGrid = new Dictionary<Vector2I, TileType>();

	// References to generated folder structure
	private StaticBody3D _meshesRoot;
	private Node3D _collisionsFolder;
	private Node3D _doorsFolder;

	public override void _Ready()
	{
		// If the game is running (not in the editor), remove this tool node completely
		if (!Engine.IsEditorHint())
		{
			QueueFree();
		}
	}

	public void SetTile(Vector2I gridPos, TileType type)
	{
		if (gridPos.X >= 0 && gridPos.X < _gridWidth && gridPos.Y >= 0 && gridPos.Y < _gridHeight)
		{
			_roomGrid[gridPos] = type;
		}
	}

	public TileType GetTile(Vector2I gridPos)
	{
		return _roomGrid.ContainsKey(gridPos) ? _roomGrid[gridPos] : TileType.Empty;
	}

	/// <label>Builds the 3D visual representation based on the grid layout.</label>
	public void BuildRoom3D()
	{
		EnsureFolderStructure();
		ClearExistingChildren();

		// Calculate origin offset based on Odd/Even dimensions
		// Odd: Center on middle tile. Even: Center on vertex intersection of the 4 center tiles.
		float offsetX = (_gridWidth % 2 == 0) ? (_gridWidth / 2.0f) * 2.0f - 1.0f : (_gridWidth / 2) * 2.0f;
		float offsetZ = (_gridHeight % 2 == 0) ? (_gridHeight / 2.0f) * 2.0f - 1.0f : (_gridHeight / 2) * 2.0f;

		for (int x = 0; x < _gridWidth; x++)
		{
			for (int y = 0; y < _gridHeight; y++)
			{
				Vector2I pos = new Vector2I(x, y);
				TileType tile = GetTile(pos);
				if (tile == TileType.Empty) continue;

				// Calculate local 3D position (assuming 2.0 units per tile scale)
				Vector3 worldPos = new Vector3((x * 2.0f) - offsetX, 0, (y * 2.0f) - offsetZ);

				SpawnTileVisuals(pos, tile, worldPos);
			}
		}

		GD.Print("[RoomBuilder] 3D Room Preview Built Successfully.");
	}

	private void EnsureFolderStructure()
	{
		if (_meshesRoot == null || !IsInstanceValid(_meshesRoot))
		{
			_meshesRoot = GetNodeOrNull<StaticBody3D>("Meshes");
			if (_meshesRoot == null)
			{
				_meshesRoot = new StaticBody3D { Name = "Meshes" };
				AddChild(_meshesRoot);
				_meshesRoot.Owner = GetTree().EditedSceneRoot;
			}
		}

		if (_collisionsFolder == null || !IsInstanceValid(_collisionsFolder))
		{
			_collisionsFolder = _meshesRoot.GetNodeOrNull<Node3D>("Collisions");
			if (_collisionsFolder == null)
			{
				_collisionsFolder = new Node3D { Name = "Collisions" };
				_meshesRoot.AddChild(_collisionsFolder);
				_collisionsFolder.Owner = GetTree().EditedSceneRoot;
			}
		}

		if (_doorsFolder == null || !IsInstanceValid(_doorsFolder))
		{
			_doorsFolder = _meshesRoot.GetNodeOrNull<Node3D>("Doors");
			if (_doorsFolder == null)
			{
				_doorsFolder = new Node3D { Name = "Doors" };
				_meshesRoot.AddChild(_doorsFolder);
				_doorsFolder.Owner = GetTree().EditedSceneRoot;
			}
		}
	}

	private void ClearExistingChildren()
	{
		if (_collisionsFolder != null)
		{
			foreach (Node child in _collisionsFolder.GetChildren()) child.QueueFree();
		}
		if (_doorsFolder != null)
		{
			foreach (Node child in _doorsFolder.GetChildren()) child.QueueFree();
		}
	}

	private void SpawnTileVisuals(Vector2I gridPos, TileType tile, Vector3 worldPos)
	{
		// 1. Floor Tile
		if (tile == TileType.Floor || tile >= TileType.WallNorth)
		{
			if (FloorMeshScene != null)
			{
				Node3D floor = (Node3D)FloorMeshScene.Instantiate();
				_collisionsFolder.AddChild(floor);
				floor.Owner = GetTree().EditedSceneRoot;
				floor.Position = worldPos;
			}
		}

		// 2. Walls & Doors
		if (tile >= TileType.WallNorth && tile <= TileType.WallWest)
		{
			SpawnWall(worldPos, GetDirectionRotation(tile));
		}
		else if (tile >= TileType.DoorNorth && tile <= TileType.DoorWest)
		{
			SpawnDoor(gridPos, tile, worldPos);
		}
		else if (tile >= TileType.CornerNorthEast && tile <= TileType.CornerNorthWest)
		{
			// Corners use 2 wall models at 90-degree offsets
			float rot1 = 0f, rot2 = 0f;
			GetCornerRotations(tile, out rot1, out rot2);
			SpawnWall(worldPos, rot1);
			SpawnWall(worldPos, rot2);
		}
	}

	private void SpawnWall(Vector3 worldPos, float rotationY)
	{
		if (WallMeshScene == null) return;
		Node3D wall = (Node3D)WallMeshScene.Instantiate();
		_collisionsFolder.AddChild(wall);
		wall.Owner = GetTree().EditedSceneRoot;
		wall.Position = worldPos;
		wall.RotationDegrees = new Vector3(0, rotationY, 0);
	}

	private void SpawnDoor(Vector2I gridPos, TileType tile, Vector3 worldPos)
	{
		if (DoorScene == null) return;
		Node3D doorNode = (Node3D)DoorScene.Instantiate();
		_doorsFolder.AddChild(doorNode);
		doorNode.Owner = GetTree().EditedSceneRoot;
		doorNode.Position = worldPos;
		doorNode.RotationDegrees = new Vector3(0, GetDirectionRotation(tile), 0);

		// If your Door script is RoomDoor, assign its grid position
		if (doorNode is RoomDoor doorScript)
		{
			doorScript.RoomGridPosition = gridPos;
		}
	}

	private float GetDirectionRotation(TileType tile) => tile switch
	{
		TileType.WallNorth or TileType.DoorNorth => 0f,
		TileType.WallEast or TileType.DoorEast => 90f,
		TileType.WallSouth or TileType.DoorSouth => 180f,
		TileType.WallWest or TileType.DoorWest => 270f,
		_ => 0f
	};

	private void GetCornerRotations(TileType corner, out float r1, out float r2)
	{
		r1 = 0f; r2 = 90f;
		switch (corner)
		{
			case TileType.CornerNorthEast: r1 = 0f; r2 = 90f; break;
			case TileType.CornerSouthEast: r1 = 90f; r2 = 180f; break;
			case TileType.CornerSouthWest: r1 = 180f; r2 = 270f; break;
			case TileType.CornerNorthWest: r1 = 270f; r2 = 0f; break;
		}
	}

	/// <label>Serializes grid data into DynamicRoom format and generates optimized collisions & exact footprint Area3D.</label>
	public void SaveAndExportRoom()
	{
		DynamicRoom roomScript = GetParent<DynamicRoom>();
		if (roomScript == null)
		{
			GD.PrintErr("[RoomBuilder] Error: Root node must have a DynamicRoom script attached.");
			return;
		}

		roomScript.gridWidth = _gridWidth;
		roomScript.gridHeight = _gridHeight;
		roomScript.gridMapData.Clear();

		// Flatten grid data into gridMapData array expected by DynamicRoom
		for (int y = 0; y < _gridHeight; y++)
		{
			for (int x = 0; x < _gridWidth; x++)
			{
				TileType tile = GetTile(new Vector2I(x, y));
				// Map tool tile types back to your dungeon generator's expected format (e.g., 0: Empty, 1: Room/Floor, 2-5: Doors)
				int exportVal = 0;
				if (tile == TileType.Floor) exportVal = 1;
				else if (tile == TileType.DoorNorth) exportVal = 2;
				else if (tile == TileType.DoorEast) exportVal = 3;
				else if (tile == TileType.DoorSouth) exportVal = 4;
				else if (tile == TileType.DoorWest) exportVal = 5;
				else if (tile != TileType.Empty) exportVal = 1; // Walls/Corners as solid structure

				roomScript.gridMapData.Add(exportVal);
			}
		}

		GD.Print("[RoomBuilder] Room data serialized and saved to DynamicRoom successfully!");
	}
}