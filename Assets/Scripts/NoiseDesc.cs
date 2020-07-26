using UnityEngine;
using Unity.Mathematics;

[CreateAssetMenu(menuName = "Procedural/Noise", fileName = "New noise descriptor")]
public class NoiseDesc : ScriptableObject
{
    [Tooltip("The position of the noise in the world")]
    public float3 Position;

    [Tooltip("The frequency of the noise (equivalent to the scale)"), Range(0f, 1f)]
    public float Frequency;

    [Tooltip("The maximum height"), Range(1f, 64f)]
    public float Amplitude;

    [Tooltip("How much all layers is mixed each other"), Range(0f, 1f)]
    public float Persistance;

    [Tooltip("Amount of layer"), Range(1, 8)]
    public int   Octaves;
}
