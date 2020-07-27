using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Chunk))]
public class ChunkTest : MonoBehaviour
{
    private Chunk chunk;

    private void Awake()
    {
        chunk = GetComponent<Chunk>();
    }

    private void Start()
    {
        chunk.InitBuffers();
        // chunk.InitializeGridData().Complete();
    }

    private void Update()
    {
        chunk.InitializeGridData().Complete();
        chunk.BuildMesh();
    }
}
