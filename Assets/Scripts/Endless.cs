using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Saitama.Mathematics;
using System.Linq;
using Unity.Jobs;

public class Endless : MonoBehaviour
{
    public int3 viewRange = 3;

    public PlanetDesc planet;

    private Dictionary<int3, Chunk> chunksA = new Dictionary<int3, Chunk>();
    private Dictionary<int3, Chunk> chunksB = new Dictionary<int3, Chunk>();

    private Queue<Chunk> chunksUnused = new Queue<Chunk>();
    private Queue<Chunk> chunksPending = new Queue<Chunk>();

    private void Start()
    {
        var position = (float3)transform.position;

        var cameraPos = (int3)math.round(position / (GameResources.ChunkSize - 1)) * (GameResources.ChunkSize - 1);

        for(var i = 0; i < viewRange.Amount(); i++)
        {
            var chunkPos = (i.To3D(viewRange) - viewRange / 2) * (GameResources.ChunkSize - 1) + cameraPos;
            var chunk = CreateChunk(chunkPos, false);
            chunksA.Add(chunkPos, chunk);
        }
    }

    private void Update()
    {

        JobHandle handle = default;

        var chunksArray = chunksPending.ToArray();
        chunksPending.Clear();

        for(var i = 0; i < chunksArray.Length; i++)
        {
            handle = chunksArray[i].InitializeGridData(handle);
        }

        handle.Complete();

        for(var i = 0; i < chunksArray.Length; i++)
            chunksArray[i].BuildMesh();

        for(var i = 0; i < chunksArray.Length; i++)
            chunksArray[i].UpdateMeshCollider();
        
        var amount = viewRange.Amount();
        var position = (float3)transform.position;

        var cameraPos = (int3)math.round(position / (GameResources.ChunkSize - 1)) * (GameResources.ChunkSize - 1);

        for(var i = 0; i < amount; i++)
        {
            var chunkPos = (i.To3D(viewRange) - viewRange / 2) * (GameResources.ChunkSize - 1) + cameraPos;

            if(chunksA.ContainsKey(chunkPos))
            {
                // Move it to chunksB
                chunksB.Add(chunkPos, chunksA[chunkPos]);
                chunksA.Remove(chunkPos);
            } 
            else
            {
                // Create new chunk
                var chunk = CreateChunk(chunkPos);
                chunksB.Add(chunkPos, chunk);
            }
        }

        // Recycle all chunks in chunksA
        var chunksAKeys = chunksA.Keys.ToArray();

        for(var i = 0; i < chunksAKeys.Length; i++)
            RecycleChunk(chunksA[chunksAKeys[i]]);

        chunksA.Clear();

        // Swap chunksA and chunksB
        var chunksBKeys = chunksB.Keys.ToArray();
        var chunksBValues = chunksB.Values.ToArray();

        for(var i = 0; i < chunksBKeys.Length; i++)
        {
            chunksA.Add(chunksBKeys[i], chunksBValues[i]);
            chunksB.Remove(chunksBKeys[i]);
        }
    }

    private void RecycleChunk(Chunk chunk)
    {
        chunk.Clean();
        chunksUnused.Enqueue(chunk);
    }

    private Chunk CreateChunk(float3 at, bool useRecycleChunk = true)
    {
        if(!useRecycleChunk || (useRecycleChunk && chunksUnused.Count == 0))
        {
            GameObject o = new GameObject($"Chunk [{at.x}, {at.y}, {at.z}]");
            o.transform.position = at;

            var chunk = o.AddComponent<Chunk>();

            chunk.planet = planet;
            chunk.InitBuffers();

            chunksPending.Enqueue(chunk);
            return chunk;
        }
        else
        {
            var chunk = chunksUnused.Dequeue();
            chunk.transform.position = at;
            chunksPending.Enqueue(chunk); 
            return chunk;
        }

        // chunk.InitializeGridData().Complete();
        // chunk.BuildMesh();
        // chunk.UpdateMeshCollider();

        // return chunk;
    }
}
