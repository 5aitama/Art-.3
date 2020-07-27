using UnityEngine;
using Unity.Mathematics;

[System.Serializable, CreateAssetMenu(menuName = "Procedural/Planet", fileName = "New Planet")]
public class PlanetDesc : ScriptableObject
{
    [Range(1f, 64f), Tooltip("The radius of the planet.")]
    public float radius;

    [Tooltip("The position of the planet in the world.")]
    public float3 position;

    [Tooltip("The noise descriptor for the planet")]
    public NoiseDesc noiseDesc;
}
