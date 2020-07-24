using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;

using Saitama.ProceduralMesh;

[BurstCompile]
public struct CalculateTrisJob : IJobParallelFor
{
    [WriteOnly]
    public NativeArray<Triangle> Triangles;

    public void Execute(int index)
    {
        Triangles[index] = new Triangle(0, 1, 2) + index * 3;
    }

    public static JobHandle Create(in NativeArray<Vertex> v, out NativeArray<Triangle> t, JobHandle inputDeps = default)
    {
        t = new NativeArray<Triangle>(v.Length / 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        return new CalculateTrisJob
        {
            Triangles = t,
        }
        .Schedule(v.Length / 3, 64, inputDeps);
    }

    public static JobHandle Create(in int vertexCount, out NativeArray<Triangle> t, JobHandle inputDeps = default)
    {
        t = new NativeArray<Triangle>(vertexCount / 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        return new CalculateTrisJob
        {
            Triangles = t,
        }
        .Schedule(vertexCount / 3, 64, inputDeps);
    }
}
