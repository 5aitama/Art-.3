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

    [ReadOnly]
    public float Persistence;

    [ReadOnly]
    public int Octaves;

    [WriteOnly]
    public NativeArray<float> GridNoise;

    public void Execute(int index)
    {
        var pos = (float3)index.To3D(GridSize);
        var worldPos = pos + Offset;

        float y = worldPos.y / 2f;
        GridNoise[index] = -y + OctaveNoise(worldPos, Frequency, Amplitude, Persistence, Octaves) + ((y % 4) / 2);
    }

    private float OctaveNoise(float3 pos, float frequency, float amplitude, float persistence, int octaves)
    {
        var total = 0f;
        var max = 0f;

        for(var i = 0; i < octaves; i++) {
            total += noise.snoise(pos * frequency) * amplitude;
            
            max += amplitude;
            
            amplitude *= persistence;
            frequency *= 2f;
        }

        return total / max;
    }

    public static JobHandle CreateAndSchedule(in ScriptableGrid gridSettings, in ScriptableOctaveNoise noiseSettings, out NativeArray<float> gridNoise, JobHandle inputDeps = default)
    {
        gridNoise = new NativeArray<float>(gridSettings.PointAmount, Allocator.TempJob);

        return new GridNoiseJob
        {
            GridSize    = gridSettings.size,
            Offset      = noiseSettings.position,
            Frequency   = noiseSettings.frequency,
            Amplitude   = noiseSettings.amplitude,
            Persistence = noiseSettings.persistence,
            Octaves     = noiseSettings.octaves,
            GridNoise   = gridNoise,
        }
        .Schedule(gridNoise.Length, 32, inputDeps);
    }
}
