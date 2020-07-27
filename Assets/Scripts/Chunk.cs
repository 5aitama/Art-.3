using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

using Saitama.ProceduralMesh;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public PlanetDesc planet;

    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;
    private MeshFilter   meshFilter;
    private Mesh         mesh;

    // Used to store the noise data
    private ComputeBuffer gridBuffer;
    // Used to store triangle vertices
    private ComputeBuffer trisBuffer;
    // Used to store the amount of triangle in trisBuffer
    private ComputeBuffer countBuffer;

    // Used to store the value of countBuffer
    private int[] countBufferArray = new int[] { 0 };
    // Used to temporary store data from trisBuffer
    private Triangle[] trisBufferTempArray;
    
    // Used to store grid data
    private NativeArray<float4> gridData;

    private void Awake()
    {
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshRenderer.material = GameResources.ChunkMaterial;
    }

    public void UpdateMeshCollider()
    {
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    public void InitBuffers()
    {
        gridBuffer = new ComputeBuffer(GameResources.ChunkPointAmount, sizeof(float) * 4, ComputeBufferType.Default);
        trisBuffer = new ComputeBuffer(GameResources.ChunkBlockAmount * Constants.MAX_TRIANGLE_PER_BLOCK, sizeof(float) * 9, ComputeBufferType.Append);
        countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        trisBufferTempArray = new Triangle[GameResources.ChunkBlockAmount * Constants.MAX_TRIANGLE_PER_BLOCK];
    }

    public JobHandle InitializeGridData(JobHandle inputDeps = default)
    {

        if(!gridData.IsCreated)
            gridData = new NativeArray<float4>(GameResources.ChunkSize.Amount(), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        
        var jobHandle = GridNoiseJob.CreateAndSchedule(planet, GameResources.ChunkSize, transform.position, ref gridData, inputDeps);
        
        // Do some other jobs here...

        return jobHandle;
    }

    public void BuildMesh()
    {
        var shader = GameResources.ComputeShader;
        var k = shader.FindKernel("Marching");

        gridBuffer.SetData(gridData, 0, 0, gridData.Length);
        trisBuffer.SetCounterValue(0);

        shader.SetInts("m_Size", GameResources.ChunkSize.x, GameResources.ChunkSize.y, GameResources.ChunkSize.z);
        shader.SetFloats("m_Isolevel", planet.noiseDesc.Isolevel);
        shader.SetBuffer(k, "m_GridPointBuffer", gridBuffer);
        shader.SetBuffer(k, "m_TriBuffer", trisBuffer);

        shader.GetKernelThreadGroupSizes(k, out uint kx, out uint ky, out uint kz);
        
        var thgx = (int)math.ceil(GameResources.ChunkSize.x / (float)kx);
        var thgy = (int)math.ceil(GameResources.ChunkSize.y / (float)ky);
        var thgz = (int)math.ceil(GameResources.ChunkSize.z / (float)kz);

        shader.Dispatch(k, thgx, thgy, thgz);

        ComputeBuffer.CopyCount(trisBuffer, countBuffer, 0);
        countBuffer.GetData(countBufferArray, 0, 0, 1);

        var tCount = countBufferArray[0];

        if(tCount == 0)
            return;

        trisBuffer.GetData(trisBufferTempArray, 0, 0, tCount);

        var vertices  = new NativeArray<Vertex>(tCount * 3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var triangles = new NativeArray<Saitama.ProceduralMesh.Triangle>(tCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        
        for(var i = 0; i < tCount; i++)
        {
            var vIndex = i * 3;

            vertices[vIndex    ] = new Vertex { pos = trisBufferTempArray[i][0] };
            vertices[vIndex + 1] = new Vertex { pos = trisBufferTempArray[i][1] };
            vertices[vIndex + 2] = new Vertex { pos = trisBufferTempArray[i][2] };

            triangles[i] = new Saitama.ProceduralMesh.Triangle(0, 1, 2) + vIndex;
        }

        mesh.Update(triangles, vertices);
        mesh.RecalculateNormals();

        UpdateMeshCollider();
    }

    public void Clean()
    {
        mesh.Clear();
    }

    public void DisposeBuffers()
    {
        gridBuffer.Dispose();
        trisBuffer.Dispose();
        countBuffer.Dispose();
    }

    private void OnDestroy()
    {
        DisposeBuffers();

        // Don't forget to dispose native array!!
        gridData.Dispose();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, (float3)GameResources.ChunkSize - 1);
    }
}
