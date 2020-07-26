using UnityEngine;
using UnityEngine.Rendering;
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

    public ComputeShader compute;

    private ComputeBuffer gridPointBuffer;
    private ComputeBuffer triBuffer;
    private ComputeBuffer countBuffer;

    public float3 planetPos = 0f;
    public float  planetRadius = 16f;

    public SphereSDFTest sphere;

    private int[] countBufferArray = new int[] { 0 };

    private CBufferTriangle[] triBufferDataFromGPU;

    private struct CBufferTriangle
    {
        public float3 a { get; private set; }
        public float3 b { get; private set; }
        public float3 c { get; private set; }

        public float3 this[int index]
        {
            get {
                switch(index)
                {
                    case 0: return a;
                    case 1: return b;
                    case 2: return c;
                    default: throw new System.IndexOutOfRangeException($"Can't get triangle vertex at index {index}");
                }
            }
        }
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();

        meshFilter.mesh = mesh;

        InitBuffers();

        Debug.Log(GameResources.ComputeShader == null);
    }

    public void InitBuffers()
    {
        var size = gridSettings.size;

        gridPointBuffer = new ComputeBuffer(gridSettings.PointAmount, sizeof(float) * 4, ComputeBufferType.Default);
        triBuffer       = new ComputeBuffer(gridSettings.BlockAmount * Constants.MAX_TRIANGLE_PER_BLOCK, sizeof(float) * 9, ComputeBufferType.Append);
        countBuffer     = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        triBufferDataFromGPU = new CBufferTriangle[gridSettings.BlockAmount * Constants.MAX_TRIANGLE_PER_BLOCK];
    }

    private void Update()
    {
        var h = GridNoiseJob.CreateAndSchedule(planetPos, planetRadius, gridSettings, noiseSettings, out NativeArray<float4> gridPointArray);
        h = SphereSDFJob.CreateAndSchedule(gridPointArray, sphere.transform.position, sphere.radius, noiseSettings.amplitude, h);
        h.Complete();
        
        var k = compute.FindKernel("Marching");

        gridPointBuffer.SetData(gridPointArray, 0, 0, gridPointArray.Length);
        triBuffer.SetCounterValue(0);

        compute.SetInts("m_Size", gridSettings.size.x, gridSettings.size.y, gridSettings.size.z);
        compute.SetFloats("m_Isolevel", gridSettings.isoLevel);
        compute.SetBuffer(k, "m_GridPointBuffer", gridPointBuffer);
        compute.SetBuffer(k, "m_TriBuffer", triBuffer);

        compute.GetKernelThreadGroupSizes(k, out uint kx, out uint ky, out uint kz);
        
        var thgx = gridSettings.size.x / (int)kx;
        var thgy = gridSettings.size.y / (int)ky;
        var thgz = gridSettings.size.z / (int)kz;

        compute.Dispatch(k, thgx, thgy, thgz);

        gridPointArray.Dispose();

        ComputeBuffer.CopyCount(triBuffer, countBuffer, 0);
        countBuffer.GetData(countBufferArray, 0, 0, 1);

        var tCount = countBufferArray[0];

        if(tCount == 0)
            return;

        triBuffer.GetData(triBufferDataFromGPU, 0, 0, tCount);

        var vertices  = new NativeArray<Vertex>(tCount * 3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var triangles = new NativeArray<Triangle>(tCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        
        for(var i = 0; i < tCount; i++)
        {
            var vIndex = i * 3;

            vertices[vIndex    ] = new Vertex { pos = triBufferDataFromGPU[i][0] };
            vertices[vIndex + 1] = new Vertex { pos = triBufferDataFromGPU[i][1] };
            vertices[vIndex + 2] = new Vertex { pos = triBufferDataFromGPU[i][2] };

            triangles[i] = new Triangle(0, 1, 2) + vIndex;
        }

        mesh.Update(triangles, vertices);
        mesh.RecalculateNormals();
    }

    public void DisposeBuffers()
    {
        gridPointBuffer.Dispose();
        triBuffer.Dispose();
        countBuffer.Dispose();
    }

    private void OnDestroy()
    {
        DisposeBuffers();
    }
}