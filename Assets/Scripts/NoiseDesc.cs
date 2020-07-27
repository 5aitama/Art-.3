using UnityEngine;
using Unity.Mathematics;

[CreateAssetMenu(menuName = "Procedural/Noise", fileName = "New noise descriptor")]
public class NoiseDesc : ScriptableObject
{
    [Tooltip("The position of the noise in the world")]
    public float3 Position;

    [Tooltip("The frequency of the noise (equivalent to the scale)"), Range(0f, 1f)]
    public float Frequency = 0.1f;

    [Tooltip("The maximum height"), Range(1f, 64f)]
    public float Amplitude = 10f;

    [Tooltip("How much all layers is mixed each other"), Range(0f, 1f)]
    public float Persistance = 0.5f;

    [Tooltip("Amount of layer"), Range(1, 8)]
    public int   Octaves = 4;

    [Tooltip("Intersection threshold"), Range(-1f, 1f)]
    public float Isolevel = 0f;
}
