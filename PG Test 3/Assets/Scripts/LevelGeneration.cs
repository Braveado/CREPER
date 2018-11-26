using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGeneration : MonoBehaviour
{
    Transform mapParent, enemiesParent;
    enum gridSpace
    {
        Empty, Floor,        
        Ext_H, Ext_V,
        End_U, End_D, End_R, End_L,
        Pillar, Roof,
        Corner_UL, Corner_UL_SE, Corner_UR, Corner_UR_SW, Corner_DL, Corner_DL_NE, Corner_DR, Corner_DR_NW,
        Border_U, Border_U_SW, Border_U_SE, Border_U_SW_SE,
        Border_D, Border_D_NW, Border_D_NE, Border_D_NW_NE,
        Border_R, Border_R_NW, Border_R_SW, Border_R_NW_SW,
        Border_L, Border_L_NE, Border_L_SE, Border_L_NE_SE,
        Inner_NW, Inner_NE, Inner_SW, Inner_SE,
        Inner_NW_NE, Inner_NE_SE, Inner_SW_SE, Inner_SW_NW, Inner_NW_SE, Inner_NE_SW,
        Inner_NW_NE_SE, Inner_NE_SE_SW, Inner_SE_SW_NW, Inner_SW_NW_NE,
        Inner_ALL
    };
    gridSpace[,] grid;
    int levelHeight, levelWidth;
    float worldUnitsInOneGridCell = 1;
    Vector2 gridCenter;    

    [Header("Map Settings")]
    public int levelRowsAndColumns;
    Vector2 levelSizeWorldUnits;
    public float levelPercentToFill;
    public int levelExtraBorder;
    public bool removePillars;
    public Camera minimapObj;

    [Header("Map Set 1")]
    public GameObject[] floors;
    public float altFloor1Chance;
    public float altFloor2Chance;
    public GameObject roof;
    public GameObject pillar;
    public GameObject border_U;
    public GameObject border_U_SW;
    public GameObject border_U_SE;
    public GameObject border_U_SW_SE;
    public GameObject border_D;
    public GameObject border_D_NW;
    public GameObject border_D_NE;
    public GameObject border_D_NW_NE;
    public GameObject border_R;
    public GameObject border_R_NW;
    public GameObject border_R_SW;
    public GameObject border_R_NW_SW;
    public GameObject border_L;
    public GameObject border_L_NE;
    public GameObject border_L_SE;
    public GameObject border_L_NE_SE;
    public GameObject extension_V;
    public GameObject extension_H;
    public GameObject corner_UR;
    public GameObject corner_UR_SW;
    public GameObject corner_UL;
    public GameObject corner_UL_SE;
    public GameObject corner_DR;
    public GameObject corner_DR_NW;
    public GameObject corner_DL;
    public GameObject corner_DL_NE;
    public GameObject end_U;
    public GameObject end_D;
    public GameObject end_R;
    public GameObject end_L;
    public GameObject inner_NW;
    public GameObject inner_NE;
    public GameObject inner_SW;
    public GameObject inner_SE;
    public GameObject inner_NW_NE;
    public GameObject inner_NE_SE;
    public GameObject inner_SW_SE;
    public GameObject inner_SW_NW;
    public GameObject inner_NW_SE;
    public GameObject inner_NE_SW;
    public GameObject inner_NW_NE_SE;
    public GameObject inner_NE_SE_SW;
    public GameObject inner_SE_SW_NW;
    public GameObject inner_SW_NW_NE;
    public GameObject inner_ALL;

    struct walker
    {
        public Vector2 dir;
        public Vector2 pos;
    }
    List<walker> walkers;
    [Header("Walker Settings")]
    public float chanceWalkerChangeDirection;
    public float chanceMoveUp;
    public float chanceMoveDown;
    public float chanceMoveLeft;
    public float chanceMoveRight;
    public float chanceWalkerSpawn;
    public float chanceWalkerDestroy;
    public int maxWalkers;

    [Header("Goal Settings")]
    public GameObject goalObj;
    Vector2 goalPos;

    [Header("Player Settings")]    
    public int initSafeSpace;

    [Header("Enemy Settings")]
    public int enemyAmount;
    public GameObject[] enemyObj;
    

    void Start()
    {
        // sets the map variables and the first walker
        Setup();
        // use walkers to create floors
        CreateFloors();
        // put walls in every place that isnt a floor
        CreateWalls();
        // set the appropiate version of wall in each wall that needs a change
        ChangeAppropiateWalls();
        // spawn the map
        SpawnLevel();
        // spawn a goal
        SpawnGoal();
        // spawn enemies
        SpawnEnemies();
    }

    void Setup()
    {
        // set rows and columns
        levelSizeWorldUnits = new Vector2(levelRowsAndColumns, levelRowsAndColumns);
        // find grid size
        levelHeight = Mathf.RoundToInt(levelSizeWorldUnits.y / worldUnitsInOneGridCell);
        levelWidth = Mathf.RoundToInt(levelSizeWorldUnits.x / worldUnitsInOneGridCell);
        // create grid
        grid = new gridSpace[levelWidth, levelHeight];
        // set grids default state
        for (int x = 0; x < levelWidth - 1; x++)
        {
            for (int y = 0; y < levelHeight - 1; y++)
            {
                // make every cell empty
                grid[x, y] = gridSpace.Empty;
            }
        }
        // find center of grid
        gridCenter = new Vector2(Mathf.RoundToInt(levelWidth / 2.0f), Mathf.RoundToInt(levelHeight / 2.0f));

        // init list of walkers
        walkers = new List<walker>();
        // set first walker
        walker newWalker = new walker();
        newWalker.dir = RandomDirection();
        newWalker.pos = gridCenter;
        // add walker to list
        walkers.Add(newWalker);

        // create parents for the level
        mapParent = new GameObject().transform;
        mapParent.name = "Map";
        enemiesParent = new GameObject().transform;
        enemiesParent.name = "Enemies";

        // set minimap camera size
        //minimapObj.orthographicSize = ((float)levelRowsAndColumns / 2f) - levelExtraBorder;
    }

    Vector2 RandomDirection()
    {
        // repeat until a return
        while (true)
        {
            // get a direction to try its chance
            int direction = Random.Range(0, 4);
            switch (direction)
            {
                // return a direction if its chance succeds
                case 0:
                    if (Random.value <= chanceMoveUp)
                        return Vector2.up;
                    break;
                case 1:
                    if (Random.value <= chanceMoveDown)
                        return Vector2.down;
                    break;
                case 2:
                    if (Random.value <= chanceMoveLeft)
                        return Vector2.left;
                    break;
                default:
                    if (Random.value <= chanceMoveRight)
                        return Vector2.right;
                    break;
            }
        }
    }

    void CreateFloors()
    {
        // to make sure the loop wont run forever
        int iterations = 0;
        do
        {
            // create floors at walkers positions
            foreach (walker myWalker in walkers)
                grid[(int)myWalker.pos.x, (int)myWalker.pos.y] = gridSpace.Floor;

            // chance destroy walker
            int numberChecks = walkers.Count;
            // might modify count while in this loop
            for (int i = 0; i < numberChecks; i++)
            {
                // only if its not the only one
                if (Random.value < chanceWalkerDestroy && walkers.Count > 1)
                {
                    walkers.RemoveAt(i);
                    // only destroy one per iteration
                    break;
                }
            }

            // chance walker pick new direction
            for (int i = 0; i < walkers.Count; i++)
            {
                if (Random.value < chanceWalkerChangeDirection)
                {
                    walker thisWalker = walkers[i];
                    thisWalker.dir = RandomDirection();
                    walkers[i] = thisWalker;
                }
            }

            // chance spawn walker
            numberChecks = walkers.Count;
            // might modify count while in this loop
            for (int i = 0; i < numberChecks; i++)
            {
                // only if number of walkers is less than max
                if (Random.value < chanceWalkerSpawn && walkers.Count < maxWalkers)
                {
                    // create a walker
                    walker newWalker = new walker();
                    newWalker.dir = RandomDirection();
                    newWalker.pos = walkers[i].pos;
                    walkers.Add(newWalker);
                    // only spawn one per iteration
                    break;
                }
            }

            // move walkers
            for (int i = 0; i < walkers.Count; i++)
            {
                walker thisWalker = walkers[i];
                thisWalker.pos += thisWalker.dir;
                walkers[i] = thisWalker;
            }
            // avoid border of grid
            for (int i = 0; i < walkers.Count; i++)
            {                
                walker thisWalker = walkers[i];
                // clamp x, y to leave a minimum space border for room walls
                thisWalker.pos.x = Mathf.Clamp(thisWalker.pos.x, 1 + levelExtraBorder, levelWidth - 2 - levelExtraBorder);
                thisWalker.pos.y = Mathf.Clamp(thisWalker.pos.y, 1 + levelExtraBorder, levelHeight - 2 - levelExtraBorder);

                walkers[i] = thisWalker;
            }

            // check to exit loop
            if ((float)NumberOfFloors() / (float)grid.Length > levelPercentToFill)
                break;
            iterations++;            
        }
        while (iterations < 99999);
    }

    int NumberOfFloors()
    {
        int count = 0;
        foreach (gridSpace space in grid)
        {
            if (space == gridSpace.Floor)
                count++;
        }
        return count;
    }

    void CreateWalls()
    {
        // loop through every grid space including edges
        for (int x = 0; x < levelWidth; x++)
        {
            for (int y = 0; y < levelHeight; y++)
            {
                // if its empty place a wall
                if (grid[x, y] == gridSpace.Empty)
                    grid[x, y] = gridSpace.Roof;
            }
        }
    }

    void ChangeAppropiateWalls()
    {
        //set cardenal auxiliar variables
        int walls = 0;
        bool up = false, down = false, left = false, right = false;
        //set ordinal auxiliar variables
        int floors = 0;
        bool nw = false, ne = false, sw = false, se = false;
        // loop through every grid space minus the extra border to change the walls for its appropiate version
        for (int x = 1; x < levelWidth - levelExtraBorder; x++)
        {
            for (int y = 1; y < levelWidth - levelExtraBorder; y++)
            {
                // if thers a wall check the spaces around it
                if (grid[x, y] == gridSpace.Roof)
                {
                    // reset the cardinal auxiliar variables
                    walls = 0;
                    up = false;
                    down = false;
                    left = false;
                    right = false;
                    // get the amount and direction of adjacent walls for cardinal checks
                    if (grid[x, y + 1] != gridSpace.Floor)
                    {
                        walls++;
                        up = true;
                    }
                    if (grid[x, y - 1] != gridSpace.Floor)
                    {
                        walls++;
                        down = true;
                    }
                    if (grid[x + 1, y] != gridSpace.Floor)
                    {
                        walls++;
                        right = true;
                    }
                    if (grid[x - 1, y] != gridSpace.Floor)
                    {
                        walls++;
                        left = true;
                    }
                    // get the correct wall using cardinal checks
                    switch (walls)
                    {
                        case 0:
                            // 0 adyacent walls, its a pillar
                            if(!removePillars)
                                grid[x, y] = gridSpace.Pillar;
                            else if(removePillars)
                                grid[x, y] = gridSpace.Floor;
                            break;
                        case 1:
                            // 1 adyacent wall, its an end, get direction
                            if (up && !down && !right && !left)
                                grid[x, y] = gridSpace.End_D;
                            else if (!up && down && !right && !left)
                                grid[x, y] = gridSpace.End_U;
                            else if (!up && !down && right && !left)
                                grid[x, y] = gridSpace.End_L;
                            else if (!up && !down && !right && left)
                                grid[x, y] = gridSpace.End_R;
                            break;
                        case 2:
                            // 2 adyacent walls, its a corner or an ext, get direction
                            if (up && down && !right && !left)
                                grid[x, y] = gridSpace.Ext_V;
                            else if (!up && !down && right && left)
                                grid[x, y] = gridSpace.Ext_H;
                            else if (up && !down && right && !left)
                                grid[x, y] = gridSpace.Corner_DL;
                            else if (up && !down && !right && left)
                                grid[x, y] = gridSpace.Corner_DR;
                            else if (!up && down && right && !left)
                                grid[x, y] = gridSpace.Corner_UL;
                            else if (!up && down && !right && left)
                                grid[x, y] = gridSpace.Corner_UR;                            
                            break;
                        case 3:
                            // 3 adyacent walls, its a border, get direction
                            if (!up)
                                grid[x, y] = gridSpace.Border_U;
                            else if (!down)
                                grid[x, y] = gridSpace.Border_D;
                            else if (!right)
                                grid[x, y] = gridSpace.Border_R;
                            else if (!left)
                                grid[x, y] = gridSpace.Border_L;
                            break;
                        default:
                            // 4 adjacent walls, its a roof, continue on the ordinal checks
                            break;
                    }
                    // chek if the current wall needs a check in ordinals
                    if(grid[x, y] >= gridSpace.Roof)
                    {
                        // reset the ordinal auxiliar variables
                        floors = 0;
                        nw = false;
                        ne = false;
                        sw = false;
                        se = false;
                        // get the amount and direction of adjacent floors for ordinal checks
                        if (grid[x - 1, y + 1] == gridSpace.Floor)
                        {
                            floors++;
                            nw = true;
                        }
                        if (grid[x + 1, y + 1] == gridSpace.Floor)
                        {
                            floors++;
                            ne = true;
                        }
                        if (grid[x - 1, y - 1] == gridSpace.Floor)
                        {
                            floors++;
                            sw = true;
                        }
                        if (grid[x + 1, y - 1] == gridSpace.Floor)
                        {
                            floors++;
                            se = true;
                        }
                        // get the appropiate version of wall using ordinal checks
                        switch (grid[x, y])
                        {
                            case gridSpace.Roof:
                                switch (floors)
                                {
                                    case 1:
                                        // 1 adyacent floor, get direction
                                        if (nw && !ne && !sw && !se)
                                            grid[x, y] = gridSpace.Inner_NW;
                                        else if (!nw && ne && !sw && !se)
                                            grid[x, y] = gridSpace.Inner_NE;
                                        else if (!nw && !ne && sw && !se)
                                            grid[x, y] = gridSpace.Inner_SW;
                                        else if (!nw && !ne && !sw && se)
                                            grid[x, y] = gridSpace.Inner_SE;
                                        break;
                                    case 2:
                                        // 2 adyacent floor, get direction
                                        if (nw && ne && !sw && !se)
                                            grid[x, y] = gridSpace.Inner_NW_NE;
                                        else if (!nw && ne && !sw && se)
                                            grid[x, y] = gridSpace.Inner_NE_SE;
                                        else if (!nw && !ne && sw && se)
                                            grid[x, y] = gridSpace.Inner_SW_SE;
                                        else if (nw && !ne && sw && !se)
                                            grid[x, y] = gridSpace.Inner_SW_NW;
                                        else if (nw && !ne && !sw && se)
                                            grid[x, y] = gridSpace.Inner_NW_SE;
                                        else if (!nw && ne && sw && !se)
                                            grid[x, y] = gridSpace.Inner_NE_SW;
                                        break;
                                    case 3:
                                        // 3 adyacent walls, get direction
                                        if (!nw)
                                            grid[x, y] = gridSpace.Inner_NE_SE_SW;
                                        else if (!ne)
                                            grid[x, y] = gridSpace.Inner_SE_SW_NW;
                                        else if (!sw)
                                            grid[x, y] = gridSpace.Inner_NW_NE_SE;
                                        else if (!se)
                                            grid[x, y] = gridSpace.Inner_SW_NW_NE;
                                        break;
                                    case 4:
                                        // 4 walls
                                        grid[x, y] = gridSpace.Inner_ALL;
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case gridSpace.Corner_DL:                                
                                if(ne)
                                    // the inverse ordinal is a floor
                                    grid[x, y] = gridSpace.Corner_DL_NE;
                                break;
                            case gridSpace.Corner_DR:
                                if (nw)
                                    // the inverse ordinal is a floor
                                    grid[x, y] = gridSpace.Corner_DR_NW;
                                break;
                            case gridSpace.Corner_UL:
                                if (se)
                                    // the inverse ordinal is a floor
                                    grid[x, y] = gridSpace.Corner_UL_SE;
                                break;
                            case gridSpace.Corner_UR:
                                if (sw)
                                    // the inverse ordinal is a floor
                                    grid[x, y] = gridSpace.Corner_UR_SW;
                                break;
                            case gridSpace.Border_D:
                                // get directions
                                if (nw && !ne)
                                    grid[x, y] = gridSpace.Border_D_NW;
                                else if (!nw && ne)
                                    grid[x, y] = gridSpace.Border_D_NE;
                                else if (nw && ne)
                                    grid[x, y] = gridSpace.Border_D_NW_NE;
                                break;
                            case gridSpace.Border_L:
                                // get directions
                                if (ne && !se)
                                    grid[x, y] = gridSpace.Border_L_NE;
                                else if (!ne && se)
                                    grid[x, y] = gridSpace.Border_L_SE;
                                else if (ne && se)
                                    grid[x, y] = gridSpace.Border_L_NE_SE;
                                break;
                            case gridSpace.Border_R:
                                // get directions
                                if (nw && !sw)
                                    grid[x, y] = gridSpace.Border_R_NW;
                                else if (!nw && sw)
                                    grid[x, y] = gridSpace.Border_R_SW;
                                else if (nw && sw)
                                    grid[x, y] = gridSpace.Border_R_NW_SW;
                                break;
                            case gridSpace.Border_U:
                                // get directions
                                if (sw && !se)
                                    grid[x, y] = gridSpace.Border_U_SW;
                                else if (!sw && se)
                                    grid[x, y] = gridSpace.Border_U_SE;
                                else if (sw && se)
                                    grid[x, y] = gridSpace.Border_U_SW_SE;
                                break;
                            default:
                                // not a wall that needs a check
                                break;
                        }
                    }                                    
                }
            }
        }
    }

    void Spawn(float x, float y, GameObject toSpawn)
    {
        // find the position to spawn
        Vector2 offset = levelSizeWorldUnits / 2.0f;
        Vector2 spawnPos = new Vector2(x, y) * worldUnitsInOneGridCell - offset;
        // spawn object
        Instantiate(toSpawn, spawnPos, Quaternion.identity);
    }

    void Spawn(float x, float y, GameObject toSpawn, Transform parent)
    {
        // find the position to spawn
        Vector2 offset = levelSizeWorldUnits / 2.0f;
        Vector2 spawnPos = new Vector2(x, y) * worldUnitsInOneGridCell - offset;
        // spawn object
        GameObject spawn = Instantiate(toSpawn, spawnPos, Quaternion.identity);
        // parent the object
        spawn.transform.parent = parent;
    }

    void SpawnLevel()
    {
        // loop through every grid space
        for (int x = 0; x < levelWidth; x++)
        {
            for (int y = 0; y < levelHeight; y++)
            {
                // spawn the map tile by tile as the loop goes
                switch (grid[x, y])
                {
                    case gridSpace.Empty:
                        break;
                    case gridSpace.Floor:
                        {
                            //Spawn every floor with equal chances
                            //Spawn(x, y, floors[Random.Range(0, floors.Length)], mapParent);
                            // check the chances of the alt floors to spawn them, if they fail spawn the main floor
                            if (Random.value <= altFloor1Chance)
                                Spawn(x, y, floors[1], mapParent);
                            else if (Random.value <= altFloor2Chance)
                                Spawn(x, y, floors[2], mapParent);
                            else
                                Spawn(x, y, floors[0], mapParent);
                        }                        
                        break;
                    case gridSpace.Roof:
                        Spawn(x, y, roof, mapParent);
                        break;
                    case gridSpace.Border_U:
                        Spawn(x, y, border_U, mapParent);
                        break;
                    case gridSpace.Border_U_SW:
                        Spawn(x, y, border_U_SW, mapParent);
                        break;
                    case gridSpace.Border_U_SE:
                        Spawn(x, y, border_U_SE, mapParent);
                        break;
                    case gridSpace.Border_U_SW_SE:
                        Spawn(x, y, border_U_SW_SE, mapParent);
                        break;
                    case gridSpace.Border_D:
                        Spawn(x, y, border_D, mapParent);
                        break;
                    case gridSpace.Border_D_NW:
                        Spawn(x, y, border_D_NW, mapParent);
                        break;
                    case gridSpace.Border_D_NE:
                        Spawn(x, y, border_D_NE, mapParent);
                        break;
                    case gridSpace.Border_D_NW_NE:
                        Spawn(x, y, border_D_NW_NE, mapParent);
                        break;
                    case gridSpace.Border_R:
                        Spawn(x, y, border_R, mapParent);
                        break;
                    case gridSpace.Border_R_NW:
                        Spawn(x, y, border_R_NW, mapParent);
                        break;
                    case gridSpace.Border_R_SW:
                        Spawn(x, y, border_R_SW, mapParent);
                        break;
                    case gridSpace.Border_R_NW_SW:
                        Spawn(x, y, border_R_NW_SW, mapParent);
                        break;
                    case gridSpace.Border_L:
                        Spawn(x, y, border_L, mapParent);
                        break;
                    case gridSpace.Border_L_NE:
                        Spawn(x, y, border_L_NE, mapParent);
                        break;
                    case gridSpace.Border_L_SE:
                        Spawn(x, y, border_L_SE, mapParent);
                        break;
                    case gridSpace.Border_L_NE_SE:
                        Spawn(x, y, border_L_NE_SE, mapParent);
                        break;
                    case gridSpace.Ext_V:
                        Spawn(x, y, extension_V, mapParent);
                        break;
                    case gridSpace.Ext_H:
                        Spawn(x, y, extension_H, mapParent);
                        break;
                    case gridSpace.Corner_UR:
                        Spawn(x, y, corner_UR, mapParent);
                        break;
                    case gridSpace.Corner_UR_SW:
                        Spawn(x, y, corner_UR_SW, mapParent);
                        break;
                    case gridSpace.Corner_UL:
                        Spawn(x, y, corner_UL, mapParent);
                        break;
                    case gridSpace.Corner_UL_SE:
                        Spawn(x, y, corner_UL_SE, mapParent);
                        break;
                    case gridSpace.Corner_DR:
                        Spawn(x, y, corner_DR, mapParent);
                        break;
                    case gridSpace.Corner_DR_NW:
                        Spawn(x, y, corner_DR_NW, mapParent);
                        break;
                    case gridSpace.Corner_DL:
                        Spawn(x, y, corner_DL, mapParent);
                        break;
                    case gridSpace.Corner_DL_NE:
                        Spawn(x, y, corner_DL_NE, mapParent);
                        break;
                    case gridSpace.End_U:
                        Spawn(x, y, end_U, mapParent);
                        break;
                    case gridSpace.End_D:
                        Spawn(x, y, end_D, mapParent);
                        break;
                    case gridSpace.End_R:
                        Spawn(x, y, end_R, mapParent);
                        break;
                    case gridSpace.End_L:
                        Spawn(x, y, end_L, mapParent);
                        break;
                    case gridSpace.Pillar:
                        Spawn(x, y, pillar, mapParent);
                        break;
                    case gridSpace.Inner_NW:
                        Spawn(x, y, inner_NW, mapParent);
                        break;
                    case gridSpace.Inner_NE:
                        Spawn(x, y, inner_NE, mapParent);
                        break;
                    case gridSpace.Inner_SW:
                        Spawn(x, y, inner_SW, mapParent);
                        break;
                    case gridSpace.Inner_SE:
                        Spawn(x, y, inner_SE, mapParent);
                        break;
                    case gridSpace.Inner_NW_NE:
                        Spawn(x, y, inner_NW_NE, mapParent);
                        break;
                    case gridSpace.Inner_NE_SE:
                        Spawn(x, y, inner_NE_SE, mapParent);
                        break;
                    case gridSpace.Inner_SW_SE:
                        Spawn(x, y, inner_SW_SE, mapParent);
                        break;
                    case gridSpace.Inner_SW_NW:
                        Spawn(x, y, inner_SW_NW, mapParent);
                        break;
                    case gridSpace.Inner_NW_SE:
                        Spawn(x, y, inner_NW_SE, mapParent);
                        break;
                    case gridSpace.Inner_NE_SW:
                        Spawn(x, y, inner_NE_SW, mapParent);
                        break;
                    case gridSpace.Inner_NW_NE_SE:
                        Spawn(x, y, inner_NW_NE_SE, mapParent);
                        break;
                    case gridSpace.Inner_NE_SE_SW:
                        Spawn(x, y, inner_NE_SE_SW, mapParent);
                        break;
                    case gridSpace.Inner_SE_SW_NW:
                        Spawn(x, y, inner_SE_SW_NW, mapParent);
                        break;
                    case gridSpace.Inner_SW_NW_NE:
                        Spawn(x, y, inner_SW_NW_NE, mapParent);
                        break;
                    case gridSpace.Inner_ALL:
                        Spawn(x, y, inner_ALL, mapParent);
                        break;
                    default:
                        break;
                }
                // spawn a sub floor if the tile selected needs one
                if(grid[x, y] > gridSpace.Floor && grid[x, y] != gridSpace.Roof)
                    Spawn(x, y, floors[3], mapParent);
            }
        }
    }

    void SpawnGoal()
    {
        List<Vector2> PossibleGoalLocations = new List<Vector2>();
        // loop through every grid space minus the extra border to search dead ends
        for (int x = 0; x < levelWidth - levelExtraBorder; x++)
        {
            for (int y = 0; y < levelWidth - levelExtraBorder; y++)
            {
                // if theres a floor check the spaces around it
                if (grid[x, y] == gridSpace.Floor)
                {
                    // if theres at least 3 walls, like in a dead end add it to the locations list
                    int walls = 0;
                    if (grid[x, y + 1] != gridSpace.Floor)
                        walls++;
                    if (grid[x, y - 1] != gridSpace.Floor)
                        walls++;
                    if (grid[x + 1, y] != gridSpace.Floor)
                        walls++;
                    if (grid[x - 1, y] != gridSpace.Floor)
                        walls++;
                    // make sure the goal cant spawn in the center
                    if (walls == 3)
                        PossibleGoalLocations.Add(new Vector2(x, y));
                }
            }
        }
        // remove the grid center from the list if it was in the locations
        if (PossibleGoalLocations.Contains(gridCenter))
            PossibleGoalLocations.Remove(gridCenter);
        // check to see if there was possible locations
        if (PossibleGoalLocations.Count > 0)
        {
            // remove locations near the player from the locations list
            for (int safeX = -initSafeSpace; safeX <= initSafeSpace; safeX++)
            {
                for (int safeY = -initSafeSpace; safeY <= initSafeSpace; safeY++)
                {
                    Vector2 thisLocation = new Vector2(gridCenter.x + safeX, gridCenter.y + safeY);
                    if (PossibleGoalLocations.Contains(thisLocation))
                        PossibleGoalLocations.Remove(thisLocation);
                }
            }
            // check again to see if now there are possible locations
            if (PossibleGoalLocations.Count > 0)
            {
                // declare aux variables
                int farestLocationIndex = 0;
                float farestLocation = 0f;                
                float locationDistance;                                
                // loop through every location to know its distance from the center
                for (int i = 0; i < PossibleGoalLocations.Count; i ++)
                {
                    // reset the current location
                    locationDistance = 0f;
                    // get the current location distance
                    locationDistance = Vector2.Distance(gridCenter, PossibleGoalLocations[i]);
                    // store it if its the farest one
                    if (locationDistance > farestLocation)
                    {
                        farestLocation = locationDistance;
                        farestLocationIndex = i;
                    }
                }
                // spawn the goal in the farest location
                goalPos = PossibleGoalLocations[farestLocationIndex];                
                Spawn(goalPos.x, goalPos.y, goalObj);
            }
        }
        // else there wasnt viable dead ends
    }

    void SpawnEnemies()
    {
        List<Vector2> PossibleEnemiesLocations = new List<Vector2>();       
        // loop through every grid space minus the extra border
        for (int x = 0; x < levelWidth - levelExtraBorder; x++)
        {
            for (int y = 0; y < levelWidth - levelExtraBorder; y++)
            {
                // if theres a floor add it to the locations list
                if (grid[x, y] == gridSpace.Floor)
                    PossibleEnemiesLocations.Add(new Vector2(x, y));
            }
        }
        // remove locations near the player from the locations list
        for(int safeX = -initSafeSpace; safeX <= initSafeSpace; safeX++)
        {
            for (int safeY = -initSafeSpace; safeY <= initSafeSpace; safeY++)
            {
                Vector2 thisLocation = new Vector2(gridCenter.x + safeX, gridCenter.y + safeY);
                if (PossibleEnemiesLocations.Contains(thisLocation))
                    PossibleEnemiesLocations.Remove(thisLocation);
            }
        }
        // remove the goal location from the list if it was in it
        if (PossibleEnemiesLocations.Contains(goalPos))
            PossibleEnemiesLocations.Remove(goalPos);
        // clamp the enemies for safety
        if (enemyAmount > PossibleEnemiesLocations.Count)
            enemyAmount = PossibleEnemiesLocations.Count;
        // spawn every enemy in random posible locations
        for (int i = 0; i < enemyAmount; i++)
        {
            int randomLocation = Random.Range(0, PossibleEnemiesLocations.Count);
            // use a random enemy with even chances
            int randomEnemy = Random.Range(0, enemyObj.Length);
            Spawn(PossibleEnemiesLocations[randomLocation].x, PossibleEnemiesLocations[randomLocation].y, enemyObj[randomEnemy], enemiesParent);
            PossibleEnemiesLocations.RemoveAt(randomLocation);            
        }
    }
}

// goal spawning at even random chance between every dead end
//// get the spawn chance
//float goalSpawnChance = (100f / PossibleGoalLocations.Count) / 100f;
//// loop through every location until the goal spawns
//int locationsIndex = 0;
//bool goalSpawned = false;
//while (!goalSpawned)
//{
//    if (Random.value <= goalSpawnChance)
//    {
//        Vector2 succesfulLocation = new Vector2(PossibleGoalLocations[locationsIndex].x, PossibleGoalLocations[locationsIndex].y);
//        Spawn(succesfulLocation.x, succesfulLocation.y, goalObj);
//        goalPos = succesfulLocation;
//        goalSpawned = true;
//    }
//    locationsIndex++;
//    if (locationsIndex >= PossibleGoalLocations.Count)
//        locationsIndex = 0;
//}

//-------------------------------------------------------------------------

// goal spawning in corners when there were no dead ends
//// there wasnt any dead ends, check for corners
//else
//{
//    // loop through every grid space
//    for (int x = 0; x < levelWidth - 1; x++)
//    {
//        for (int y = 0; y < levelWidth - 1; y++)
//        {
//            // if theres a floor check the spaces around it
//            if (grid[x, y] == gridSpace.Floor)
//            {
//                // if theres at least 2 walls, like in a corner add it to the locations list
//                int walls = 0;
//                if (grid[x, y + 1] != gridSpace.Floor)
//                    walls++;
//                if (grid[x, y - 1] != gridSpace.Floor)
//                    walls++;
//                if (grid[x + 1, y] != gridSpace.Floor)
//                    walls++;
//                if (grid[x - 1, y] != gridSpace.Floor)
//                    walls++;
//                // make sure the goal cant spawn in the center
//                if (walls == 2 && x != gridCenter.x && y != gridCenter.y)
//                    PossibleGoalLocations.Add(new Vector2(x, y));
//            }
//        }
//    }
//    // check to see if there was possible locations, this time is guaranteed
//    if (PossibleGoalLocations.Count > 0)
//    {
//        // remove locations near the player from the locations list
//        for (int safeX = -initSafeSpace; safeX <= initSafeSpace; safeX++)
//        {
//            for (int safeY = -initSafeSpace; safeY <= initSafeSpace; safeY++)
//            {
//                Vector2 thisLocation = new Vector2(gridCenter.x + safeX, gridCenter.y + safeY);
//                if (PossibleGoalLocations.Contains(thisLocation))
//                    PossibleGoalLocations.Remove(thisLocation);
//            }
//        }
//        // check again to see if now there are possible locations
//        if (PossibleGoalLocations.Count > 0)
//        {
//            // get the spawn chance
//            float goalSpawnChance = (100f / PossibleGoalLocations.Count) / 100f;
//            // loop through every location until the goal spawns
//            int locationsIndex = 0;
//            bool goalSpawned = false;
//            while (!goalSpawned)
//            {
//                if (Random.value <= goalSpawnChance)
//                {
//                    Vector2 succesfulLocation = new Vector2(PossibleGoalLocations[locationsIndex].x, PossibleGoalLocations[locationsIndex].y);
//                    Spawn(succesfulLocation.x, succesfulLocation.y, goalObj);
//                    goalPos = succesfulLocation;
//                    goalSpawned = true;
//                }
//                locationsIndex++;
//                if (locationsIndex >= PossibleGoalLocations.Count)
//                    locationsIndex = 0;
//            }
//        }
//        else
//        {
//            // spawn the goal in the center
//            Spawn(0f, 0f, goalObj);
//        }
//    }
//}