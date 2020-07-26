using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public PlanetDesc planet;

    private MeshCollider meshCollider;
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
    private CBufferTriangle[] trisBufferTempArray;

    private void Awake()
    {
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();

        mesh = new Mesh();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public void UpdateMeshCollider()
    {
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }
}
