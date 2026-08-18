using Godot;
using System;

public partial class Room : Node3D
{
	// SIGNALS
	[Signal] public delegate void ExitEnteredEventHandler();
	[Signal] public delegate void PlayerEnteredEventHandler(int x, int y);

	//OPENINGS MANAGEMENT BOOLEANS
	[ExportGroup("Opening Booleans")]
	[Export] public bool makePathNorth = false;
	[Export] public bool makePathEast = false;
	[Export] public bool makePathSouth = false;
	[Export] public bool makePathWest = false;
	[Export] public bool allPaths = false;

	//OPENINGS NODES
	[ExportGroup("Opening Nodes")]
	[Export] public Node BridgeNorth;
	[Export] public Node BridgeEast;
	[Export] public Node BridgeSouth;
	[Export] public Node BridgeWest;

	[Export] public Node WallNorth;
	[Export] public Node WallEast;
	[Export] public Node WallSouth;
	[Export] public Node WallWest;

	//TILE TYPE VISUALS
	[ExportGroup("Tile Type Nodes")]
	[Export] public Sprite3D StartSprite;
	[Export] private PackedScene end;
	[Export] public Sprite3D MainSprite;

	//ITEM SHENANIGANS
	[ExportGroup("Items")]
	[Export] private PackedScene item;
	[Export] public Godot.Collections.Array<Node3D> ItemSpawnPoints = new Godot.Collections.Array<Node3D>(); //The coordinates of the End Tiles.

	[Export] public Node3D northBridgeTip;
	[Export] public Node3D westBridgeTip;
	[Export] public Node3D southBridgeTip;
	[Export] public Node3D eastBridgeTip;
	//LOCAL VARIABLES

	private Random random;
	public Vector2I roomGridPos;
	public Godot.Collections.Array<Vector2I> roomGridNeighbours;

	public void SpawnElements()
	{
		//RandomizeSpawnPoint(SpawnPoint1);
		//RandomizeSpawnPoint(SpawnPoint2);
		//RandomizeSpawnPoint(SpawnPoint3);
		//RandomizeSpawnPoint(SpawnPoint4);

		foreach (Node3D spawnPoint in ItemSpawnPoints)
		{
			if (random.Next(0, 100) >= 50)
			{
				Node3D itemInstance = (Node3D)item.Instantiate();
				itemInstance.Position = spawnPoint.Position;
				AddChild(itemInstance);
			}
		}
	}

	public void setRoom()
	{
		if (allPaths)
		{
			WallNorth.QueueFree();
			WallEast.QueueFree();
			WallSouth.QueueFree();
			WallWest.QueueFree();
		}

		if (makePathNorth)
		{
			WallNorth.QueueFree();
		}
		else
		{
			BridgeNorth.QueueFree();
		}

		if (makePathEast)
		{
			WallEast.QueueFree();
		}
		else
		{
			BridgeEast.QueueFree();
		}

		if (makePathSouth)
		{
			WallSouth.QueueFree();
		}
		else
		{
			BridgeSouth.QueueFree();
		}

		if (makePathWest)
		{
			WallWest.QueueFree();
		}
		else
		{
			BridgeWest.QueueFree();
		}
	}

	private void RandomizeSpawnPoint(Node3D SpawnPoint)
	{
		float xDisplacement = (float)random.NextDouble();
		SpawnPoint.Position = new Vector3();
	}

	public void SetRandom(Random genRandom)
	{
		random = genRandom;
	}

	public void SetStartTile()
	{
		StartSprite.Visible = true;
	}

	public void SetEndTile()
	{
		EndLevel endInstance = (EndLevel)end.Instantiate();
		endInstance.Position = new Vector3(0, (float)0.751, 0);
		endInstance.ExitEntered += On_End;
		AddChild(endInstance);
	}
	public void SetMainTile()
	{
		MainSprite.Visible = true;
	}

	//Makes the Room visible and working
	public void SetActive()
	{
		Visible = true;
		ProcessMode = ProcessModeEnum.Inherit;
	}

	//Makes the Room visible, but it stops working (paused)
	public void SetInactive()
	{
		Visible = true;
		ProcessMode = ProcessModeEnum.Disabled;
	}

	//Makes the Room invisible and it stops working
	public void SetDisabled()
	{
		Visible = false;
		SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
	}

	public void On_End()
	{
		EmitSignal(SignalName.ExitEntered);
	}

	public void On_PlayerEntered(Node3D body)
	{
		if (body.Name == "Character")
		{
			EmitSignal(SignalName.PlayerEntered, roomGridPos.X, roomGridPos.Y);
		}
	}


	/// <summary>
	/// Checks the boolean variables of the Class, and deletes all the Brides and Walls acordingly.
	/// </summary>

}
