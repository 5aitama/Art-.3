using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

using Saitama.ProceduralMesh;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingMono : MonoBehaviour
{
    private MeshFilter mf;
    private Mesh m;

    public int3 gridSize = 16;
    
    [Range(-1f, 1f)]
    public float isoLevel = 0f;

    public float3 noiseOffset = 0f;

    [Range(0f, 1f)]
    public float noiseFrequency = 0.05f;
    
    public float noiseAmplitude = 1f;


    private void Awake()
    {
        mf = GetComponent<MeshFilter>();
        m = new Mesh();

        mf.mesh = m;
    }

    private void Update()
    {
        var handle = GridNoiseJob.Create(gridSize, noiseOffset, noiseFrequency, noiseAmplitude, out NativeArray<float> gridNoise);
        handle = MarchingJob.Create(isoLevel, gridSize, gridNoise, out NativeList<Vertex> v, handle);
        handle.Complete();

        CalculateTrisJob.Create(v.AsArray(), out NativeArray<Triangle> t, handle).Complete();

        m.Update(t, v);
        m.RecalculateNormals();

        gridNoise.Dispose();
        v.Dispose();
        t.Dispose();
    }
}
