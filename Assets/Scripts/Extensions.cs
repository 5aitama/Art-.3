using UnityEngine;
using Unity.Mathematics;

public static class Extensions
{
    /// <summary>
    /// Return the amount of element inside 3 dimensional array.
    /// <param name="x">The amount of element in each axis</param>
    /// </summary>
    public static int Amount(this int3 x)
        => x[0] * x[1] * x[2];
}
