using Unity.Entities;
using Unity.Mathematics;

public struct Chunk : IComponentData
{
    public float  IsoLevel;
    public int3   Size;
    public int    PointAmount => Size.x * Size.y * Size.z;

    public float3 NoisePosition;
    public int    NoiseOctaves;
    public float  NoiseAmplitude;
    public float  NoiseFrequency;
    public float  NoisePersistence;

    public Chunk(in float3 position, in ScriptableGrid grid, in ScriptableOctaveNoise noise)
    {
        Size                = grid.size;
        IsoLevel            = grid.isoLevel;
        NoisePosition       = noise.position + position;
        NoiseOctaves        = noise.octaves;
        NoiseAmplitude      = noise.amplitude;
        NoiseFrequency      = noise.frequency;
        NoisePersistence    = noise.persistence;
    }
}
