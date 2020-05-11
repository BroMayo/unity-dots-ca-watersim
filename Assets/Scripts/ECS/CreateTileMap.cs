using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Rendering;
using Unity.Transforms;
using System.Linq.Expressions;
using Unity.Mathematics;
using System.Numerics;
using System;
using Unity.Jobs;
using Unity.Burst;

public class CreateTileMap : MonoBehaviour
{
    public int GridWidth = 80;
    public int GridHeight = 40;
    //Liquid placed when clicked
    public int liquidPerClick = 5;

    [SerializeField]
    float CellSize = 1;

    private Entity[] Cells;

    bool Fill;

    Unity.Entities.EntityManager em;

    EntityArchetype CellArchtype;

    public Mesh quadMesh;
    public Material SpriteSheetMat;

    private static CreateTileMap instance;

    public static CreateTileMap GetInstance()
    {
        return instance;
    }

    void Awake()
    {
        instance = this;

        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = ((float)GridWidth * CellSize) / ((float)GridHeight * CellSize);

        if (screenRatio >= targetRatio)
        {
            Camera.main.orthographicSize = ((float)GridHeight * CellSize) / 2;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            Camera.main.orthographicSize = ((float)GridHeight * CellSize) / 2 * differenceInSize;
        }

        //Grab Entity Manager
        em = World.DefaultGameObjectInjectionWorld.EntityManager;

        //Cell ArchType
        CellArchtype = em.CreateArchetype(
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(Rotation),
            typeof(NonUniformScale),
            typeof(CellComponent));

        // Generate our grid
        CreateGrid();
    }

    // Update is called once per frame
    private CellComponent clickedCell;
    void Update()
    {
        // Convert mouse position to Grid Coordinates
        UnityEngine.Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int x = (int)((pos.x - (this.transform.position.x - (GridWidth * CellSize / 2))));
        int y = -(int)((pos.y - (this.transform.position.y + (GridHeight * CellSize / 2) + CellSize)));

        // Check if we are filling or erasing walls
        if (Input.GetMouseButtonDown(0))
        {
            if ((x > 0 && x < GridWidth) && (y > 0 && y < GridHeight))
            {  //Click is inside grid, grab cell component data
                clickedCell = em.GetComponentData<CellComponent>(Cells[CalculateCellIndex(x, y, GridWidth)]);
                if (clickedCell.CellType == 0)
                {
                    Fill = true;
                }
                else
                {
                    Fill = false;
                }
            }
        }

        // Left click draws/erases walls
        if (Input.GetMouseButton(0))
        {
            if (x != 0 && y != 0 && x != GridWidth - 1 && y != GridHeight - 1)
            {
                if ((x > 0 && x < GridWidth) && (y > 0 && y < GridHeight))
                {
                    clickedCell = em.GetComponentData<CellComponent>(Cells[CalculateCellIndex(x, y, GridWidth)]);
                    if (Fill)
                    {
                        clickedCell.CellType = 1;
                        clickedCell.SpriteSheetFrame = 2;
                        clickedCell.Liquid = 0;
                        em.SetComponentData(Cells[CalculateCellIndex(x, y, GridWidth)], clickedCell);
                    }
                    else
                    {
                        clickedCell.CellType = 0;
                        clickedCell.Liquid = 0;
                        clickedCell.SpriteSheetFrame = 0;
                        em.SetComponentData(Cells[CalculateCellIndex(x, y, GridWidth)], clickedCell);
                    }
                }
            }
        }

        // Right click places liquid
        if (Input.GetMouseButton(1))
        {
            clickedCell = em.GetComponentData<CellComponent>(Cells[CalculateCellIndex(x, y, GridWidth)]);
            if ((x > 0 && x < GridWidth) && (y > 0 && y < GridHeight))
            {
                clickedCell.CellType = 0;
                clickedCell.Liquid = liquidPerClick;
                clickedCell.SpriteSheetFrame = 1;
                em.SetComponentData(Cells[CalculateCellIndex(x, y, GridWidth)], clickedCell);
            }
        }
    }

    void CreateGrid()
    {

        //Create Entity TileMap
        Cells = new Entity[GridWidth * GridHeight];

        //Make this object transform center of map
        UnityEngine.Vector3 offset = new UnityEngine.Vector3(
            this.transform.position.x - (((((float)GridWidth * CellSize)) / 2) - (CellSize/2)),
            this.transform.position.y + (((((float)GridHeight * CellSize)) / 2) + (CellSize / 2)), 0);

        // Create Tiles
        bool isWall;
        int index = 0;
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                Entity cell;
                isWall = false;

                //Create Cell Entity
                cell = em.CreateEntity(CellArchtype);

                // Border Tiles
                if (x == 0 || y == 0 || x == GridWidth - 1 || y == GridHeight - 1)
                {
                    isWall = true;
                }

                //Calculate World Pos
                float xpos = offset.x + (float)(x * CellSize);
                float ypos = offset.y - (float)(y * CellSize);
                float3 pos = new float3(xpos, ypos, 0);

                //Fill Position Data
                em.SetComponentData(cell, new Translation
                {
                    Value = pos
                });

                //Calc Neighbors Indexs
                int bottomIndex = -1;
                int topIndex = -1;
                int leftIndex = -1;
                int rightIndex = -1;

                if (index - GridWidth >= 0)
                {
                    topIndex = (index - GridWidth);  // north
                }
                if (index % GridWidth != 0)
                {
                    leftIndex = (index - 1);  // west
                }

                if (((index + 1) % GridWidth) != 0)
                {
                    rightIndex = index + 1;  // east
                }

                if (index + GridWidth < Cells.Length)
                {
                    bottomIndex = (index + GridWidth);  // south
                }

                if (isWall)
                {
                    //Set CellComponent Data
                    em.SetComponentData(cell, new CellComponent
                    {
                        xGrid = x,
                        yGrid = y,
                        CellType = 1, //Solid
                        SpriteSheetFrame = 2, //Wall Frame
                        worldPos = new Unity.Mathematics.float2(xpos, ypos),
                        CellSize = CellSize,
                        Liquid = 0f,
                        Settled = false,
                        index = index,
                        LeftIndex = leftIndex,
                        RightIndex = rightIndex,
                        BottomIndex = bottomIndex,
                        TopIndex = topIndex
                    });
                }else{
                    //Set Empty Cell Data
                    em.SetComponentData(cell, new CellComponent
                    {
                        xGrid = x,
                        yGrid = y,
                        CellType = 0,//NOT Solid
                        SpriteSheetFrame = 0, //Empty Frame
                        worldPos = new Unity.Mathematics.float2(xpos, ypos),
                        CellSize = CellSize,
                        Liquid = 0f, //Empty
                        Settled = false,
                        index = index,
                        LeftIndex = leftIndex,
                        RightIndex = rightIndex,
                        BottomIndex = bottomIndex,
                        TopIndex = topIndex
                    });
                }

                //Add Cell to Array
                Cells[CalculateCellIndex(x, y, GridWidth)] = cell;
                index++;
            }
        }
    }

    private int CalculateCellIndex(int x, int y, int gridWidth)
    {
        return x + y * gridWidth;
    }

}
