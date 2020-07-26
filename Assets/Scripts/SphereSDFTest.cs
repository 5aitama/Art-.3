using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class SphereSDFTest : MonoBehaviour
{
    public float radius = 1;

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
        //Debug.Log(math.max(0, SphereSDF(transform.position, radius)));
    }

    private float SphereSDF(float3 position, float radius)
    {
        return radius - math.length(position);
    }
}
