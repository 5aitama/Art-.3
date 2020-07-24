using Unity.Entities;
using UnityEngine;

public class TestSystem : ComponentSystem
{
    private ChunkManagerSystem m_ChunkManagerSystem;

    protected override void OnCreate()
    {
        m_ChunkManagerSystem = World.GetOrCreateSystem<ChunkManagerSystem>();
    }

    protected override void OnUpdate()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            m_ChunkManagerSystem.Create(0f);
    }
}
