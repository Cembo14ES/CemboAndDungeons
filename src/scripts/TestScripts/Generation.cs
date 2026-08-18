using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class Generation : Node
{
    // --- GRID SIZE ---
    [ExportGroup("Grid Size")]
    [Export] public int Width; //The Width of the dungeon grid.
    [Export] public int Height; //The Height of the dungeon grid.

    // --- SEED SETTINGS ---
    [ExportGroup("Seed")]
    [Export] public bool UseCustomSeed; //If true, it will use the Seed value. If False, it will use a Randomly generated Seed.
    [Export] public int Seed; //The random Seed used to randomize the Doungeon. Doungeon.

    // --- MAIN PATH SETTINGS ---
    [ExportGroup("Main Path")]
    [Export(PropertyHint.Range, "0,100")] public int mainPathDirectness; //This % determines how direct the Main Path is from start to finish. At 100%, the Main Path will be as short as posible, while at 0%, it will be either Random or Longest.
    [Export(PropertyHint.Enum, "Longest,Random")] public string mainDirectionFailType = "Random"; //If not 100% direct, the Main Path can be random or the longest posible.
    [Export] public int MinMainPathLength = -1; //The Minimun amount of tiles the Main Path should have.
    [Export] public int MaxMainPathLength = -1; //the Maximun amount of tiles the Main Path should have.
    private int mainPathGenerationAttemps = 5000; //The number of atempts the algorithm will try to generate a Main Path between the Min and Max lenght

    // --- POPULATION SETTINGS ---
    [ExportGroup("Population")]
    [Export(PropertyHint.Range, "0,100")] public int populatingDirectness; //Same as the Main Path one.
    [Export(PropertyHint.Enum, "Longest,Random")] public string populatingDirectionFailType = "Random"; //Same as the Main Path one.


    // --- BRANCH SETTINGS ---
    [ExportGroup("Branches")]
    [Export(PropertyHint.Range, "0,100")] public int BranchChance; //The chance of a branch generating off each carved Tile.
    [Export] public int MinBranchLength; //The Minimun lenght of a branch
    [Export] public int MaxBranchLength;  //The maximun lenght of a branch   
    [Export(PropertyHint.Range, "0,100")] public int closedBranchEndChance; //This % represents the chance of a branch ending closed or connecting to another Tile.

    // --- START AND END SETTINGS ---
    [ExportGroup("Start & End")]
    [Export] public Vector2I startTile = new Vector2I(-1, -1); //The coordinates of the Start Tile.
    [Export] public Godot.Collections.Array<Vector2I> endTiles = new Godot.Collections.Array<Vector2I>(); //The coordinates of the End Tiles.

    [Export] public bool closedStart = false; //If True, the Start Tile will only have one path to follow (the Main one).
    [Export] public bool closedEnd = false; //Same as above but with the End Tile.

    // --- PRESETS ---
    [ExportGroup("Preset")]
    [Export(PropertyHint.Enum, "No,Heart,H-shape")] public string preset = "No";

    // --- TILES ---
    [ExportGroup("Room Tiles")]
    [Export] public PackedScene room; //This Scene contains the Room wich will be used to generate the Dungeon

    // --- PRIVATE VARIABLES ---
    public int[,] grid; //The Main Grid: It contains the info of the type of Tiles. It starts filled with EMPTY Tiles.
    public Random random; //The Random Variable.
    private bool firtsGeneration = true;
    private bool setStart;
    private bool[] setEnds;
    private bool areMinMaxMainValuesValid = true;
    [Export] public MapSpawner mapSpawner;


    // --- TILE TYPE CONSTANTS ---
    const int ILLEGAL = -1; //Illegal Tiles cannot be replaced by any other type of tile.
    const int EMPTY = 0; //Empty Tiles. There is nothing on these coordinates of the grid map.
    const int TILE = 1; //Normal Tile. They are Tiles that connect to another Tiles, forming Paths.
    const int STARTTILE = 2; //The Tile where the Dungeon Main Path starts generating.
    const int ENDTILE = 3; //The Tile where the Dungeon Main Path ends generating.
    const int MAINPATH = 4; //This is a regular Tile that forms the Main Path of the Dungeon. 

    /// <summary>
    /// The _Ready function goes thru the steps of generating the Dungeon:
    /// 
    /// STEP 1: Validate Start and Ends
    /// STEP 2: Generate Main Path
    /// STEP 3: Populate the Grid
    /// STEP 4: Carve branches
    /// STEP 5: Post Procesing
    /// 
    /// Finaly, it prints the dungeon on the Output
    /// </summary>
    public void GenerateDungeon()
    {
        GD.PrintRich("\n[color=green]Step 1: INITIAL VALIDATIONS AND CHECKS\n");
        STEP1_InitialValidationsAndChecks();


        GD.PrintRich("\n[color=green]Step 2: GENERATE MAIN PATH\n");
        STEP2_GenerateMainPath();


        GD.PrintRich("\n[color=green]Step 3: POPULATE EMPTY ROWS AND COLUMNS\n");
        STEP3_PopulateRowColumns();


        GD.PrintRich("\n[color=green]Step 4: CREATE BRANCHES\n");
        STEP4_CreateBranches();


        GD.PrintRich("\n[color=green]Step 5: POST PROCESING\n");
        STEP5_PostProccesing();

        //TODO: SOME THINGS

        GD.PrintRich("\n[color=green]DUNGEON GENERATION FINALIZED\n");
        PrintDungeon();

        GD.PrintRich("\n[color=green]GENERATING DUNGEON IN 3D SPACE\n");
        mapSpawner.STEP6_SpawnMap(grid);

        GD.PrintRich("\n[color=green]FINISHED\n");
    }

    // =====================================================
    //                 MAIN STEPS FUNCTIONS
    // =====================================================

    /// <summary>
    /// Before Generation, some variables and User Settings need to check if they are valid.
    /// 
    /// It validates the grid dimension and it initialaizes it.
    /// It also initializes the Random variable, with the given Seed or a random one.
    /// 
    /// Also, this function validates the Start and End Coordinates.
    /// It checks if the coordinates are at a valid position (both odd).
    /// It also checks if 2 sets of coordinates are the same.
    /// 
    /// If checks fails, it replaces the set coordinates with new, random ones.
    /// 
    /// TODO: make check that ensures no end tiles are the same
    /// </summary>
    private void STEP1_InitialValidationsAndChecks()
    {
        //The algorithm only works with uneven grid sizes.
        if (Width % 2 == 0) Width--;
        if (Height % 2 == 0) Height--;

        //Grid initizlization
        grid = new int[Width, Height];

        //Seed initizlization
        int currentSeed = UseCustomSeed ? Seed : _ = new Random().Next();

        //RNG initizlization
        random = new Random(currentSeed);
        GD.PrintRich($"\n[color=yellow]Generating dungeon using Seed: {currentSeed}\n");

        // Min/Max Main Path Lenght values check.
        if (MinMainPathLength < 0)
            MinMainPathLength = 0;

        if (MaxMainPathLength < 0)
            MaxMainPathLength = int.MaxValue;

        if (MinMainPathLength > MaxMainPathLength){
            areMinMaxMainValuesValid = false;
            GD.PushWarning("Min/Max Main Path Lenght values are contradictory, and will be ignored");
        }

        //Mark the ILLEGAL Tiles into the Grid
        MarkIllegalTiles();

        //Start and End Tiles Validation

        // 1. Validate the Start Tile first
        int startX = startTile.X > 0 ? ValidateOdd(startTile.X, Width) : GetRandomOdd(Width);
        int startY = startTile.Y > 0 ? ValidateOdd(startTile.Y, Height) : GetRandomOdd(Height);

        while (grid[startX, startY] == ILLEGAL) //Check if tile is Out of bounds.
        {
            startX = GetRandomOdd(Width);
            startY = GetRandomOdd(Height);
        }

        startTile = new Vector2I(startX, startY); //Sets the valid Start
        GD.Print($"Start coordinates have been set at X={startX}, Y={startY}");


        // 2. If the endTiles array is empty in the inspector, generate at least one random end tile.
        if (endTiles.Count == 0)
        {
            int endX = GetRandomOdd(Width);
            int endY = GetRandomOdd(Height);

            while (startX == endX && startY == endY) //If the End Tile is the same as the start, generate a new one.
            {
                endX = GetRandomOdd(Width);
                endY = GetRandomOdd(Height);
            }
            while (grid[endX, endY] == ILLEGAL) //If the End Tile is Out of Bounds, generate a new one.
            {
                endX = GetRandomOdd(Width);
                endY = GetRandomOdd(Height);
            }

            endTiles.Add(new Vector2I(endX, endY)); //Adds the valid End to the Array
            GD.Print($"Random End coordinates have been generated at X={endX}, Y={endY}");

            return; //There is no need to run the 3rd step if the endTiles array is empty.
        }

        // 3. Loop through and validate every single end tile inside the array.
        for (int i = 0; i < endTiles.Count; i++)
        {
            Vector2I currentEnd = endTiles[i];

            // Validate the coordinates like before
            int endX = currentEnd.X > 0 ? ValidateOdd(currentEnd.X, Width) : GetRandomOdd(Width);
            int endY = currentEnd.Y > 0 ? ValidateOdd(currentEnd.Y, Height) : GetRandomOdd(Height);

            while (startX == endX && startY == endY) //If the End Tile is the same as the start, generate a new one.
            {
                endX = GetRandomOdd(Width);
                endY = GetRandomOdd(Height);
            }
            while (grid[endX, endY] == ILLEGAL) //If the End Tile is Out of Bounds, generate a new one.
            {
                endX = GetRandomOdd(Width);
                endY = GetRandomOdd(Height);
            }

            endTiles[i] = new Vector2I(endX, endY); //Adds the valid End to the Array
            GD.Print($"End coordinates number {i} have been set at X={endX}, Y={endY}");
        }
    }

    /// <summary>
    /// Generates the Main Path of the dungeon.
    /// 
    /// It connects the Start Tile to the first End Tile, and if there are more End Tiles, it
    /// connects those with their closests Path Tile
    /// </summary>
    private void STEP2_GenerateMainPath()
    {
        Vector2I firstEnd = endTiles[0]; //The first End Tile gets assigned to its own variable for simplicity
        bool validPathFound = false;
        int pathAttempts = 0;


        //The Algorithm will try to find a path that fits the stablished requierements.
        //If after some atempts it does not find one, it ignores the requierements and moves on.
        while (pathAttempts < mainPathGenerationAttemps)
        {
            // 1. Wipes previous failed attempts from the grid
            ClearMainPathsFromGrid();

            // 2. Generate and carve the primary Main Path
            List<Vector2I> mainPath = FindPath(startTile.X, startTile.Y, firstEnd.X, firstEnd.Y, mainPathDirectness, mainDirectionFailType);
            CarvePathIntoGrid(mainPath, MAINPATH);

            // 3. Hook up remaining End tiles to the closest Main Path
            for (int i = 1; i < endTiles.Count; i++)
            {
                Vector2I currentEnd = endTiles[i];
                Vector2I closestTile = GetClosestExistingPath(currentEnd);

                List<Vector2I> branchPath = FindPath(currentEnd.X, currentEnd.Y, closestTile.X, closestTile.Y, mainPathDirectness, mainDirectionFailType);
                CarvePathIntoGrid(branchPath, MAINPATH);
            }

            // 4. Calculate exact total length directly from the grid 
            int totalPathLength = CountMainTilesOnGrid();

            // 5. Validate the Main Path length
            if (areMinMaxMainValuesValid)
            {
                if (totalPathLength >= MinMainPathLength && totalPathLength <= MaxMainPathLength)
                {
                    GD.Print($"Total combined length of all main paths: {totalPathLength}");
                    GD.Print($"Number of Main Path attempts: {pathAttempts}");
                    validPathFound = true;
                    break; // Perfect path length found.
                }
            }
            else
            {
                validPathFound = true;
                break;
            }


            pathAttempts++;
        }

        if (!validPathFound)
        {
            GD.PrintErr($"GENERATION: Failed to find a valid combined path length within {mainPathGenerationAttemps} attempts! Adjust the Min/Max range.");
        }
    }

    /// <summary>
    /// After generating the Main Path, some row and collumns may be left fully empty.
    /// This method finds those and creates paths to them.
    /// It selects a random empty Tile from the row/collumn and conects it to the nearest Tile.
    /// </summary>
    private void STEP3_PopulateRowColumns()
    {
        // Scan and populate empty rows
        for (int y = 1; y < Height - 1; y++)
        {
            if (IsLineEmpty(y, true))
            {
                int randomTileX = ValidateOdd(random.Next(1, Width - 1), Width); //Chooses a random Tile

                while (grid[randomTileX, y] == ILLEGAL) //If the Tile is Illegal, finds a new one
                {
                    randomTileX = ValidateOdd(random.Next(1, Width - 1), Width);
                }

                GD.Print("x=" + randomTileX);

                //The random Tile and the clostest Tile.
                Vector2I randomEmptyTile = new Vector2I(randomTileX, y);
                Vector2I closestTile = GetClosestExistingPath(randomEmptyTile);

                GD.PrintRich($"[color=orange]Empty row detected, starting branch from [X:{randomEmptyTile.X}, Y:{randomEmptyTile.Y}] to [X:{closestTile.X}, Y:{closestTile.Y}]");

                //Finds the connecting Path and Carves it.
                List<Vector2I> populationRow = FindPath(randomEmptyTile.X, randomEmptyTile.Y, closestTile.X, closestTile.Y, populatingDirectness, populatingDirectionFailType);
                CarvePathIntoGrid(populationRow, TILE);

            }
        }

        // Scan and populate empty collumns
        for (int x = 1; x < Width - 1; x++)
        {
            if (IsLineEmpty(x, false))
            {
                int randomTileY = ValidateOdd(random.Next(1, Height - 1), Height); //Chooses a random Tile

                while (grid[x, randomTileY] == ILLEGAL) //If the Tile is Illegal, finds a new one
                {
                    randomTileY = ValidateOdd(random.Next(1, Height - 1), Height);
                }

                //The random Tile and the clostest Tile.
                Vector2I randomEmptyTile = new Vector2I(x, randomTileY);
                Vector2I closestTile = GetClosestExistingPath(randomEmptyTile);

                GD.PrintRich($"[color=orange]Empty collumn detected, starting branch from [X:{randomEmptyTile.X}, Y:{randomEmptyTile.Y}] to [X:{closestTile.X}, Y:{closestTile.Y}]");

                //Finds the connecting Path and Carves it.
                List<Vector2I> populationCollumn = FindPath(randomEmptyTile.X, randomEmptyTile.Y, closestTile.X, closestTile.Y, populatingDirectness, populatingDirectionFailType);
                CarvePathIntoGrid(populationCollumn, TILE);
            }
        }
    }

    /// <summary>
    /// Afte Population, now there will be attempts to carve aditional branches in each existing Tile.
    /// </summary>
    private void STEP4_CreateBranches()
    {
        List<Vector2I> allExistingTiles = GetAllCarvedTiles(); //Collects all existing Tiles in the Grid.

        foreach (Vector2I step in allExistingTiles)
        {
            if (random.Next(100) <= BranchChance) //For the branch to atemp generation, it must succed the stablished chance.
            {
                CarveBranch(step.X, step.Y);
            }
        }
    }

    /// <summary>
    /// This last Function runs some aditional code and checks based mainly on user preferences.
    /// </summary>
    private void STEP5_PostProccesing()
    {
        grid[startTile.X, startTile.Y] = STARTTILE; //Marks the Start Tile on the Grid

        // Loops through all End tiles to mark them on the Grid
        foreach (Vector2I endPos in endTiles)
        {
            grid[endPos.X, endPos.Y] = ENDTILE;
        }

        if (closedStart) //It gets all the Neighboring Tiles and deletes the ones that aren't Mains
        {
            List<Vector2I> emptyNeighbors = new List<Vector2I>();
            List<Vector2I> tileNeighbors = new List<Vector2I>();
            List<Vector2I> mainNeighbors = new List<Vector2I>();
            CheckNeighborType(startTile.X, startTile.Y - 1, emptyNeighbors, tileNeighbors, mainNeighbors); //SOUTH
            CheckNeighborType(startTile.X, startTile.Y + 1, emptyNeighbors, tileNeighbors, mainNeighbors); //NORTH
            CheckNeighborType(startTile.X - 1, startTile.Y, emptyNeighbors, tileNeighbors, mainNeighbors); //WEST
            CheckNeighborType(startTile.X + 1, startTile.Y, emptyNeighbors, tileNeighbors, mainNeighbors); //EAST

            if (tileNeighbors.Count > 0)
            {
                foreach (Vector2I path in tileNeighbors)
                {
                    grid[path.X, path.Y] = EMPTY;
                }
            }
        }

        if (closedEnd) //It gets all the Neighboring Tiles and deletes the ones that aren't Mains
        {
            // Apply the logic to each End Tile
            foreach (Vector2I currentEnd in endTiles)
            {
                List<Vector2I> emptyNeighbors = new List<Vector2I>();
                List<Vector2I> tileNeighbors = new List<Vector2I>();
                List<Vector2I> mainNeighbors = new List<Vector2I>();
                CheckNeighborType(currentEnd.X, currentEnd.Y - 1, emptyNeighbors, tileNeighbors, mainNeighbors); //SOUTH
                CheckNeighborType(currentEnd.X, currentEnd.Y + 1, emptyNeighbors, tileNeighbors, mainNeighbors); //NORTH
                CheckNeighborType(currentEnd.X - 1, currentEnd.Y, emptyNeighbors, tileNeighbors, mainNeighbors); //WEST
                CheckNeighborType(currentEnd.X + 1, currentEnd.Y, emptyNeighbors, tileNeighbors, mainNeighbors); //EAST

                if (tileNeighbors.Count > 0)
                {
                    foreach (Vector2I path in tileNeighbors)
                    {
                        grid[path.X, path.Y] = EMPTY;
                    }
                }
            }
        }
    }

    // =====================================================
    //                    MAIN FUNCTIONS
    // =====================================================

    /// <summary>
    /// This function goes thru the frid, and marks the outer rows and collumns as Illegal
    /// 
    /// TODO: Add functionalirty to also mark custom tiles as illegal
    /// </summary>
    private void MarkIllegalTiles()
    {
        if (preset == "H-shape")
        {
            Width = 15;
            Height = 15;
            grid = new int[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                grid[7, y] = ILLEGAL;         // Left Column (x = 0)
                grid[Width - 1, y] = ILLEGAL; // Right Column (x = Max)
            }
            for (int y = 0; y < Height; y++)
            {
                grid[8, y] = ILLEGAL;         // Left Column (x = 0)
                grid[Width - 1, y] = ILLEGAL; // Right Column (x = Max)
            }
            for (int y = 0; y < Height; y++)
            {
                grid[6, y] = ILLEGAL;         // Left Column (x = 0)
                grid[Width - 1, y] = ILLEGAL; // Right Column (x = Max)
            }

            for (int x = 0; x < Width; x++)
            {
                grid[x, 6] = EMPTY;          // Top Row (y = 0)
                grid[x, Height - 1] = EMPTY; // Bottom Row (y = Max)
            }
            for (int x = 0; x < Width; x++)
            {
                grid[x, 7] = EMPTY;          // Top Row (y = 0)
                grid[x, Height - 1] = EMPTY; // Bottom Row (y = Max)
            }
            for (int x = 0; x < Width; x++)
            {
                grid[x, 8] = EMPTY;          // Top Row (y = 0)
                grid[x, Height - 1] = EMPTY; // Bottom Row (y = Max)
            }



        }
        // 1. Loop through every collumn to mark the Top and Bottom rows
        for (int x = 0; x < Width; x++)
        {
            grid[x, 0] = ILLEGAL;          // Top Row (y = 0)
            grid[x, Height - 1] = ILLEGAL; // Bottom Row (y = Max)
        }

        // 2. Loop through every row to mark the Left and Right columns
        for (int y = 0; y < Height; y++)
        {
            grid[0, y] = ILLEGAL;         // Left Column (x = 0)
            grid[Width - 1, y] = ILLEGAL; // Right Column (x = Max)
        }
    }

    /// <summary>
    /// This method recives 2 coordinates, and generates a path between the 2
    /// Depending on the direcctness, it will truy to make the shortes path
    /// posible, or the Longest/Random.
    /// </summary>
    /// 
    /// <returns>The set of coordinates that make the Main Path</returns>
    private List<Vector2I> FindPath(int startX, int startY, int endX, int endY, int direcctnessMultiplier, string directionType)
    {
        //Inizializing
        List<Vector2I> breadcrumbs = new List<Vector2I>(); //A List containing Vector2I (X and Y coordinates). This list will contain the coordinates of Tiles than indicate the path from the Start to Finish
        bool[,] visited = new bool[Width, Height]; //Will keep track of the Tiles that the algorith has already gone thru.


        breadcrumbs.Add(new Vector2I(startX, startY));// Adds the Start tile as the first Main Path Tile
        visited[startX, startY] = true;// Marks the Start tile as visited.

        while (breadcrumbs.Count > 0)
        {
            Vector2I current = breadcrumbs[breadcrumbs.Count - 1]; // Moves to the current tile

            if (current.X == endX && current.Y == endY) // Checks if the current tile is the End tile
            {
                return breadcrumbs;
            }

            //Now, its going to find "safe neighbors". Those are tiles that are valid (not Illegal or not already visited).
            //Also, note that it checks 2 Tiles ahead every direction.

            List<Vector2I> safeNeighbors = new List<Vector2I>(); //It will contain the valid neighbors.

            //Coordinates of each cardinal Tile
            int tileNORTH = current.Y + 2;
            int tileSOUTH = current.Y - 2;
            int tileWEST = current.X - 2;
            int tileEAST = current.X + 2;

            //Check if SOUTH tile is valid
            if (tileSOUTH > 0 && !visited[current.X, tileSOUTH] && grid[current.X, tileSOUTH] != ILLEGAL)
                safeNeighbors.Add(new Vector2I(current.X, current.Y - 2));

            //Check if NORTH tile is valid
            if (tileNORTH < Height - 1 && !visited[current.X, tileNORTH] && grid[current.X, tileNORTH] != ILLEGAL)
                safeNeighbors.Add(new Vector2I(current.X, tileNORTH));

            //Check if WEST tile is valid
            if (tileWEST > 0 && !visited[tileWEST, current.Y] && grid[tileWEST, current.Y] != ILLEGAL)
                safeNeighbors.Add(new Vector2I(tileWEST, current.Y));

            //Check if EAST tile is valid
            if (tileEAST < Width - 1 && !visited[tileEAST, current.Y] && grid[tileEAST, current.Y] != ILLEGAL)
                safeNeighbors.Add(new Vector2I(tileEAST, current.Y));


            // Now, is going to choose a safe neighbor to continue the Main Path.
            // First, depending on the direcctnessMultiplier, it will choose the optimal neighbor or a random one.
            // If it chooses the optimal path, it will choose the closest neighbour to the End Tile.
            // Else, it will choose either at random or the furthest to the End Tile.

            // If there is no safe neighbor, it will go back on the breadcrum path.

            if (safeNeighbors.Count > 0)
            {
                Vector2I nextTile = safeNeighbors[0]; //The Tile that will be added to breadcrums

                bool isShortestPath = random.Next(100) <= direcctnessMultiplier; //Decides if the path is the shortest or not. 
                int distanceToCheck = isShortestPath ? int.MaxValue : 0; //Asigns the distance to compare to depending on the direction type.

                if (!isShortestPath && directionType == "Random") //First, it check if the next Tile should be Random, since its the easiest one to calculate.
                {
                    nextTile = safeNeighbors[random.Next(safeNeighbors.Count)]; //Chooses a random neighbor.
                }
                else
                {
                    foreach (Vector2I neighbor in safeNeighbors) //Iterates thru all neighbours to check de distance between them and the End Tile.
                    {
                        int dist = Math.Abs(neighbor.X - endX) + Math.Abs(neighbor.Y - endY); //Gets the distance between the neighbor and the End Tile.

                        if (isShortestPath)
                        {
                            if (dist < distanceToCheck) //Checks if the distance is lower than the previous neighbor.
                            {
                                distanceToCheck = dist; //When true, saves the current distance as the closest to the End Tile.
                                nextTile = neighbor; //Sets the current neighbor as the closest Tile.
                            }
                        }
                        else
                        {
                            //When reaching this point in the code, its allways "Longest".
                            if (dist > distanceToCheck) //Checks if the distance is higher than the previous neighbor.
                            {
                                distanceToCheck = dist; //When true, saves the current distance as the furthest to the End Tile.
                                nextTile = neighbor; //sets the current neighbor as the furthest Tile.
                            }
                        }
                    }
                }

                visited[nextTile.X, nextTile.Y] = true; //Marks this tile as visited.
                breadcrumbs.Add(nextTile); //Adds this tile to the breadcrums.
            }
            else
            {
                breadcrumbs.RemoveAt(breadcrumbs.Count - 1); //If no neighbors are valid, it goes back a step.
            }
        }

        return breadcrumbs;
    }

    /// <summary>
    /// This method carves a List with a path info and carves it in the grid, assigning the desigbnated tile type.
    /// </summary>
    /// <param name="path">The List containing the path to carve</param>
    /// <param name="tileType">The tipe of tiles beeing carved</param>
    private void CarvePathIntoGrid(List<Vector2I> path, int tileType)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2I current = path[i];
            Vector2I next = path[i + 1];
            if (tileType == MAINPATH)
            {
                if (grid[next.X, next.Y] != MAINPATH)
                {
                    grid[current.X, current.Y] = MAINPATH;
                    grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = MAINPATH;
                    grid[next.X, next.Y] = MAINPATH;
                }
                else
                {
                    grid[current.X, current.Y] = MAINPATH;
                    grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = MAINPATH;
                    break;
                }

            }
            else if (tileType == TILE)
            {
                if (grid[next.X, next.Y] != MAINPATH)
                {
                    if (grid[current.X, current.Y] == MAINPATH)
                    {
                        grid[current.X, current.Y] = MAINPATH;
                        grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = TILE;
                        grid[next.X, next.Y] = TILE;
                    }
                    else
                    {
                        grid[current.X, current.Y] = TILE;
                        grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = TILE;
                        grid[next.X, next.Y] = TILE;
                    }

                }
                else if (grid[current.X, current.Y] == MAINPATH)
                {
                    if (grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] == MAINPATH)
                    {
                        grid[current.X, current.Y] = MAINPATH;
                        grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = MAINPATH;
                        grid[next.X, next.Y] = MAINPATH;
                    }
                    else
                    {
                        grid[current.X, current.Y] = MAINPATH;
                        grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = TILE;
                        grid[next.X, next.Y] = MAINPATH;
                    }

                }
                else
                {
                    grid[current.X, current.Y] = TILE;
                    grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = TILE;
                    grid[next.X, next.Y] = MAINPATH;
                }

            }

        }
    }

    /// <summary>
    /// This Function generates a valid branch in the grid and carves it.
    /// </summary>
    /// <param name="startX">The X coordinate of the starting tile</param>
    /// <param name="startY">The Y coordinate of the starting tile</param>
    private void CarveBranch(int startX, int startY)
    {
        List<Vector2I> branchPath = [new Vector2I(startX, startY)]; //Inizializes the branch.

        int stepsTaken = 0;
        int targetLength = random.Next(MinBranchLength, MaxBranchLength + 1); //Chooses the lenght of the branch

        while (branchPath.Count > 0 && stepsTaken < targetLength)
        {
            Vector2I current = branchPath[branchPath.Count - 1]; //Initializes the current Tile of the brach.

            //Based on the current tile, the following lines check the neighbours of the Tile and classifies them.
            List<Vector2I> emptyNeighbors = new List<Vector2I>();
            List<Vector2I> tileNeighbors = new List<Vector2I>();
            List<Vector2I> mainNeighbors = new List<Vector2I>();

            CheckNeighborType(current.X, current.Y - 2, emptyNeighbors, tileNeighbors, mainNeighbors); //SOUTH
            CheckNeighborType(current.X, current.Y + 2, emptyNeighbors, tileNeighbors, mainNeighbors); //NORTH
            CheckNeighborType(current.X - 2, current.Y, emptyNeighbors, tileNeighbors, mainNeighbors); //WEST
            CheckNeighborType(current.X + 2, current.Y, emptyNeighbors, tileNeighbors, mainNeighbors); //EAST

            if (emptyNeighbors.Count > 0) //If it finds a wall, it carves the tile and continues.
            {
                Vector2I nextWall = emptyNeighbors[random.Next(emptyNeighbors.Count)]; //Chooses a Random Tile.

                //Assigns the Path as Tiles.
                grid[(current.X + nextWall.X) / 2, (current.Y + nextWall.Y) / 2] = TILE;
                grid[nextWall.X, nextWall.Y] = TILE;

                branchPath.Add(nextWall);
                stepsTaken++;
            }
            else if (tileNeighbors.Count > 0) //If it finds a Tile, it will either stop forming the branch, or carve one last path and stop.
            {
                if (random.Next(100) > closedBranchEndChance) //Check if the branch will connect to the Tile or end.
                {
                    Vector2I nextPath = tileNeighbors[random.Next(tileNeighbors.Count)];
                    grid[(current.X + nextPath.X) / 2, (current.Y + nextPath.Y) / 2] = TILE;
                }
                break;

            }
            else //If there was no Tile or Empty neighbor, the current Tile is not valid and goes a step back.
            {
                if (stepsTaken > 0) //It ensures that the Branch has made at least a step before going back
                {
                    branchPath.RemoveAt(branchPath.Count - 1);
                }
                else
                {
                    break;
                }
            }
        }
    }

    // =====================================================
    //                   HELPER FUNCTIONS
    // =====================================================

    /// <summary>
    /// The method checks for Rows/Collumns on the grid that are fully EMTPY (Excluding Illegal).
    /// </summary>
    /// <param name="index">The Row or Collumn to check</param>
    /// <param name="isRow">If the method has to check for Rows or Collumns</param>
    /// <returns>If the Row/Collumn is empty or not</returns>
    private bool IsLineEmpty(int index, bool isRow)
    {
        if (index % 2 != 0)
        {
            int length = isRow ? Width : Height;
            int fullyIllegal = 0;
            for (int i = 1; i < length - 1; i++)
            {
                int checkX = isRow ? i : index;
                int checkY = isRow ? index : i;

                if (grid[checkX, checkY] == TILE) return false;
                if (grid[checkX, checkY] == MAINPATH) return false;
                if (grid[checkX, checkY] == ILLEGAL)
                {
                    fullyIllegal++;
                }
            }

            if (fullyIllegal >= length - 2) //Illegal Line Check
            {
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// It goes thru all the Tiles in the Grid, and finds all the Carved Tiles (Every non EMPTY or ILLEGAL Tile).
    /// </summary>
    /// <returns>A collection with all the Carved Tiles in the Grid</returns>
    private List<Vector2I> GetAllCarvedTiles()
    {
        List<Vector2I> allCarvedTiles = new List<Vector2I>();

        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                if (grid[x, y] == TILE | grid[x, y] == MAINPATH | grid[x, y] == STARTTILE | grid[x, y] == ENDTILE)
                {
                    if (x % 2 != 0 && y % 2 != 0) //Check if it isn't and even Tile.
                    {
                        allCarvedTiles.Add(new Vector2I(x, y));
                    }
                }
            }
        }
        return allCarvedTiles;
    }


    /// <summary>
    /// It goes thru all the Tiles in the Grid, and finds all the Empty Tiles.
    /// </summary>
    /// <returns>A collection with all the Empty Tiles in the Grid</returns>
    /// 
    /// CURRENTLY UNESED, BUT MIGHT BE USED LATER.
    private List<Vector2I> GetAllEmptyTiles()
    {
        List<Vector2I> allEmptyTiles = new List<Vector2I>();
        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                if (grid[x, y] == EMPTY)
                {
                    if (x % 2 != 0 && y % 2 != 0) //Check if it isn't and even Tile.
                    {
                        allEmptyTiles.Add(new Vector2I(x, y));
                    }
                }
            }
        }
        return allEmptyTiles;
    }

    /// <summary>
    /// The Function finds the closest Carved Tile to the parameter Tile.
    /// </summary>
    /// <param name="tile">The Tile that needs to find its closest Carved Tile</param>
    /// <returns>The closest Carved Tile to the parameter one.</returns>
    private Vector2I GetClosestExistingPath(Vector2I tile)
    {
        List<Vector2I> allExistingPaths = GetAllCarvedTiles(); //Gets all existing Tiles.

        Vector2I closestTile;
        closestTile = allExistingPaths[0];
        int closestDist = int.MaxValue;

        foreach (Vector2I tileCandidate in allExistingPaths) //Iterates thru all the Carved Tiles.
        {
            int dist = Math.Abs(tileCandidate.X - tile.X) + Math.Abs(tileCandidate.Y - tile.Y); //Gets the distance between the Tiles
            if (dist < closestDist) //Checks if the distance is higher than the previous checked Tile.
            {
                closestDist = dist; //When true, saves the current distance as the furthest.
                closestTile = tileCandidate; //Sets the current Tile as the furthest one.
            }
        }

        return closestTile;
    }

    /// <summary>
    /// It checks if the Tile is EMPTY, TILE or MAIN
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="walls"></param>
    /// <param name="paths"></param>
    /// <param name="mains"></param>
    /// 
    /// TODO: TRY TO SIMPLIFY THE FUNCTION.
    private void CheckNeighborType(int x, int y, List<Vector2I> walls, List<Vector2I> paths, List<Vector2I> mains)
    {
        if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1) //Checks if coordinates are inbounds
        {
            if (grid[x, y] == EMPTY)
            {
                walls.Add(new Vector2I(x, y));
            }
            else if (grid[x, y] == TILE)
            {
                paths.Add(new Vector2I(x, y));
            }
            else if (grid[x, y] == MAINPATH)
            {
                mains.Add(new Vector2I(x, y));
            }
        }
    }

    /// <summary>
    /// Validates the value parameter.
    /// It makes the number Odd, turns it into a positive number, and limits it to the designated limit.
    /// </summary>
    /// <param name="maxBound">The biggest number it can be</param>
    /// <returns>The new Validated integer</returns>
    private int ValidateOdd(int value, int maxBound)
    {
        if (value % 2 == 0) value++;
        if (value < 1) value = 1;

        if (value >= maxBound - 1)
        {
            value = maxBound - 2;

            // Safety check
            if (value % 2 == 0)
            {
                value--;
            }
        }
        return value;
    }

    /// <summary>
    /// Gets a Random Odd number withing the designated limit. Its allways positive
    /// </summary>
    /// <param name="maxBound">The biggest number it can be</param>
    /// <returns>The new Random Validated integer</returns>
    private int GetRandomOdd(int maxBound)
    {
        return ValidateOdd(random.Next(1, maxBound - 1), maxBound);
    }

    /// <summary>
    /// Scans the Grid and erases all Main Path Tiles, turning them back into EMPTY.
    /// Used to reset the Grid when a Main Path attempt fails the length check.
    /// </summary>
    private void ClearMainPathsFromGrid()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == MAINPATH)
                {
                    grid[x, y] = EMPTY;
                }
            }
        }
    }

    /// <summary>
    /// Counts and returns exactly how many MAIN Tiles currently exist on the grid.
    /// </summary>
    private int CountMainTilesOnGrid()
    {
        int count = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == MAINPATH)
                {
                    count++;
                }
            }
        }
        return count;
    }

    

    /// <summary>
    /// Checks the provided Tile's type, and returns it.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private int CheckTileType(int x, int y)
    {
        if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1) //Checks if coordinates are inbounds
        {
            if (grid[x, y] == EMPTY)
            {
                return EMPTY;
            }
            else if (grid[x, y] == TILE)
            {
                return TILE;
            }
            else if (grid[x, y] == MAINPATH)
            {
                return MAINPATH;
            }
            else if (grid[x, y] == STARTTILE)
            {
                return STARTTILE;
            }
            else if (grid[x, y] == ENDTILE)
            {
                return ENDTILE;
            }
            else if (grid[x, y] == ILLEGAL)
            {
                return ILLEGAL;
            }
        }
        GD.Print("Room type not found. Fix issue");
        return ILLEGAL; //If there is no mach, reurns ILLEGAL as a failsave
    }

    public void resetMap()
    {
        startTile = new Vector2I(-1, -1);
        endTiles.Clear();
        random = null;
    }

    


    /// <summary>
    /// Prints the Dungeon into the Godot console Output.
    /// </summary>
    private void PrintDungeon()
    {
        GD.PrintRich($"\n[color=aqua]--- Dungeon Blueprint ---\n");

        StringBuilder sb = new StringBuilder();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == EMPTY) sb.Append("███");
                else if (grid[x, y] == TILE) sb.Append(" . ");
                else if (grid[x, y] == STARTTILE) sb.Append(" S ");
                else if (grid[x, y] == ENDTILE) sb.Append(" E ");
                else if (grid[x, y] == MAINPATH) sb.Append(" M ");
                else if (grid[x, y] == ILLEGAL) sb.Append("█=█");
                else sb.Append(" ? ");
            }
            sb.AppendLine();
        }
        GD.Print(sb.ToString());
    }
}