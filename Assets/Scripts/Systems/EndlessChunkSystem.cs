using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Saitama.Mathematics;

public class EndlessChunkSystem : ComponentSystem
{
    private Transform cameraTransform;
    public int3 Range = new int3(10, 5, 10);
    private ChunkManagerSystem m_ChunkManagerSystem;

    private NativeMultiHashMap<int3, Entity> arrayA;

    protected override void OnCreate()
    {
        m_ChunkManagerSystem = World.GetOrCreateSystem<ChunkManagerSystem>();
        arrayA = new NativeMultiHashMap<int3, Entity>(128, Allocator.Persistent);
    }

    protected override void OnStartRunning()
    {
        cameraTransform = Camera.main.transform;
    }

    protected override void OnDestroy()
    {
        arrayA.Dispose();
    }

    protected override void OnUpdate()
    {
        var chunkOffset = (ChunkManagerSystem.ChunkSize) / 2;

        var cameraPos = (int3)math.round((float3)cameraTransform.position / chunkOffset) * chunkOffset;

        var r = (Range / 2 - 1) * ChunkManagerSystem.ChunkSize.x;

        var maxDistance = math.sqrt(r * r + r * r);
        var m = math.max(maxDistance.x, math.max(maxDistance.y, maxDistance.z));
        for(var i = 0; i < Range.x * Range.y * Range.z; i++)
        {
            var localPos = i.To3D(Range) - Range / 2;
            var worldPos = localPos * chunkOffset + cameraPos;
            
            if(arrayA.ContainsKey(worldPos) || math.distance(worldPos, cameraPos) >= m)
                continue;

            var e = m_ChunkManagerSystem.Create(worldPos);
            arrayA.Add(worldPos, e);
        }

        var kv = arrayA.GetKeyValueArrays(Allocator.Temp);
        for(var i = 0; i < kv.Length; i++)
        {
            if(math.distance(kv.Keys[i], cameraPos) >= m)
            {
                EntityManager.DestroyEntity(kv.Values[i]);
                arrayA.Remove(kv.Keys[i]);
            }
        }
    }
}
