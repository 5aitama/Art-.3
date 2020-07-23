using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using Saitama.ProceduralMesh;

public struct CalculateTrisJob : IJobParallelFor
{
    
    [WriteOnly]
    public NativeArray<Triangle> Triangles;

    public void Execute(int index)
    {
        Triangles[index] = new Triangle(0, 1, 2) + index * 3;
    }

    public static JobHandle Create(in NativeArray<Vertex> vertices, out NativeArray<Triangle> triangles, JobHandle inputDeps = default)
    {
        triangles = new NativeArray<Triangle>(vertices.Length / 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        return new CalculateTrisJob
        {
            Triangles = triangles,
        }
        .Schedule(vertices.Length / 3, 64, inputDeps);
        
    }
}
