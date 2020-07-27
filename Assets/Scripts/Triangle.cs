using Unity.Mathematics;

public struct Triangle
{
    public float3 a { get; private set; }
    public float3 b { get; private set; }
    public float3 c { get; private set; }

    public float3 this[int index]
    {
        get {
            switch (index)
            {
                case 0: return a;
                case 1: return b;
                case 2: return c;

                default: throw new System.IndexOutOfRangeException($"Can't get triangle vertex at index {index}");
            }
        }
    }
}
