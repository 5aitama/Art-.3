using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(menuName = "Marching/Octave Noise", fileName = "New octave noise settings")]
public class ScriptableOctaveNoise : ScriptableObject
{
    public float3 position;

    [Range(0f, 1f)]
    public float frequency = 0.1f;

    [Range(0f, 128f)]
    public float amplitude = 16f;

    [Range(0f, 1f)]
    public float persistence = 0.5f;
    
    [Range(1, 16)]
    public int octaves = 4;

    public int terracingHeight = 4;
}
