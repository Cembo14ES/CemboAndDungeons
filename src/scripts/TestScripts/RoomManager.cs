using Godot;
using System;

public partial class RoomManager : Node3D
{
	[Export] public bool Debug_allInactive = false;
	[Export] public bool Debug_allActive = false;

	private int currentX;
	private int currentY;


	public void GetSignals(int startX, int staryY)
	{
		Godot.Collections.Array<Node> rooms = GetChildren();

		foreach (Node room in rooms)
		{
			Room castedRoom = (Room) room;
			castedRoom.PlayerEntered += SetRoomsVisibilities;
		}

		SetRoomsVisibilities(startX, staryY);
	}

	public void SetRoomsVisibilities(int x, int y)
	{
		GD.Print(x + "," + y);
		currentX = x;
		currentY = y;

		Godot.Collections.Array<Node> rooms = GetChildren();

		foreach (Node room in rooms)
		{
			Room castedRoom = (Room)room;
		
			int roomX = castedRoom.roomX;
			int roomY = castedRoom.roomY;

			if (x == roomX && y == roomY)
			{
				castedRoom.SetActive();
				GD.Print("SET ROOM " + roomX + ", " + roomY + " TO ACTIVE");
			}
			else if (isAdjadcent(x, y, roomX, roomY))
			{
				castedRoom.SetInactive();
				GD.Print("SET ROOM " + roomX + ", " + roomY + " TO INACTIVE");
			}
			else
			{
				castedRoom.SetDisabled();
				GD.Print("SET ROOM " + roomX + ", " + roomY + " TO DISABLED");
			}
		}
	}

	public bool isAdjadcent(int x, int y, int roomX, int roomY)
	{
		if (x == roomX && y == roomY + 1)
		{
			return true;
		}
		if (x == roomX && y == roomY - 1)
		{
			return true;
		}
		if (x == roomX + 1 && y == roomY)
		{
			return true;
		}
		if (x == roomX - 1 && y == roomY)
		{
			return true;
		}

		return false;
	}

	public void Debug_ToogleDebugInactive()
	{
		if (Debug_allInactive)
		{
			Debug_allInactive = false;
			SetRoomsVisibilities(currentX, currentY);
		}
		else
		{
			Debug_allInactive = true;
			Debug_setAllInactive();
		}
	}

	public void Debug_ToogleDebugActive()
	{
		if (Debug_allActive)
		{
			Debug_allActive = false;
			SetRoomsVisibilities(currentX, currentY);
		}
		else
		{
			Debug_allActive = true;
			Debug_setAllActive();
		}
	}

	public void Debug_setAllActive()
	{
		Godot.Collections.Array<Node> rooms = GetChildren();

		foreach (Node room in rooms)
		{
			Room castedRoom = (Room)room;
			castedRoom.SetActive();
		}
	}

	public void Debug_setAllInactive()
	{
		Godot.Collections.Array<Node> rooms = GetChildren();

		foreach (Node room in rooms)
		{
			Room castedRoom = (Room)room;
			castedRoom.SetInactive();
		}
	}
}
