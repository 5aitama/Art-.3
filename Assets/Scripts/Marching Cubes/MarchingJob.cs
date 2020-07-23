using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

using System.Threading;

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

    public GridEdge(in int3 pos, in int3 gridSize, in NativeArray<float> gridNoise)
    {
        this.pos = pos;
        this.val = gridNoise[pos.To1D(gridSize)];
    }
}

[BurstCompile]
public struct MarchingJob : IJobParallelFor
{
    [ReadOnly]
    public int3 GridSize;

    [ReadOnly]
    public NativeArray<float> GridNoise;

    [ReadOnly]
    public float Isolevel;

    [WriteOnly]
    public NativeList<Vertex>.ParallelWriter Vertices;

    public int VertexCounter;

    public void Execute(int index)
    {
        var position = index.To3D(GridSize);

        if(position.x >= GridSize.x - 1 || position.y >= GridSize.y - 1 || position.z >= GridSize.z - 1)
            return;

        var gridEdges = GetGridValues(position, Allocator.Temp);
        var cubeIndex = DetermineIndex(gridEdges);

        if(Constants.EdgeTable[cubeIndex] == 0)
            return;
        
        var vertices = FindIntersectVertices(cubeIndex, gridEdges, Allocator.Temp);

        AddVertices(cubeIndex, vertices, ref Vertices);
    }

    private NativeArray<GridEdge> GetGridValues(int3 position, Allocator allocator)
    {
        var values = new NativeArray<GridEdge>(8, allocator, NativeArrayOptions.UninitializedMemory);

        values[0] = new GridEdge(position + new int3(0, 0, 1), GridSize, GridNoise);
        values[1] = new GridEdge(position + new int3(1, 0, 1), GridSize, GridNoise);
        values[2] = new GridEdge(position + new int3(1, 0, 0), GridSize, GridNoise);
        values[3] = new GridEdge(position + new int3(0, 0, 0), GridSize, GridNoise);

        values[4] = new GridEdge(position + new int3(0, 1, 1), GridSize, GridNoise);
        values[5] = new GridEdge(position + new int3(1, 1, 1), GridSize, GridNoise);
        values[6] = new GridEdge(position + new int3(1, 1, 0), GridSize, GridNoise);
        values[7] = new GridEdge(position + new int3(0, 1, 0), GridSize, GridNoise);

        return values;
    }

    private int DetermineIndex(in NativeArray<GridEdge> gridEdges)
    {
        var cubeIndex = 0;

        for(int i = 0, j = 1; i < gridEdges.Length; i++, j += j)
            if(gridEdges[i].val < Isolevel) cubeIndex |= j;

        return cubeIndex;
    }

    private NativeArray<Vertex> FindIntersectVertices(in int cubeIndex, in NativeArray<GridEdge> gridEdges, Allocator allocator)
    {
        var vertices = new NativeArray<Vertex>(12, allocator, NativeArrayOptions.UninitializedMemory);

        for(int i = 0, j = 1; i < 12; i++, j += j)
        {
            if((Constants.EdgeTable[cubeIndex] & j) != 0)
            {
                var indices = Constants.EdgeInterpTable[i];
                vertices[i] = new Vertex
                {
                    pos = EdgesInterp(Isolevel, gridEdges[indices.x], gridEdges[indices.y])
                };
            }
        }

        return vertices;
    }

    private float3 EdgesInterp(float isolevel, GridEdge a, GridEdge b)
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

        for(var i = 0; Constants.TriTable[cubeIndex * 16 + i] != -1; i += 3)
        {
            var vert = new NativeArray<Vertex>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var v0 = new Vertex { pos = vertices[Constants.TriTable[cubeIndex * 16 + i    ]].pos };
            var v1 = new Vertex { pos = vertices[Constants.TriTable[cubeIndex * 16 + i + 1]].pos };
            var v2 = new Vertex { pos = vertices[Constants.TriTable[cubeIndex * 16 + i + 2]].pos };

            _vertices.AddNoResize(v0);
            _vertices.AddNoResize(v1);
            _vertices.AddNoResize(v2);
        }

        v.AddRangeNoResize(_vertices);
    }

    public static JobHandle Create(in float isoLevel, in int3 gridSize, in NativeArray<float> gridNoise, out NativeList<Vertex> v, JobHandle inputDeps = default)
    {
        var gridBlockAmount = (gridSize.x - 1) * (gridSize.y - 1) * (gridSize.z - 1);
        
        v = new NativeList<Vertex>(Constants.MAX_VERTEX_PER_BLOCK * gridBlockAmount, Allocator.TempJob);

        return new MarchingJob
        {
            Isolevel    = isoLevel,
            GridSize    = gridSize,
            GridNoise   = gridNoise,

            Vertices    = v.AsParallelWriter(),

            VertexCounter = 0,
        }
        .Schedule(gridSize.x * gridSize.y * gridSize.z, 1, inputDeps);
    }
}
