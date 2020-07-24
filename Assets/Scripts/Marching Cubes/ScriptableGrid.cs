using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(menuName = "Marching/Grid", fileName = "New grid settings")]
public class ScriptableGrid : ScriptableObject
{
    public float3 position;
    public int3 size = 32;
    public float isoLevel = 0f;

    public int PointAmount 
        => size.x * size.y * size.z;
}
