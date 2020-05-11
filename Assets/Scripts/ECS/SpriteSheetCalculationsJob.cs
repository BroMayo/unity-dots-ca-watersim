using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using Unity.Mathematics;
using System.Numerics;

public class SpriteSheetCalculationsJob : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        JobHandle jobHandle = Entities.ForEach((ref CellComponent cc) =>
        {
            float uvWidth = 1f / 3;
            float uvHeight = 1f;
            float uvOffsetX;
            float uvOffsetY = -1f; // -1f = Full Size : 1f = Empty Cell

            //Make sure cells with liquid are rendered as water
            if (cc.Liquid > 0.05f && cc.SpriteSheetFrame != 1)
            {
                uvOffsetX = uvWidth * 1;
            }
            else
            {
                uvOffsetX = uvWidth * cc.SpriteSheetFrame;
            }

            //Fill Liquid Cells with Liquid above
            if (cc.CellType != 1 && cc.Liquid != 0)
            {
                if(cc.isDownFlowingLiquid)
                {
                    uvOffsetY = -1f;
                }
                else
                { //Scale Water Cells based on amount of liquid contained
                    uvOffsetY = math.max(-1, -(cc.Liquid));
                }
            }

            cc.uv = new UnityEngine.Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

            cc.matrix = UnityEngine.Matrix4x4.TRS(new UnityEngine.Vector3(cc.worldPos.x, cc.worldPos.y, 0), UnityEngine.Quaternion.identity,
                new UnityEngine.Vector3(cc.CellSize, cc.CellSize, 0));

        }).Schedule(inputDeps);

        return jobHandle;
    }

}
