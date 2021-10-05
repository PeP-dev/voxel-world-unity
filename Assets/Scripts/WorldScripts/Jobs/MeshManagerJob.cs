using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class MeshManagerJob
{
    public MeshManagerJob()
    {
        _jobs = new Dictionary<Vector2Int, JobHolder>();
    }
    public Dictionary<Vector2Int, JobHolder> _jobs { get; }


    public int nbJobs
    {
        get { return _jobs.Count; }
    }


    private int jobsCompleted;
    public float averageTime;
    private float totalTime;


    public struct JobHolder
    {
        internal MeshGeneratorJob job;
        internal JobHandle handle;
        internal float startTime;
    }
    public MeshData GenerateMeshAt(Vector2Int position, Block[] blocks)
    {
        if (!_jobs.ContainsKey(position))
        {
                NativeArray<Block> native = new NativeArray<Block>(blocks.Length, Allocator.Persistent);
                for (int i = 0; i < blocks.Length; i++)
                {
                    native[i] = blocks[i];
                }
                MeshGeneratorJob job = new MeshGeneratorJob
                {
                    vertices = new NativeList<Vector3>(Allocator.Persistent),
                    triangles = new NativeList<int>(Allocator.Persistent),
                    colors = new NativeList<Color32>(Allocator.Persistent),
                    blocks = native
                };
                JobHandle handle = job.Schedule();
                if (handle.IsCompleted)
                {
                    handle.Complete();
                    _jobs.Remove(position);
                    MeshData m = new MeshData(job.vertices.ToArray(), job.triangles.ToArray(), job.colors.ToArray());
                    job.vertices.Dispose();
                    job.triangles.Dispose();
                    job.colors.Dispose();
                    job.blocks.Dispose();
                    return m;
                }
                _jobs.Add(position, new JobHolder
                {
                    job = job,
                    handle = handle,
                    startTime = Time.time,
                });
        }
        else
        {
            JobHolder holder = _jobs[position];
            if (holder.handle.IsCompleted)
            {
                holder.handle.Complete();
                jobsCompleted++;
                totalTime += Time.time - holder.startTime;
                averageTime = totalTime / jobsCompleted;
                _jobs.Remove(position);
                MeshData m = new MeshData(holder.job.vertices.ToArray(), holder.job.triangles.ToArray(), holder.job.colors.ToArray());
                holder.job.vertices.Dispose();
                holder.job.triangles.Dispose();
                holder.job.colors.Dispose();
                holder.job.blocks.Dispose();
                return m;
            }
        }
        return null;
    }

}
public struct MeshGeneratorJob : IJob
{

    public NativeList<Vector3> vertices;
    public NativeList<int> triangles;
    public NativeList<Color32> colors;
    public NativeArray<Block> blocks;

    public void Execute()
    {
        GreedyMesh();
    }

    public void AddSquareFace(Vector3[] vertices, Color32 color, bool isBackFace)
    {
        if (vertices.Length != 4)
        {
            throw new ArgumentException("A square face requires 4 vertices");
        }
        // Add the 4 vertices, and color for each vertex.
        for (int i = 0; i < vertices.Length; i++)
        {
            this.vertices.Add(vertices[i]);
            colors.Add(color);
        }

        if (isBackFace)
        {
            triangles.Add(this.vertices.Length - 4);
            triangles.Add(this.vertices.Length - 3);
            triangles.Add(this.vertices.Length - 2);

            triangles.Add(this.vertices.Length - 4);
            triangles.Add(this.vertices.Length - 2);
            triangles.Add(this.vertices.Length - 1);
        }
        else
        {
            triangles.Add(this.vertices.Length - 2);
            triangles.Add(this.vertices.Length - 3);
            triangles.Add(this.vertices.Length - 4);

            triangles.Add(this.vertices.Length - 1);
            triangles.Add(this.vertices.Length - 2);
            triangles.Add(this.vertices.Length - 4);
        }
    }

    public bool CompareStep(Vector3Int a, Vector3Int b, int direction, bool backFace)
    {
        Block blockA = GetBlock(a);
        Block blockB = GetBlock(b);

        return blockA == blockB && IsBlockFaceVisible(b, direction, backFace);
    }
    public bool IsBlockFaceVisible(Vector3Int blockPosition, int axis, bool backFace)
    {
        blockPosition[axis] += backFace ? -1 : 1;
        return !GetBlock(blockPosition).IsSolid();
    }

    private bool ContainsIndex(Vector3Int index) =>
        index.x >= 0 && index.x < Chunk.Dimensions.x &&
        index.y >= 0 && index.y < Chunk.Dimensions.y &&
        index.z >= 0 && index.z < Chunk.Dimensions.z;

    public Block GetBlock(Vector3Int index)
    {
        if (!ContainsIndex(index))
        {
            return Block.Air;
        }

        return blocks[Chunk.FlattenIndex(index)];
    }
    public Block GetBlock(int x, int y, int z)
    {
        Vector3Int index = new Vector3Int(x, y, z);
        if (!ContainsIndex(index))
        {
            return Block.Air;
        }

        return blocks[Chunk.FlattenIndex(index)];
    }
    
    public void GreedyMesh()
    {
        bool[,] merged;

        Vector3Int startPos, currPos, quadSize, m, n, offsetPos;
        Vector3[] vertices;

        Block startBlock;
        int direction, workAxis1, workAxis2;

        // Iterate over each face of the blocks.
        for (int face = 0; face < 6; face++)
        {
            bool isBackFace = face > 2;
            direction = face % 3;
            workAxis1 = (direction + 1) % 3;
            workAxis2 = (direction + 2) % 3;

            startPos = new Vector3Int();
            currPos = new Vector3Int();

            // Iterate over the chunk layer by layer.
            for (startPos[direction] = 0; startPos[direction] < Chunk.Dimensions[direction]; startPos[direction]++)
            {
                merged = new bool[Chunk.Dimensions[workAxis1], Chunk.Dimensions[workAxis2]];

                // Build the slices of the mesh.
                for (startPos[workAxis1] = 0; startPos[workAxis1] < Chunk.Dimensions[workAxis1]; startPos[workAxis1]++)
                {
                    for (startPos[workAxis2] = 0; startPos[workAxis2] < Chunk.Dimensions[workAxis2]; startPos[workAxis2]++)
                    {
                        startBlock = GetBlock(startPos);

                        // If this block has already been merged, is air, or not visible skip it.
                        if (merged[startPos[workAxis1], startPos[workAxis2]] || !startBlock.IsSolid() || !IsBlockFaceVisible(startPos, direction, isBackFace))
                        {
                            continue;
                        }

                        // Reset the work var
                        quadSize = new Vector3Int();

                        // Figure out the width, then save it
                        for (currPos = startPos, currPos[workAxis2]++; currPos[workAxis2] < Chunk.Dimensions[workAxis2] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1], currPos[workAxis2]]; currPos[workAxis2]++) { }
                        quadSize[workAxis2] = currPos[workAxis2] - startPos[workAxis2];

                        // Figure out the height, then save it
                        for (currPos = startPos, currPos[workAxis1]++; currPos[workAxis1] < Chunk.Dimensions[workAxis1] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1], currPos[workAxis2]]; currPos[workAxis1]++)
                        {
                            for (currPos[workAxis2] = startPos[workAxis2]; currPos[workAxis2] < Chunk.Dimensions[workAxis2] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1], currPos[workAxis2]]; currPos[workAxis2]++) { }

                            // If we didn't reach the end then its not a good add.
                            if (currPos[workAxis2] - startPos[workAxis2] < quadSize[workAxis2])
                            {
                                break;
                            }
                            else
                            {
                                currPos[workAxis2] = startPos[workAxis2];
                            }
                        }
                        quadSize[workAxis1] = currPos[workAxis1] - startPos[workAxis1];

                        // Now we add the quad to the mesh
                        m = new Vector3Int();
                        m[workAxis1] = quadSize[workAxis1];

                        n = new Vector3Int();
                        n[workAxis2] = quadSize[workAxis2];

                        // We need to add a slight offset when working with front faces.
                        offsetPos = startPos;
                        offsetPos[direction] += isBackFace ? 0 : 1;

                        //Draw the face to the mesh
                        vertices = new Vector3[] {
                            offsetPos,
                            offsetPos + m,
                            offsetPos + m + n,
                            offsetPos + n
                        };
                        AddSquareFace(vertices, startBlock.getColor(), isBackFace);

                        // Mark it merged
                        for (int f = 0; f < quadSize[workAxis1]; f++)
                        {
                            for (int g = 0; g < quadSize[workAxis2]; g++)
                            {
                                merged[startPos[workAxis1] + f, startPos[workAxis2] + g] = true;
                            }
                        }
                    }
                }
            }
        }
    }
}