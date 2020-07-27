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
        var pos = (float3)index.To3D(GridSize) - (float3)GridSize / 2f;
        
        var worldPos = pos + Offset;
        
        var n = OctaveNoise(worldPos, Frequency, Amplitude, Persistence, Octaves);
        n = (n + 1f) / 2f;
        n *= Amplitude;
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

    public static JobHandle CreateAndSchedule(in PlanetDesc planetDesc, in int3 gridSize, in float3 position, ref NativeArray<float4> gridNoise, JobHandle inputDeps = default)
    {
        return new GridNoiseJob
        {
            GridSize        = gridSize,
            Offset          = planetDesc.noiseDesc.Position + position,
            Frequency       = planetDesc.noiseDesc.Frequency,
            Amplitude       = planetDesc.noiseDesc.Amplitude,
            Persistence     = planetDesc.noiseDesc.Persistance,
            Octaves         = planetDesc.noiseDesc.Octaves,
            GridNoise       = gridNoise,
            PlanetPos       = planetDesc.position,
            PlanetRadius    = planetDesc.radius,
        }
        .Schedule(gridNoise.Length, 32, inputDeps);
    }
}
