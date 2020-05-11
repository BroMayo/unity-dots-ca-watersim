using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct CellComponent : IComponentData
{
    //Empty(0) or Solid(1)
    public int CellType;

    //Rendering Data
    //0 = Empty //1 = Water //2 == Wall
    public int SpriteSheetFrame;
    public Vector4 uv;
    public Matrix4x4 matrix;
    public bool isDownFlowingLiquid;

    //CellSize
    public float CellSize;

    //Grid Pos & index
    public int xGrid;
    public int yGrid;
    public int index;

    //World Pos
    public Unity.Mathematics.float2 worldPos;
    
    //Check is Water is settled
    public bool Settled;
    public int SettleCount;

    public float Liquid;
    
    //Neighbor Cells
    public int BottomIndex;
    public int TopIndex;
    public int LeftIndex;
    public int RightIndex;

    //Values stored for modifying self and neighbor
    public float modifySelf;
    public float modifyBottom;
    public float modifyTop;
    public float modifyLeft;
    public float modifyRight;
}
