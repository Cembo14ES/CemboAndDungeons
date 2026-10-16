using Godot;
using System;
using System.Collections.Generic;

public partial class DynamicTest : Node3D
{
	[Export] public DynamicGenTest genAlg;
	[Export] public CharacterBody3D character;
	private List<DynamicRoom> rooms;
	private DynamicRoom playerCurrentRoom;
	private DebugUI Debug_debugUI;

	private bool Debug_renderAllRooms = false;

	public async override void _Ready()
	{
		Debug_debugUI = (DebugUI) GetNode("/root/DebugLayer");
		DebugMaster.Instance.SignalDebug_RoomRenderToogle += UpdateAllRoomsStates;

		// 1. Wait here until the generator finishes its background work
		bool success = await genAlg.GenerateLevel();

		if (!success)
		{
			GD.PrintErr("Level generation failed to complete.");
			return;
		}

		// 2. Safe to fetch info now that generation is 100% finished
		rooms = genAlg.placedRooms;
		getSignals();

		// 3. Safe to clean up the algorithm node
		genAlg.QueueFree();
	}

	public override void _Process(double delta)
	{
		if (DebugMaster.Instance.levelDebugUIEnabled && playerCurrentRoom != null)
		{
			Debug_UpdateRoomInfo();
		}
	}

	private void getSignals()
	{
		foreach (DynamicRoom room in rooms)
		{
			room.Signal_SetRoomStates += UpdateAllRoomsStates;
			foreach (RoomDoor door in room.Doors)
			{
				door.Signal_TeleportPlayer += On_TeleportPlayer;
			}
		}
	}

	private void On_TeleportPlayer(Vector3 pos)
	{
		character.Position = new Vector3(pos.X, character.Position.Y, pos.Z);
	}

	private void UpdateAllRoomsStates(DynamicRoom activeRoom)
	{
		playerCurrentRoom = activeRoom;
		foreach (DynamicRoom room in rooms)
		{
			if (room == activeRoom)
				room.SetRoomState(DynamicRoom.RoomStateEnum.Active);

			else if (activeRoom.ConnectedRooms.Contains(room))
				room.SetRoomState(DynamicRoom.RoomStateEnum.Inactive);

			else
				room.SetRoomState(DynamicRoom.RoomStateEnum.Disabled);
		}
		
		if (Debug_renderAllRooms)
		{
			UpdateAllRoomsStates(Debug_renderAllRooms);
		}
	}

	private void UpdateAllRoomsStates(bool state)
	{
		Debug_renderAllRooms = state;
		if (Debug_renderAllRooms)
		{
			foreach (DynamicRoom room in rooms)
				room.Visible = Debug_renderAllRooms;
		}
		else if (playerCurrentRoom != null)
		{
			UpdateAllRoomsStates(playerCurrentRoom);
		}
	}

	private void Debug_UpdateRoomInfo()
	{
		string title = "Room Info";
		string name = "Name: " + playerCurrentRoom.Name;
		string position = "Pos: " + playerCurrentRoom.Position.ToString();
		string roomtype = "Room type: " + playerCurrentRoom.roomType;
		
		Debug_debugUI.UpdateRoomInfo(title + "\n\n" + name + "\n" + position + "\n" + roomtype);
	}
}
