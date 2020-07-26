using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(menuName = "Marching/Grid", fileName = "New grid settings")]
public class ScriptableGrid : ScriptableObject
{
    public float3 position;
    public int3 size = 32;
    public float isoLevel = 0f;

    public int BlockAmount
        => (size.x - 1) * (size.y - 1) * (size.z - 1);

    public int PointAmount 
        => size.x * size.y * size.z;
}
