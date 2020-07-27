using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class GameResources : MonoBehaviour, ISerializationCallbackReceiver
{
    public static ComputeShader ComputeShader { get; private set; }
    public static int3 ChunkSize { get; private set; }
    public static Material ChunkMaterial { get; private set; }
    
    public static int ChunkPointAmount => ChunkSize.Amount();
    public static int ChunkBlockAmount => (ChunkSize - 1).Amount();

    public ComputeShader computeShader;
    public int3 chunkSize;
    public Material chunkMaterial;

    public void OnAfterDeserialize()
    {
        ComputeShader = computeShader;
        ChunkSize = chunkSize;
        ChunkMaterial = chunkMaterial;
    }

    public void OnBeforeSerialize()
    {
        computeShader = ComputeShader;
        chunkSize = ChunkSize;
        chunkMaterial = ChunkMaterial;
    }
}
