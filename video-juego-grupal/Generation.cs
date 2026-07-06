using Godot;
using GodotPlugins.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public partial class Generation : Node
{

    // --- GRID SIZE ---
    [ExportGroup("Grid Size")]
    [Export] public int Width; //The Width of the dungeon grid
    [Export] public int Height; //The Height of the dungeon grid

    // --- SEED SETTINGS ---
    [ExportGroup("Seed")]
    [Export] public int Seed; //The random Seed used to randomize the doungeon
    [Export] public bool UseCustomSeed; //If true, it will use the Seed value. If False, it will use a random generated seed

    // --- MAIN PATHS SETTINGS ---
    [ExportGroup("Main Path")]
    [Export(PropertyHint.Range, "0,100")] public int mainPathDirectness; //This % determines how direct the main path is from start to finish. At 0%, the Main Path will be as short as posible, while at 100%, it will be as random as posible
    [Export(PropertyHint.Enum, "Longest,Random")] public string mainDirectionType = "Random"; //If not perfectly direct, the main path can be random or the longest posible
    [Export] public int MinMainPathLength; //The Minimun amount of tiles the main path should have
    [Export] public int MaxMainPathLength; //the Maximun amount of tiles the main path shoul have

    [ExportGroup("Population")]
    [Export(PropertyHint.Range, "0,100")] public int populatingDirectness;
    [Export(PropertyHint.Enum, "Longest,Random")] public string populatingDirectionType = "Random";


    // --- SIDE BRANCH SETTINGS ---
    [ExportGroup("Branches")]
    [Export(PropertyHint.Range, "0,100")] public int BranchChance; //The chance of a branch generating off each tile
    [Export] public int MinBranchLength; //The Minimun lenght of a branch
    [Export] public int MaxBranchLength;  //The maximun lenght of a branch               

    // --- START AND END SETTINGS ---
    [ExportGroup("Start & End")]
    [Export] public Vector2I startTile = new Vector2I(-1, -1);
    [Export] public Godot.Collections.Array<Vector2I> endTiles = new Godot.Collections.Array<Vector2I>();

    [Export] public bool closedStart = false;
    [Export] public bool closedEnd = false;


    // --- TILE TYPE CONSTANTS ---

    const int OoB = -1;
    const int WALL = 0;
    const int PATH = 1;
    const int START = 2;
    const int END = 3;
    const int MAIN = 4;

    private int[,] grid; //The Main Grid: It contains the info of the tiles
    private Random random; //The Random Variable


    // Check if the grid dimensions are valid initializes the seed.
    public override void _Ready()
    {
        //The algorithm only works with uneven grid sizes.
        if (Width % 2 == 0) Width--;
        if (Height % 2 == 0) Height--;

        //Grid initizlization
        grid = new int[Width, Height];

        //Seed initizlization
        int currentSeed = UseCustomSeed ? currentSeed = Seed : currentSeed = new Random().Next();

        //Random initizlization
        random = new Random(currentSeed);

        GD.Print($"\n[DungeonGenerator] Generating dungeon using Seed: {currentSeed}");
        GD.Print("_Ready function finished");

        GenerateDungeon();

        PrintDungeon();
    }

    //Generates the whole dungeon.
    private void GenerateDungeon()
    {
        MarkOutOfBoundsBorders();


        GD.Print("Starting Step1: SET START & END COORDINATES");
        NEOSTEP1_ValidateStartEnd();


        GD.Print("Step 2: GENERATE MAIN PATH ");
        STEP2_GenerateMainPath();


        GD.Print("Step 3: POPULATE EMPTY ROWS AND COLUMNS");
        STEP3_PopulateRowColumns();


        GD.Print("Step4: CREATE BRANCHES");
        STEP4_CreateBranches();

        GD.Print("Step5: OVERWRITE RENDER FOR VISUAL LABELS");
        grid[startTile.X, startTile.Y] = START;

        // Loop through all end tiles to mark them visually
        foreach (Vector2I endPos in endTiles)
        {
            grid[endPos.X, endPos.Y] = END;
        }

        STEP5_PostProccesing();
    }

    private void NEOSTEP1_ValidateStartEnd()
    {
        // 1. Validate the Start Tile first
        int startX = startTile.X > 0 ? MakeOdd(startTile.X, Width) : GetRandomOdd(Width);
        int startY = startTile.Y > 0 ? MakeOdd(startTile.Y, Height) : GetRandomOdd(Height);

        while (grid[startX, startY] == OoB)
        {
            startX = GetRandomOdd(Width);
            startY = GetRandomOdd(Height);
        }

        GD.Print("Start Coordinates were not valid, new start at X:" + startX + " Y:" + startY);


        startTile = new Vector2I(startX, startY);

        // 2. Safety Check: If the array is empty in the inspector, generate at least one random end tile
        if (endTiles.Count == 0)
        {
            int endX = GetRandomOdd(Width);
            int endY = GetRandomOdd(Height);

            while (startX == endX && startY == endY)
            {
                endX = GetRandomOdd(Width);
                endY = GetRandomOdd(Height);
            }

            while (grid[endX, endY] == OoB)
            {
                endX = GetRandomOdd(Width);
                endY = GetRandomOdd(Height);
            }

            endTiles.Add(new Vector2I(endX, endY));

            GD.Print("End coordinates generated at X:" + startX + " Y:" + startY);
            return;
        }

        // 3. Loop through and validate every single end tile inside the array
        for (int i = 0; i < endTiles.Count; i++)
        {
            Vector2I currentEnd = endTiles[i];

            // Process current end tile coordinates using the same ternary logic
            int endX = currentEnd.X > 0 ? MakeOdd(currentEnd.X, Width) : GetRandomOdd(Width);
            int endY = currentEnd.Y > 0 ? MakeOdd(currentEnd.Y, Height) : GetRandomOdd(Height);

            // If this specific end tile overlaps with the start tile, reroll its coordinates
            while (startX == endX && startY == endY)
            {
                endX = GetRandomOdd(Width);
                endY = GetRandomOdd(Height);
            }
            while (grid[endX, endY] == OoB)
            {
                endX = GetRandomOdd(Width);
                endY = GetRandomOdd(Height);
            }

            // Save the cleaned-up, valid coordinates back into the array slot
            endTiles[i] = new Vector2I(endX, endY);
        }
    }


    private void STEP2_GenerateMainPath()
    {
        Vector2I firstEnd = endTiles[0];
        bool validPathFound = false;
        int pathAttempts = 0;

        while (pathAttempts < 5000)
        {
            // 1. Wipe previous failed attempts from the grid
            ClearMainPathsFromGrid();

            // 2. PHASE 1: Generate and carve the primary Main Path
            List<Vector2I> mainPath = FindPath(startTile.X, startTile.Y, firstEnd.X, firstEnd.Y, mainPathDirectness, mainDirectionType);
            CarvePathIntoGrid(mainPath, MAIN);

            // 3. PHASE 2: Hook up remaining End tiles to the closest Main Path
            for (int i = 1; i < endTiles.Count; i++)
            {
                Vector2I currentEnd = endTiles[i];
                Vector2I closestTile = GetClosestExistingPath(currentEnd);

                List<Vector2I> branchPath = FindPath(currentEnd.X, currentEnd.Y, closestTile.X, closestTile.Y, mainPathDirectness, mainDirectionType);
                CarvePathIntoGrid(branchPath, MAIN);
            }

            // 4. Calculate exact total length directly from the grid 
            // (This avoids double-counting overlapping junction tiles)
            int totalPathLength = CountMainTilesOnGrid();

            // 5. Validate the COMBINED length
            if (totalPathLength >= MinMainPathLength && totalPathLength <= MaxMainPathLength)
            {
                GD.Print($"Total combined length of all main paths: {totalPathLength}");
                GD.Print($"Number of Main Path attempts: {pathAttempts}");
                validPathFound = true;
                break; // Perfect path length found!
            }

            pathAttempts++;
        }

        if (!validPathFound)
        {
            GD.PrintErr("[DungeonGenerator] CRITICAL: Failed to find a valid combined path length within 5000 attempts! Try expanding your Min/Max range.");
        }
    }

    private void STEP3_PopulateRowColumns()
    {
        // 1. Scan and fix empty inner rows
        for (int y = 1; y < Height - 1; y++)
        {
            if (IsLineEmpty(y, true))
            {
                int randomTile = random.Next(1, Width - 1);

                if (randomTile % 2 == 0)
                {
                    randomTile--;
                }

                while (grid[randomTile, y] == OoB)
                {
                    randomTile = random.Next(1, Width - 1);

                    if (randomTile % 2 == 0)
                    {
                        randomTile--;
                    }
                }


                Vector2I randomEmptyTile = new Vector2I(randomTile, y);
                Vector2I closestTile = GetClosestExistingPath(randomEmptyTile);

                GD.Print("Empty row detected, starting branch from X:" + randomEmptyTile.X + ", Y: " + randomEmptyTile.Y + " to X:" + closestTile.X + ", Y:" + closestTile.Y);

                List<Vector2I> branch = FindPath(randomEmptyTile.X, randomEmptyTile.Y, closestTile.X, closestTile.Y, populatingDirectness, populatingDirectionType);

                CarvePathIntoGrid(branch, PATH);
                PrintDungeon();
            }
        }

        // 2. Scan and fix empty inner columns
        for (int x = 1; x < Width - 1; x++)
        {
            if (IsLineEmpty(x, false))
            {
                int randomTile = random.Next(1, Height - 1);

                if (randomTile % 2 == 0)
                {
                    randomTile--;
                }


                while (grid[x, randomTile] == OoB)
                {
                    randomTile = random.Next(1, Height - 1);

                    if (randomTile % 2 == 0)
                    {
                        randomTile--;
                    }
                }



                Vector2I randomEmptyTile = new Vector2I(x, randomTile);
                Vector2I closestTile = GetClosestExistingPath(randomEmptyTile);

                GD.Print("Empty collumn detected, starting branch from X:" + randomEmptyTile.X + ", Y: " + randomEmptyTile.Y + " to X:" + closestTile.X + ", Y:" + closestTile.Y);

                List<Vector2I> branch = FindPath(randomEmptyTile.X, randomEmptyTile.Y, closestTile.X, closestTile.Y, populatingDirectness, populatingDirectionType);

                CarvePathIntoGrid(branch, PATH);
                PrintDungeon();
            }
        }
    }

    private void STEP4_CreateBranches()
    {
        List<Vector2I> allExistingPaths = GetAllPaths();

        foreach (Vector2I step in allExistingPaths) //For each room in the main path, it will try to create a branch, depending on the stablished chance
        {
            if (random.Next(100) <= BranchChance)
            {
                CarveBranch(step.X, step.Y);
            }
        }
    }

    private void STEP5_PostProccesing()
    {


        if (closedStart)
        {
            List<Vector2I> wallsNeighbors = new List<Vector2I>();
            List<Vector2I> pathNeighbors = new List<Vector2I>();
            CheckNeighborType(startTile.X, startTile.Y - 1, wallsNeighbors, pathNeighbors); //SOUTH
            CheckNeighborType(startTile.X, startTile.Y + 1, wallsNeighbors, pathNeighbors); //NORTH
            CheckNeighborType(startTile.X - 1, startTile.Y, wallsNeighbors, pathNeighbors); //WEST
            CheckNeighborType(startTile.X + 1, startTile.Y, wallsNeighbors, pathNeighbors); //EAST

            if (pathNeighbors.Count > 0)
            {
                foreach (Vector2I path in pathNeighbors)
                {
                    grid[path.X, path.Y] = WALL;
                }
            }
        }

        if (closedEnd)
        {
            // Apply single-entry enforcement to every end tile
            foreach (Vector2I currentEnd in endTiles)
            {
                List<Vector2I> wallsNeighbors = new List<Vector2I>();
                List<Vector2I> pathNeighbors = new List<Vector2I>();
                CheckNeighborType(currentEnd.X, currentEnd.Y - 1, wallsNeighbors, pathNeighbors); //SOUTH
                CheckNeighborType(currentEnd.X, currentEnd.Y + 1, wallsNeighbors, pathNeighbors); //NORTH
                CheckNeighborType(currentEnd.X - 1, currentEnd.Y, wallsNeighbors, pathNeighbors); //WEST
                CheckNeighborType(currentEnd.X + 1, currentEnd.Y, wallsNeighbors, pathNeighbors); //EAST

                if (pathNeighbors.Count > 0)
                {
                    foreach (Vector2I path in pathNeighbors)
                    {
                        grid[path.X, path.Y] = WALL;
                    }
                }
            }
        }
    }

    private void MarkOutOfBoundsBorders()
    {
        // 1. Loop through every column to mark the Top and Bottom rows
        for (int x = 0; x < Width; x++)
        {
            grid[x, 0] = OoB;          // Top Row (y = 0)
            grid[x, Height - 1] = OoB; // Bottom Row (y = Max)
        }

        // 2. Loop through every row to mark the Left and Right columns
        for (int y = 0; y < Height; y++)
        {
            grid[0, y] = OoB;         // Left Column (x = 0)
            grid[Width - 1, y] = OoB; // Right Column (x = Max)
        }
    }

    /// <summary>
    /// This method recives 2 coordinates, and generates a path between the 2
    /// NOTE: It marks a tile every two tiles (Read documentation if unclear).
    /// </summary>
    /// 
    /// <returns>The set of coordinates that make the Path</returns>
    private List<Vector2I> FindPath(int startX, int startY, int endX, int endY, int direcctnessMultiplier, string directionType)
    {
        //Inizializing
        List<Vector2I> breadcrumbs = new List<Vector2I>(); //A List containing Vector2I (X and Y coordinates). This list will contain the coordinates than indicate the path from the Start to Finish
        bool[,] visited = new bool[Width, Height]; //Will keep track of tiles that already have the main path


        breadcrumbs.Add(new Vector2I(startX, startY));// Adds the Start tile as the first element
        visited[startX, startY] = true;// Marks the Start tile as visited. This is used for not going back to tiles previously been by backtracking

        while (breadcrumbs.Count > 0)
        {
            Vector2I current = breadcrumbs[breadcrumbs.Count - 1]; // Moves to the current tile

            if (current.X == endX && current.Y == endY) // Checks if the current tile is the End tile
            {
                return breadcrumbs;
            }

            //Now, its going to find "safe neighbors". Those are tiles that are valid (not OoB, not already visited...).
            //Also, note that it checks 2 Tiles ahead every direction.

            List<Vector2I> safeNeighbors = new List<Vector2I>(); //It will contain the valid neighbors.

            //Coordinates of each cardinal tile
            int tileNORTH = current.Y + 2;
            int tileSOUTH = current.Y - 2;
            int tileWEST = current.X - 2;
            int tileEAST = current.X + 2;

            //Check if SOUTH tile is valid
            if (tileSOUTH > 0 && !visited[current.X, tileSOUTH] && grid[current.X, tileSOUTH] != OoB)
                safeNeighbors.Add(new Vector2I(current.X, current.Y - 2));

            //Check if NORTH tile is valid
            if (tileNORTH < Height - 1 && !visited[current.X, tileNORTH] && grid[current.X, tileNORTH] != OoB)
                safeNeighbors.Add(new Vector2I(current.X, tileNORTH));

            //Check if WEST tile is valid
            if (tileWEST > 0 && !visited[tileWEST, current.Y] && grid[tileWEST, current.Y] != OoB)
                safeNeighbors.Add(new Vector2I(tileWEST, current.Y));

            //Check if EAST tile is valid
            if (tileEAST < Width - 1 && !visited[tileEAST, current.Y] && grid[tileEAST, current.Y] != OoB)
                safeNeighbors.Add(new Vector2I(tileEAST, current.Y));


            // Now, is going to choose a safe neighbor to continue the path.
            // First, depending on the MainPathWinding, it will choose the optimal neighbor or a random one
            // If it chooses the optimal path, it will check witch neighbor is the closest, and go there
            // Else, it will choose at random
            // If there is no safe neighbor, it will go back on the breadcrum path
            if (safeNeighbors.Count > 0)
            {
                Vector2I next; //The Tile that will be added to breadcrums

                if (random.Next(100) <= direcctnessMultiplier) // chooses between the shortest path (True), and a random path (False)
                {

                    next = safeNeighbors[0];
                    int closestDist = int.MaxValue; //The shortest distance between current neighbor and the end (infinite on the first iteration)

                    foreach (Vector2I neighbor in safeNeighbors)
                    {
                        int dist = Math.Abs(neighbor.X - endX) + Math.Abs(neighbor.Y - endY); //Gets the distance between the neighbor and the end
                        if (dist < closestDist)
                        { //Checks if the distance is lower than the previous neighbor
                            closestDist = dist; //When true, saves the current distance as the closest to the end
                            next = neighbor; //sets the current neighbor as the closest tile
                        }
                    }
                }
                else
                {
                    if (directionType == "Longest")
                    {
                        next = safeNeighbors[0];
                        int furthestDist = 0; //The longest distance between current neighbor and the end (infinite on the first iteration)

                        foreach (Vector2I neighbor in safeNeighbors)
                        {
                            int dist = Math.Abs(neighbor.X - endX) + Math.Abs(neighbor.Y - endY); //Gets the distance between the neighbor and the end
                            if (dist > furthestDist)
                            { //Checks if the distance is higher than the previous neighbor
                                furthestDist = dist; //When true, saves the current distance as the furthest to the end
                                next = neighbor; //sets the current neighbor as the furthest tile
                            }
                        }
                    }
                    else
                    {
                        next = safeNeighbors[random.Next(safeNeighbors.Count)]; //Chooses a random neighbor
                    }

                }

                visited[next.X, next.Y] = true; //Marks this tile as visited
                breadcrumbs.Add(next); //Adds this tile to the breadcrums
            }
            else
            {
                breadcrumbs.RemoveAt(breadcrumbs.Count - 1);
            }
        }

        return breadcrumbs;
    }

    /// <summary>
    /// This Function generates a valid branch in the grid and carves it
    /// </summary>
    /// <param name="startX">The X coordinate of the starting tile</param>
    /// <param name="startY">The Y coordinate of the starting tile</param>
    private void CarveBranch(int startX, int startY)
    {
        List<Vector2I> branchPath = [new Vector2I(startX, startY)]; //Inizializes the branch

        int stepsTaken = 0;
        int targetLength = random.Next(MinBranchLength, MaxBranchLength + 1); //Determines the lenght of the branch

        while (branchPath.Count > 0 && stepsTaken < targetLength)
        {
            Vector2I current = branchPath[branchPath.Count - 1]; //Initializes the current tile of the brach

            //Based on the current tile, the following lines check the neighbours of the tile and classify them in wall or path tiles
            List<Vector2I> wallNeighbors = new List<Vector2I>();
            List<Vector2I> pathNeighbors = new List<Vector2I>();

            CheckNeighborType(current.X, current.Y - 2, wallNeighbors, pathNeighbors); //SOUTH
            CheckNeighborType(current.X, current.Y + 2, wallNeighbors, pathNeighbors); //NORTH
            CheckNeighborType(current.X - 2, current.Y, wallNeighbors, pathNeighbors); //WEST
            CheckNeighborType(current.X + 2, current.Y, wallNeighbors, pathNeighbors); //EAST

            if (pathNeighbors.Count > 0) //If it finds a path, it stops forming the branch
            {

                Vector2I nextPath = pathNeighbors[random.Next(pathNeighbors.Count)];
                grid[(current.X + nextPath.X) / 2, (current.Y + nextPath.Y) / 2] = PATH;
                break;


            }
            else if (wallNeighbors.Count > 0) //If it finds a wall, it carves the tile and continues
            {
                Vector2I nextWall = wallNeighbors[random.Next(wallNeighbors.Count)];
                grid[(current.X + nextWall.X) / 2, (current.Y + nextWall.Y) / 2] = PATH;
                grid[nextWall.X, nextWall.Y] = PATH;

                branchPath.Add(nextWall);
                stepsTaken++;
            }
            else //If there was no path or wall neighbor, the tile is not valid and goes a step back
            {
                if (stepsTaken > 0)
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

    /// <summary>
    /// The method checks for lines/collumns on the grid that fully consist of walls (Excluding OoB)
    /// </summary>
    /// <param name="index">The Line or Row to check</param>
    /// <param name="isRow">If the method has to check for collumns or rows</param>
    /// <returns>If the row/collumn is empty or not</returns>
    private bool IsLineEmpty(int index, bool isRow)
    {
        if (index % 2 != 0)
        {
            int length = isRow ? Width : Height;
            int fullyOoB = 0;
            for (int i = 1; i < length - 1; i++)
            {
                int checkX = isRow ? i : index;
                int checkY = isRow ? index : i;

                if (grid[checkX, checkY] == PATH) return false;
                if (grid[checkX, checkY] == MAIN) return false;
                if (grid[checkX, checkY] == OoB)
                {
                    fullyOoB++;
                }
            }
            GD.Print(fullyOoB);

            if (fullyOoB >= length - 2)
            {
                GD.Print("OoB row");
                return false;
            }
            return true;


        }

        return false;
    }

    private List<Vector2I> GetAllPaths()
    {
        List<Vector2I> allPaths = new List<Vector2I>();
        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                if (grid[x, y] == PATH | grid[x, y] == MAIN)
                {
                    if (x % 2 != 0 && y % 2 != 0)
                    {
                        allPaths.Add(new Vector2I(x, y));
                    }
                }
            }
        }

        return allPaths;
    }

    private List<Vector2I> GetAllWalls()
    {
        List<Vector2I> allWalls = new List<Vector2I>();
        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                if (grid[x, y] == WALL)
                {
                    if (x % 2 != 0 && y % 2 != 0)
                    {
                        allWalls.Add(new Vector2I(x, y));
                    }
                }
            }
        }

        return allWalls;
    }

    private Vector2I GetClosestExistingPath(Vector2I tile)
    {
        List<Vector2I> allExistingPaths = GetAllPaths();


        Vector2I closestTile;
        closestTile = allExistingPaths[0];
        int closestDist = int.MaxValue;


        foreach (Vector2I tileCandidate in allExistingPaths)
        {
            int dist = Math.Abs(tileCandidate.X - tile.X) + Math.Abs(tileCandidate.Y - tile.Y); //Gets the distance between the neighbor and the end
            if (dist < closestDist)
            { //Checks if the distance is higher than the previous neighbor
                closestDist = dist; //When true, saves the current distance as the furthest to the end
                closestTile = tileCandidate; //sets the current neighbor as the furthest tile
            }
        }


        return closestTile;
    }

    // --- HELPER FUNCTIONS ---

    //The method gets a coordinate, and checks if it consists of a wall of path
    private void CheckNeighborType(int x, int y, List<Vector2I> walls, List<Vector2I> paths)
    {
        if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1) //Checks if coordinates are inbounds
        {
            if (grid[x, y] == WALL)
            {
                walls.Add(new Vector2I(x, y));
            }
            else if (grid[x, y] == PATH)
            {
                paths.Add(new Vector2I(x, y));
            }
        }
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
            if (tileType == MAIN)
            {
                if (grid[next.X, next.Y] != MAIN)
                {
                    grid[current.X, current.Y] = MAIN;
                    grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = MAIN;
                    grid[next.X, next.Y] = MAIN;
                }
                else
                {
                    grid[current.X, current.Y] = MAIN;
                    grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = MAIN;
                    break;
                }

            }
            else if (tileType == PATH)
            {
                if (grid[next.X, next.Y] != MAIN)
                {
                    if (grid[current.X, current.Y] == MAIN)
                    {
                        grid[current.X, current.Y] = MAIN;
                        grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = PATH;
                        grid[next.X, next.Y] = PATH;
                    }
                    else
                    {
                        grid[current.X, current.Y] = PATH;
                        grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = PATH;
                        grid[next.X, next.Y] = PATH;
                    }

                }
                else if (grid[current.X, current.Y] == MAIN)
                {
                    if (grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] == MAIN)
                    {
                        grid[current.X, current.Y] = MAIN;
                        grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = MAIN;
                        grid[next.X, next.Y] = MAIN;
                    }
                    else
                    {
                        grid[current.X, current.Y] = MAIN;
                        grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = PATH;
                        grid[next.X, next.Y] = MAIN;
                    }

                }
                else
                {
                    grid[current.X, current.Y] = PATH;
                    grid[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = PATH;
                    grid[next.X, next.Y] = MAIN;
                }

            }

        }
    }


    private int MakeOdd(int value, int maxBound)
    {
        if (value % 2 == 0) value++;
        if (value < 1) value = 1;
        if (value >= maxBound - 1) value = maxBound - 2;
        return value;
    }

    private int GetRandomOdd(int maxBound)
    {
        return MakeOdd(random.Next(1, maxBound - 1), maxBound);
    }

    /// <summary>
    /// Scans the grid and erases all MAIN path tiles, turning them back into WALLs.
    /// Used to reset the board when a path attempt fails its length check.
    /// </summary>
    private void ClearMainPathsFromGrid()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == MAIN)
                {
                    grid[x, y] = WALL;
                }
            }
        }
    }

    /// <summary>
    /// Counts exactly how many MAIN path tiles currently exist on the grid.
    /// </summary>
    private int CountMainTilesOnGrid()
    {
        int count = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == MAIN)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private void PrintDungeon()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"--- Dungeon Blueprint ---");
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == WALL) sb.Append("███");
                else if (grid[x, y] == PATH) sb.Append(" . ");
                else if (grid[x, y] == START) sb.Append(" S ");
                else if (grid[x, y] == END) sb.Append(" E ");
                else if (grid[x, y] == MAIN) sb.Append(" M ");
                else if (grid[x, y] == OoB) sb.Append("█=█");
                else sb.Append(" ? ");
            }
            sb.AppendLine();
        }
        GD.Print(sb.ToString());
    }
}