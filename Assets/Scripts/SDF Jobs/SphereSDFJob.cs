using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct SphereSDFJob : IJobParallelFor
{
    [ReadOnly]
    public float3 Position;
    
    [ReadOnly]
    public float Radius;

    [ReadOnly]
    public float Amplitude;

    public NativeArray<float4> gridPoints;
    
    public void Execute(int index)
    {
        var w = gridPoints[index].w + math.max(gridPoints[index].w, SphereSDF(gridPoints[index].xyz - Position, Radius) * Amplitude);
        gridPoints[index] = new float4(gridPoints[index].xyz, w);
    }

    private float SphereSDF(float3 pos, float radius = 1f)
        => radius - math.length(pos);

    public static JobHandle CreateAndSchedule(in NativeArray<float4> gridPoints, in float3 shperePosition, in float sphereRadius, in float Amplitude, JobHandle inputDeps)
    {
        return new SphereSDFJob
        {
            Position    = shperePosition,
            Radius      = sphereRadius,
            Amplitude   = Amplitude,
            gridPoints  = gridPoints,
        }
        .Schedule(gridPoints.Length, 64, inputDeps);
    }
}
