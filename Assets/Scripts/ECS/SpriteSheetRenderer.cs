using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;

[UpdateAfter(typeof(SpriteSheetCalculationsJob))]
public class SpriteSheetRenderer : ComponentSystem
{

    public EntityQuery entityQuery;

    protected override void OnUpdate()
    {
        entityQuery = GetEntityQuery(ComponentType.ReadOnly<CellComponent>());

        NativeArray<CellComponent> cellSpriteDataArray = entityQuery.ToComponentDataArray<CellComponent>(Allocator.TempJob);

        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        Vector4[] uv = new Vector4[1];
        Camera mainC = Camera.main;
        Material SpriteSheetMat = CreateTileMap.GetInstance().SpriteSheetMat;
        Mesh mesh = CreateTileMap.GetInstance().quadMesh;
        int shaderPropertyId = Shader.PropertyToID("_MainTex_UV");

        //Account for limitations of DrawMeshInstanced
        int sliceCount = 1023;
        for (int i = 0; i < cellSpriteDataArray.Length; i+=sliceCount)
        {
            int sliceSize = math.min(cellSpriteDataArray.Length - i, sliceCount);

            List<Matrix4x4> matrixList = new List<Matrix4x4>();
            List<Vector4> uvList = new List<Vector4>();
            for (int j = 0; j < sliceSize; j++)
            {
                CellComponent cellComponentData = cellSpriteDataArray[i + j];
                matrixList.Add(cellComponentData.matrix);
                uvList.Add(cellComponentData.uv);
            }

            materialPropertyBlock.SetVectorArray(shaderPropertyId, uvList);

            Graphics.DrawMeshInstanced(
                mesh,
                0,
                SpriteSheetMat,
                matrixList,
                materialPropertyBlock);
         }

        cellSpriteDataArray.Dispose();
    }

}
