using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class GameResources : MonoBehaviour, ISerializationCallbackReceiver
{
    public static ComputeShader ComputeShader { get; private set; }
    public static int3 ChunkSize { get; private set; }

    public ComputeShader computeShader;
    public int3 chunkSize;

    public void OnAfterDeserialize()
    {
        ComputeShader = computeShader;
        ChunkSize = chunkSize;
    }

    public void OnBeforeSerialize()
    {
        computeShader = ComputeShader;
        chunkSize = ChunkSize;
    }
}
