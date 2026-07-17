using Godot;
using System;

public partial class Room : StaticBody3D
{
	//If one of the booleans get sets to True, the Bridge/Wall will be deleted.
	[Export] public bool removeBridgeNorth = false;
	[Export] public bool removeBridgeEast = false;
	[Export] public bool removeBridgeSouth = false;
	[Export] public bool removeBridgeWest = false;
	[Export] public bool removeAllBridges = false;

	[Export] public bool removeWallNorth = false;
	[Export] public bool removeWallEast = false;
	[Export] public bool removeWallSouth = false;
	[Export] public bool removeWallWest = false;
	[Export] public bool removeAllWalls = false;

	[Export] public Node BridgeNorth;
	[Export] public Node BridgeEast;
	[Export] public Node BridgeSouth;
	[Export] public Node BridgeWest;

	[Export] public Node WallNorth;
	[Export] public Node WallEast;
	[Export] public Node WallSouth;
	[Export] public Node WallWest;

	[Export] public Sprite3D StartSprite;
	[Export] public Sprite3D EndSprite;
	[Export] public Sprite3D MainSprite;

	[Export] public Node3D SpawnPoint1;
	[Export] public Node3D SpawnPoint2;
	[Export] public Node3D SpawnPoint3;
	[Export] public Node3D SpawnPoint4;

	private Random random;
	private PackedScene item = GD.Load<PackedScene>("res://Loot.tscn");

	

	public void SpawnElements()
	{
		//RandomizeSpawnPoint(SpawnPoint1);
		//RandomizeSpawnPoint(SpawnPoint2);
		//RandomizeSpawnPoint(SpawnPoint3);
		//RandomizeSpawnPoint(SpawnPoint4);

		if (random.Next(0, 100) >= 50)
		{
			Node3D itemInstance = (Node3D)item.Instantiate();
			itemInstance.Position = SpawnPoint1.Position;
			AddChild(itemInstance);
		}
		if (random.Next(0, 100) >= 50)
		{
			Node3D itemInstance = (Node3D)item.Instantiate();
			itemInstance.Position = SpawnPoint2.Position;
			AddChild(itemInstance);
		}
		if (random.Next(0, 100) >= 50)
		{
			Node3D itemInstance = (Node3D)item.Instantiate();
			itemInstance.Position = SpawnPoint3.Position;
			AddChild(itemInstance);
		}
		if (random.Next(0, 100) >= 50)
		{
			Node3D itemInstance = (Node3D)item.Instantiate();
			itemInstance.Position = SpawnPoint4.Position;
			AddChild(itemInstance);
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


	public void setStartTile()
	{
		StartSprite.Visible = true;
	}

	public void setEndTile()
	{
		EndSprite.Visible = true;
	}
	public void setMainTile()
	{
		MainSprite.Visible = true;
	}


	/// <summary>
	/// Checks the boolean variables of the Class, and deletes all the Brides and Walls acordingly.
	/// </summary>
	public void setRoom()
	{
		if (removeAllBridges)
		{
			BridgeNorth.QueueFree();
			BridgeEast.QueueFree();
			BridgeSouth.QueueFree();
			BridgeWest.QueueFree();
		}

		if (removeAllWalls)
		{
			WallNorth.QueueFree();
			WallEast.QueueFree();
			WallSouth.QueueFree();
			WallWest.QueueFree();
		}

		if (removeBridgeNorth)
		{
			BridgeNorth.QueueFree();
		}
		if (removeBridgeEast)
		{
			BridgeEast.QueueFree();
		}
		if (removeBridgeSouth)
		{
			BridgeSouth.QueueFree();
		}
		if (removeBridgeWest)
		{
			BridgeWest.QueueFree();
		}
		if (removeWallNorth)
		{
			WallNorth.QueueFree();
		}
		if (removeWallEast)
		{
			WallEast.QueueFree();
		}
		if (removeWallSouth)
		{
			WallSouth.QueueFree();
		}
		if (removeWallWest)
		{
			WallWest.QueueFree();
		}
	}
}
