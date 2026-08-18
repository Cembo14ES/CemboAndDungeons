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
	public override void _Ready()
	{
		Debug_debugUI = (DebugUI) GetNode("/root/DebugLayer");
		DebugMaster.Instance.SignalDebug_RoomRenderToogle += UpdateAllRoomsStates;
		genAlg.GenerateLevel();

		rooms = genAlg.placedRooms;
		getSignals();
	}
	public override void _Process(double delta)
	{
		if (DebugMaster.Instance.levelDebugUIEnabled)
		{
			UpdateRoomInfo();
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
				room.SetActiveRoom();

			else if (activeRoom.ConnectedRooms.Contains(room))
				room.SetInactiveRoom();

			else
				room.SetDisabledRoom();
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
		else
			UpdateAllRoomsStates(playerCurrentRoom);
	}

	private void UpdateRoomInfo()
	{
		string title = "Room Info";
		string name = "Name: " + playerCurrentRoom.Name;
		string position = "Pos: " + playerCurrentRoom.Position.ToString();
		string entities = "Entities: 0";
		Debug_debugUI.UpdateRoomInfo(title + "\n\n" + name + "\n" + position + "\n" + entities);
	}
}
