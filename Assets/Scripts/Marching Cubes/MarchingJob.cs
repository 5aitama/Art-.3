using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

using Saitama.ProceduralMesh;
using Saitama.Mathematics;

public struct GridEdge
{
    public float3 pos;
    public float val;

    public GridEdge(int3 pos, float val)
    {
        this.pos = pos;
        this.val = val;
    }

    public GridEdge(in int3 pos, in int3 gridSize, in NativeArray<float4> gridNoise)
    {
        this.pos = pos;
        this.val = gridNoise[pos.To1D(gridSize)].w;
    }
}

[BurstCompile]
public struct MarchingJob : IJobParallelFor
{
    [ReadOnly]
    public int3 GridSize;

    [ReadOnly]
    public NativeArray<float4> GridNoise;

    [ReadOnly]
    public float Isolevel;

    [WriteOnly]
    public NativeList<Vertex>.ParallelWriter Vertices;

    public void Execute(int index)
    {
        var position = index.To3D(GridSize);

        if(position.x >= GridSize.x - 1 || position.y >= GridSize.y - 1 || position.z >= GridSize.z - 1)
            return;

        var gridEdges = GetGridValues(position, GridSize, GridNoise, Allocator.Temp);
        var cubeIndex = DetermineIndex(Isolevel, gridEdges);

        if(Constants.EdgeTable[cubeIndex] == 0)
            return;
        
        var vertices = FindIntersectVertices(cubeIndex, Isolevel, gridEdges, Allocator.Temp);

        AddVertices(cubeIndex, vertices, ref Vertices);
    }

    public static NativeArray<GridEdge> GetGridValues(in int3 position, in int3 gridSize, in NativeArray<float4> gridNoise, Allocator allocator)
    {
        var values = new NativeArray<GridEdge>(8, allocator, NativeArrayOptions.UninitializedMemory);

        values[0] = new GridEdge(position + new int3(0, 0, 1), gridSize, gridNoise);
        values[1] = new GridEdge(position + new int3(1, 0, 1), gridSize, gridNoise);
        values[2] = new GridEdge(position + new int3(1, 0, 0), gridSize, gridNoise);
        values[3] = new GridEdge(position + new int3(0, 0, 0), gridSize, gridNoise);

        values[4] = new GridEdge(position + new int3(0, 1, 1), gridSize, gridNoise);
        values[5] = new GridEdge(position + new int3(1, 1, 1), gridSize, gridNoise);
        values[6] = new GridEdge(position + new int3(1, 1, 0), gridSize, gridNoise);
        values[7] = new GridEdge(position + new int3(0, 1, 0), gridSize, gridNoise);

        return values;
    }

    public static int DetermineIndex(in float isoLevel, in NativeArray<GridEdge> gridEdges)
    {
        var cubeIndex = 0;

        for(int i = 0, j = 1; i < gridEdges.Length; i++, j += j)
            if(gridEdges[i].val < isoLevel) cubeIndex |= j;

        return cubeIndex;
    }

    public static NativeArray<Vertex> FindIntersectVertices(in int cubeIndex, in float isoLevel, in NativeArray<GridEdge> gridEdges, Allocator allocator)
    {
        var vertices = new NativeArray<Vertex>(12, allocator, NativeArrayOptions.UninitializedMemory);

        for(int i = 0, j = 1; i < 12; i++, j += j)
        {
            if((Constants.EdgeTable[cubeIndex] & j) != 0)
            {
                var indices = Constants.EdgeInterpTable[i];
                vertices[i] = new Vertex
                {
                    pos = EdgesInterp(isoLevel, gridEdges[indices.x], gridEdges[indices.y])
                };
            }
        }

        return vertices;
    }

    public static int GetVertexAmount(in int cubeIndex)
    {
        int vAmount = 0;

        for(var i = 0; Constants.TriTable[cubeIndex * 16 + i] != -1; i += 3)
            vAmount++;
        
        return vAmount;
    }

    public static float3 EdgesInterp(float isolevel, GridEdge a, GridEdge b)
    {
        if(math.abs(isolevel - a.val) < 0.00001f)
            return a.pos;
        if(math.abs(isolevel - b.val) < 0.00001f)
            return b.pos;
        if(math.abs(a.val - b.val) < 0.00001f)
            return a.pos;
        
        var mu = (isolevel - a.val) / (b.val - a.val);
        return a.pos + mu * (b.pos - a.pos);
    }

    private void AddVertices(in int cubeIndex, in NativeArray<Vertex> vertices, ref NativeList<Vertex>.ParallelWriter v)
    {
        var _vertices = new NativeList<Vertex>(Constants.MAX_VERTEX_PER_BLOCK, Allocator.Temp);
        var center = (float3)GridSize / 2f;

        for(var i = 0; Constants.TriTable[cubeIndex * 16 + i] != -1; i += 3)
        {
            var vert = new NativeArray<Vertex>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var v0 = new Vertex { pos = vertices[Constants.TriTable[cubeIndex * 16 + i    ]].pos - center };
            var v1 = new Vertex { pos = vertices[Constants.TriTable[cubeIndex * 16 + i + 1]].pos - center };
            var v2 = new Vertex { pos = vertices[Constants.TriTable[cubeIndex * 16 + i + 2]].pos - center };

            _vertices.AddNoResize(v0);
            _vertices.AddNoResize(v1);
            _vertices.AddNoResize(v2);
        }

        v.AddRangeNoResize(_vertices);
    }

    /// <summary>
    /// Create and schedule new MarchingJob.
    /// </summary>
    /// <param name="isoLevel">The isolevel</param>
    /// <param name="gridSize">The size of the grid</param>
    /// <param name="gridNoise">The noise data.</param>
    /// <param name="v">The vertices generated</param>
    /// <param name="inputDeps">Job dependency</param>
    /// <returns></returns>
    public static JobHandle CreateAndSchedule(in ScriptableGrid gridSettings, in NativeArray<float4> gridNoise, out NativeList<Vertex> v, JobHandle inputDeps = default)
    {
        if(gridSettings.PointAmount != gridNoise.Length)
            throw new System.Exception($"The length of the grid ({gridSettings.PointAmount}) not equal to the length of {nameof(gridNoise)} ({gridNoise.Length}) !");

        var gridBlockAmount = (gridSettings.size.x - 1) * (gridSettings.size.y - 1) * (gridSettings.size.z - 1);
        
        v = new NativeList<Vertex>(Constants.MAX_VERTEX_PER_BLOCK * gridBlockAmount, Allocator.TempJob);

        return new MarchingJob
        {
            Isolevel    = gridSettings.isoLevel,
            GridSize    = gridSettings.size,
            GridNoise   = gridNoise,

            Vertices    = v.AsParallelWriter(),
        }
        .Schedule(gridSettings.PointAmount, 1, inputDeps);
    }
}
