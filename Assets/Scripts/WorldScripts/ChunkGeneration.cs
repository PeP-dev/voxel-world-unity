using UnityEngine;
using System;
using System.Collections.Generic;

public class Chunk
{
    public static readonly Vector3Int Dimensions = new Vector3Int(32,80,32);

    public Vector2Int ChunkPoint { get; }
    public Vector3Int Position { get; }
    public Block[] blocks { get; }

    public Chunk(Block[] blocks, Vector2Int chunkPoint)
    {
        Position = new Vector3Int(chunkPoint.x*Dimensions.x,  0, chunkPoint.y*Dimensions.z);
        ChunkPoint = chunkPoint;
        this.blocks = blocks;
    }

    public GameObject GenerateGameObject(MeshData data)
    {
        GameObject obj = new GameObject("Chunk " + ChunkPoint.x + " / " + ChunkPoint.y);
        obj.AddComponent<MeshFilter>().ApplyMeshData(data);
        obj.AddComponent<MeshCollider>().sharedMesh = obj.GetComponent<MeshFilter>().mesh;
        obj.AddComponent<MeshRenderer>();
        return obj;
    }
    public void SetBlock(Vector3Int index, Block block)
    {
        if (!ContainsIndex(index))
        {
            throw new IndexOutOfRangeException($"Chunk does not contain index: {index}");
        }

        blocks[FlattenIndex(index)] = block;
    }
    public bool CompareStep(Vector3Int a, Vector3Int b, int direction, bool backFace)
    {
        Block blockA = GetBlock(a);
        Block blockB = GetBlock(b);

        return blockA == blockB && blockB.IsSolid() && IsBlockFaceVisible(b, direction, backFace);
    }
    public bool IsBlockFaceVisible(Vector3Int blockPosition, int axis, bool backFace)
    {
        blockPosition[axis] += backFace ? -1 : 1;
        return !GetBlock(blockPosition).IsSolid();
    }
    public Block GetBlock(Vector3Int index)
    {
        if (!ContainsIndex(index))
        {
            return Block.Air;
        }

        return blocks[FlattenIndex(index)];
    }
    private bool ContainsIndex(Vector3Int index) =>
        index.x >= 0 && index.x < Dimensions.x &&
        index.y >= 0 && index.y < Dimensions.y &&
        index.z >= 0 && index.z < Dimensions.z;
    public static int FlattenIndex(Vector3Int index) =>
        (index.z * Dimensions.x * Dimensions.y) +
        (index.y * Dimensions.x) +
        index.x;
}

public class MeshData
{
    public Vector3[] Vertices { get; }
    public int[] Triangles { get; }
    public Color32[] Colors { get; }

    public MeshData(Vector3[] vertices, int[] triangles, Color32[] colors)
    {
        Vertices = vertices;
        Triangles = triangles;
        Colors = colors;
    }
}

public class MeshBuilder
{
    private readonly List<Vector3> vertices;
    private readonly List<int> triangles;
    private readonly List<Color32> colors;

    public MeshBuilder()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color32>();
    }


    public MeshData ToMeshData()
    {
        MeshData data = new MeshData(
            vertices.ToArray(),
            triangles.ToArray(),
            colors.ToArray()
        );

        vertices.Clear();
        triangles.Clear();
        colors.Clear();

        return data;
    }
}



public struct Block
{
    public byte A;
    public byte R;
    public byte G;
    public byte B;


    public Block(byte R, byte G, byte B, byte A)
    {
        this.A = A;
        this.B = B;
        this.R = R;
        this.G = G;
    }
    public static Block Air
    {
        get { return new Block(0, 0, 0, 0); }
    }
    public static Block Dirt
    {
        get { return new Block(160, 82, 45,255); }
    }
    public static Block Grass
    {
        get { return new Block(0, 100, 0, 255); }
    }
    public static Block Stone
    {
        get { return new Block(128, 128, 128,255); }
    }
    public static Block Cloud
    {
        get { return new Block(240,240,240,255); }
    }
    public bool IsSolid() => A == byte.MaxValue;
    public static bool operator ==(Block a, Block b)
    {
        return a.equals(b);
    }
    public static bool operator !=(Block a, Block b)
    {
        return !a.equals(b);
    }
    public bool equals(Block b)
    {
        return b != null && b.A == A && b.R == R && b.G == G && b.B == B;
    }
    public Color32 getColor() => new Color32(R, G, B, A);

}


public static class MeshFilterExts
{
    public static void ApplyMeshData(this MeshFilter meshFilter, MeshData meshData)
    {
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh.Clear();
        meshFilter.mesh.vertices = meshData.Vertices;
        meshFilter.mesh.triangles = meshData.Triangles;
        //Color mesh and calculate normals
        meshFilter.mesh.colors32 = meshData.Colors;
        meshFilter.mesh.RecalculateNormals();
    }
}