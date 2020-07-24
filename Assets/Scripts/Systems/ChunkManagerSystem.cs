using Unity.Jobs;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;
using UnityEngine.Rendering;

using Saitama.ProceduralMesh;

[AlwaysUpdateSystem]
public class ChunkManagerSystem : ComponentSystem
{
    private struct PendingChunk
    {
        public float3 position;
        public Entity entity;
    }

    private UnityEngine.Material chunkMaterial;

    public static int3 ChunkSize = 32;

    private NativeQueue<PendingChunk> pendingChunks;

    protected override void OnCreate()
    {
        chunkMaterial = UnityEngine.Resources.Load<UnityEngine.Material>("Materials/ChunkMaterial");
        pendingChunks = new NativeQueue<PendingChunk>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        pendingChunks.Dispose();
    }

    protected override void OnUpdate()
    {
        for(var i = 0; i < 5; i++)
        if(pendingChunks.TryDequeue(out PendingChunk item))
        {
            InternalCreate(item.entity, item.position);
        }

        Entities.WithAllReadOnly<TagChunkNeedBuild>().ForEach((Entity e, in ref Chunk chunk, ref RenderBounds renderBounds) =>
        {
            JobHandle handle = default;

            handle = GridNoiseJob.CreateAndSchedule(chunk, out NativeArray<float> gridNoise);
            handle = MarchingJob.CreateAndSchedule(chunk, gridNoise, out NativeList<Vertex> v, handle);

            handle.Complete();

            CalculateTrisJob.Create(v, out NativeArray<Triangle> t, handle).Complete();

            var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(e);

            renderMesh.mesh.Update(t, v.AsArray());
            renderMesh.mesh.RecalculateNormals();

            renderBounds.Value = new Unity.Mathematics.AABB
            {
                Center = renderMesh.mesh.bounds.center,
                Extents = renderMesh.mesh.bounds.extents,
            };

            gridNoise.Dispose();
            v.Dispose();
            t.Dispose();

            EntityManager.RemoveComponent<TagChunkNeedBuild>(e);
        });
    }

    public Entity Create(float3 position)
    {
        var e = EntityManager.CreateEntity();
        pendingChunks.Enqueue(new PendingChunk { entity = e, position = position });
        return e;
    }

    private void InternalCreate(Entity e, float3 position)
    {
        EntityManager.AddComponentData(e, new Translation
        {
            Value = position,
        });

        EntityManager.AddComponentData(e, new Rotation
        {
            Value = quaternion.identity,
        });

        EntityManager.AddComponent<LocalToWorld>(e);
        EntityManager.AddComponent<RenderBounds>(e);

        EntityManager.AddSharedComponentData<RenderMesh>(e, new RenderMesh
        {
            mesh                 = new Mesh(),
            material             = chunkMaterial,
            subMesh              = 0,
            layer                = 0,
            castShadows          = ShadowCastingMode.On,
            receiveShadows       = true,
            needMotionVectorPass = false,
        });

        EntityManager.AddComponentData(e, new Chunk
        {
            IsoLevel            = 0f,
            Size                = 32,
            NoisePosition       = position,
            NoiseAmplitude      = 128,
            NoiseFrequency      = 0.025f,
            NoiseOctaves        = 4,
            NoisePersistence    = 0.5f,
        });

        EntityManager.AddComponent<TagChunkNeedBuild>(e);
    }
}
