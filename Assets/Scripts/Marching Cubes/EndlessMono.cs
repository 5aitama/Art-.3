using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Saitama.Mathematics;


public class EndlessMono : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform sphere;
    public float sphereRadius;

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(sphere.position, sphereRadius);
        Debug.Log(math.length(sphere.position) - sphereRadius);
    }
}
