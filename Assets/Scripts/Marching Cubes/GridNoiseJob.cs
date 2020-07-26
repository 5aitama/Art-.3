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

    [ReadOnly]
    public int TerracedHeight;

    [WriteOnly]
    public NativeArray<float4> GridNoise;

    [ReadOnly]
    public float3 PlanetPos;

    [ReadOnly]
    public float PlanetRadius;

    public void Execute(int index)
    {
        var pos = (float3)index.To3D(GridSize);
        
        var worldPos = pos + Offset;
        
        var n = OctaveNoise(worldPos, Frequency, Amplitude, Persistence, Octaves);
        n = (n + 1f) / 2f;
        n *= Amplitude;

        var dir = math.normalize(PlanetPos - worldPos);

        n += PlanetRadius - math.length(worldPos);

        GridNoise[index] = new float4(pos, n);
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

    public static JobHandle CreateAndSchedule(in float3 planetPos, in float planetRadius, in ScriptableGrid gridSettings, in ScriptableOctaveNoise noiseSettings, out NativeArray<float4> gridNoise, JobHandle inputDeps = default)
    {
        gridNoise = new NativeArray<float4>(gridSettings.PointAmount, Allocator.TempJob);

        return new GridNoiseJob
        {
            GridSize        = gridSettings.size,
            Offset          = noiseSettings.position,
            Frequency       = noiseSettings.frequency,
            Amplitude       = noiseSettings.amplitude,
            Persistence     = noiseSettings.persistence,
            Octaves         = noiseSettings.octaves,
            GridNoise       = gridNoise,
            TerracedHeight  = noiseSettings.terracingHeight,
            PlanetPos       = planetPos,
            PlanetRadius    = planetRadius,
        }
        .Schedule(gridNoise.Length, 32, inputDeps);
    }
}
