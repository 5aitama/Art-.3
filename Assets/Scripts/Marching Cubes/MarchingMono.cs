using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

using Saitama.ProceduralMesh;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingMono : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh mesh;

    public ScriptableGrid gridSettings;
    public ScriptableOctaveNoise noiseSettings;


    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();

        meshFilter.mesh = mesh;
    }

    private void Update()
    {
        JobHandle handle = default;

        handle = GridNoiseJob.CreateAndSchedule(gridSettings, noiseSettings, out NativeArray<float> gridNoise);
        handle = MarchingJob.CreateAndSchedule(gridSettings, gridNoise, out NativeList<Vertex> v, handle);
       
        handle.Complete();

        CalculateTrisJob.Create(v.AsArray(), out NativeArray<Triangle> t, handle).Complete();

        mesh.Update(t, v);
        mesh.RecalculateNormals();

        gridNoise.Dispose();
        v.Dispose();
        t.Dispose();
    }
}
