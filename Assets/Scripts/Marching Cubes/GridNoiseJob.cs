using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

using Saitama.Mathematics;

[BurstCompile]
public struct GridNoiseJob : IJobParallelFor
{
    [ReadOnly]
    public int3 GridSize;

    [ReadOnly]
    public float3 Offset;

    [ReadOnly]
    public float Frequency;

    [ReadOnly]
    public float Amplitude;

    [WriteOnly]
    public NativeArray<float> GridNoise;

    public void Execute(int index)
    {
        var pos = (float3)index.To3D(GridSize);
        var worldPos = pos + Offset;

        float y = worldPos.y / 2f;
        GridNoise[index] = -y + noise.snoise((pos + Offset) * Frequency) * Amplitude + ((y % 4) / 2);
    }

    public static JobHandle Create(in int3 gridSize, in float3 noiseOffset, in float noiseFrequency, in float noiseAmplitude, out NativeArray<float> gridNoise, JobHandle inputDeps = default)
    {
        gridNoise = new NativeArray<float>(gridSize.x * gridSize.y * gridSize.z, Allocator.TempJob);

        return new GridNoiseJob
        {
            GridSize    = gridSize,
            Offset      = noiseOffset,
            Frequency   = noiseFrequency,
            Amplitude   = noiseAmplitude,
            GridNoise   = gridNoise,
        }
        .Schedule(gridNoise.Length, 32, inputDeps);
    }
}
