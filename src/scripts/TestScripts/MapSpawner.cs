using Godot;
using System;

public partial class MapSpawner : Node3D
{
    [Export] public Generation genAlgorithm;
    [Export] public PackedScene room;
    [Export] public Node3D roomsManager;

    // --- TILE TYPE CONSTANTS ---
    const int ILLEGAL = -1; //Illegal Tiles cannot be replaced by any other type of tile.
    const int EMPTY = 0; //Empty Tiles. There is nothing on these coordinates of the grid map.
    const int TILE = 1; //Normal Tile. They are Tiles that connect to another Tiles, forming Paths.
    const int STARTTILE = 2; //The Tile where the Dungeon Main Path starts generating.
    const int ENDTILE = 3; //The Tile where the Dungeon Main Path ends generating.
    const int MAINPATH = 4; //This is a regular Tile that forms the Main Path of the Dungeon. 

    private int width;
    private int height;

    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
    {
        width = genAlgorithm.Width;
        height = genAlgorithm.Height;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    /// <summary>
    /// This function is responsable of Spawning Rooms in 3D Space that the Character can traverse.
    /// It goes thru the grid checking if the Tile is a Room, and spawns a Room mesh at the apropiate coordinates.
    /// Then, it checks the surrouding Tiles and places Bridges and Doors acordingly.
    /// </summary>
    public void STEP6_SpawnMap(int[,] grid)
    {
        Vector3 roomStartPoint = new Vector3();
        Vector3 roomStartPointY = new Vector3();
        bool isfirstY = true;

        bool[,] generated = new bool[width, height]; //Keeps track of already generated Tiles.

        //It goes thru the whole grid.
        for (int y = 0; y < height; y++)
        {
            isfirstY = true;

            for (int x = 0; x < width; x++)
            {
                if (CheckTileIsRoom(grid, x, y)) //If the Tile is a Room, it will go thru the whole Room Spawning ordeal.
                {
                    Room roomInstance = (Room)room.Instantiate(); //Instantiates the Room.
                    roomInstance.Name = "Room " + x.ToString() + "," + y.ToString();


                    // If it finds the scrip, it will check each neighboring Tile and check if it is another Room
                    // and if it has been Generated already, and remove Bridges/Walls acordingly.
                    if (roomInstance != null)
                    {
                        roomInstance.roomGridPos = new Vector2I(x, y);

                        // North (-Z in 3D space)
                        bool northIsRoom = CheckTileIsRoom(grid, x, y - 1);
                        roomInstance.makePathNorth = northIsRoom;

                        // South (+Z in 3D space)
                        bool southIsRoom = CheckTileIsRoom(grid, x, y + 1);
                        roomInstance.makePathSouth = southIsRoom;

                        // East (+X in 3D space)
                        bool eastIsRoom = CheckTileIsRoom(grid, x + 1, y);
                        roomInstance.makePathEast = eastIsRoom;

                        // West (-X in 3D space)
                        bool westIsRoom = CheckTileIsRoom(grid, x - 1, y);
                        roomInstance.makePathWest = westIsRoom;

                        if (grid[x, y] == STARTTILE)
                        {
                            roomInstance.SetStartTile();
                        }

                        if (grid[x, y] == ENDTILE)
                        {
                            roomInstance.SetEndTile();
                        }

                        if (grid[x, y] == MAINPATH)
                        {
                            roomInstance.SetMainTile();
                        }

                        roomInstance.setRoom(); //Runs the Object removal function.
                    }
                    else
                    {
                        GD.PrintErr($"ERROR: ROOM SCRIPT NOT FOUND FOR X={x}, Y={y}");
                    }

                    roomInstance.SetRandom(genAlgorithm.random); //Passes the random variable to spawn things.
                    roomInstance.SpawnElements(); //Spawn things in the tile.

                    if (isfirstY)
                    {
                        roomStartPointY += roomInstance.southBridgeTip.Position;
                        roomStartPoint = roomStartPointY;

                        roomInstance.Position = roomStartPoint;
                    }

                    roomInstance.Position = roomStartPoint; //Sets the Room position in Space
                    roomStartPoint += roomInstance.eastBridgeTip.Position;

                    roomsManager.AddChild(roomInstance); //Adds the Room to the Scene.  

                    if (isfirstY)
                    {
                        roomStartPointY += roomInstance.southBridgeTip.Position;
                        roomStartPoint = roomStartPointY;

                        roomInstance.GlobalPosition = roomStartPoint;
                    }

                    roomInstance.GlobalPosition = roomStartPoint; //Sets the Room position in Space
                    roomStartPoint += roomInstance.eastBridgeTip.Position;

                    generated[x, y] = true; //The Tile has already been generated.

                    isfirstY = false;
                }
                
            }
        }
    }

    /// <summary>
    /// This function checks if the set of coordinates contains a Room Tile, independent of its actual Type
    /// </summary>
    /// <returns>True if its a Room, False if its Empty or Illegal</returns>
    private bool CheckTileIsRoom(int[,] grid, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return false;
        }

        if (grid[x, y] == MAINPATH || grid[x, y] == TILE || grid[x, y] == STARTTILE || grid[x, y] == ENDTILE)
        {
            return true;
        }
        return false;
    }
}
