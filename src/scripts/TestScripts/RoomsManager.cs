using Godot;

/// <summary>
/// This Script is atached to the Node cotaining all the Rooms of a level. It manages
/// the states of the rooms.
/// </summary>
public partial class RoomsManager : Node3D
{
	[Export] public bool debug_allInactive = false;
	[Export] public bool debug_allActive = false;

	private int playerGridPosX;
	private int playerGridPosY;


	/// <summary>
	/// This method connects all the Rooms signals for
	/// detecting the player to the visibilities method.
	/// </summary>
	public void GetSignals()
	{
		Godot.Collections.Array<Node> rooms = GetChildren();

		foreach (Node room in rooms)
		{
			Room castedRoom = (Room) room;
			castedRoom.PlayerEntered += SetRoomsVisibilities;
		}
	}

	/// <summary>
	/// This method iterates thru all the rooms, and sets their state acording
	/// to the parameter grid position. The X Y position will be active, adjacent
	/// tiles will be inactive, the rest will be invisible.
	/// </summary>
	/// <param name="roomX">The Room X pos in the grid</param>
	/// <param name="roomY">The Room Y pos in the grid</param>
	public void SetRoomsVisibilities(int roomX, int roomY)
	{
		playerGridPosX = roomX;
		playerGridPosY = roomY;

		Godot.Collections.Array<Node> rooms = GetChildren();

		foreach (Node room in rooms)
		{
			Room castedRoom = (Room) room;
		
			int currentRoomX = castedRoom.roomGridPos.X;
			int currentRoomY = castedRoom.roomGridPos.Y;

			if (roomX == currentRoomX && roomY == currentRoomY)
			{
				castedRoom.SetActive();
				GD.Print("SET ROOM " + currentRoomX + ", " + currentRoomY + " TO ACTIVE");
			}
			else if (isAdjadcent(roomX, roomY, currentRoomX, currentRoomY))
			{
				castedRoom.SetInactive();
				GD.Print("SET ROOM " + currentRoomX + ", " + currentRoomY + " TO INACTIVE");
			}
			else
			{
				castedRoom.SetDisabled();
				GD.Print("SET ROOM " + currentRoomX + ", " + currentRoomY + " TO DISABLED");
			}
		}
	}

	//Checks if a room is adjacent of another
	public bool isAdjadcent(int roomX, int roomY, int currentRoomX, int currentRoomY)
	{
		if (roomX == currentRoomX && roomY == currentRoomY + 1)
		{
			return true;
		}
		if (roomX == currentRoomX && roomY == currentRoomY - 1)
		{
			return true;
		}
		if (roomX == currentRoomX + 1 && roomY == currentRoomY)
		{
			return true;
		}
		if (roomX == currentRoomX - 1 && roomY == currentRoomY)
		{
			return true;
		}

		return false;
	}

	public void Debug_ToogleDebugInactive()
	{
		if (debug_allInactive)
		{
			debug_allInactive = false;
			SetRoomsVisibilities(playerGridPosX, playerGridPosY);
		}
		else
		{
			debug_allInactive = true;
			Debug_setAllInactive();
		}
	}

	public void Debug_ToogleDebugActive()
	{
		if (debug_allActive)
		{
			debug_allActive = false;
			SetRoomsVisibilities(playerGridPosX, playerGridPosY);
		}
		else
		{
			debug_allActive = true;
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
