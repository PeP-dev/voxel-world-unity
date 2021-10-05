using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class GenerationJobManager
{
    private float seed;
    public Dictionary<Vector2Int, JobHolder> _jobs { get; }
    //Constructor with random seed
    public GenerationJobManager()
    {
        this.seed = UnityEngine.Random.Range(0f, 50000f);
        this._jobs = new Dictionary<Vector2Int, JobHolder>();
    }
    //Constructor with set seed
    public GenerationJobManager( float seed)
    {
        this.seed = seed;
        _jobs = new Dictionary<Vector2Int, JobHolder>();
    }

    //Getter for the number of jobs
    public int nbJobs
    {
        get { return _jobs.Count; }
    }
    private int jobsCompleted;
    public float averageTime;
    private float totalTime;
    //Useful struct for managing a single Job and its state.
    public struct JobHolder
    {
        internal ChunkGeneratorJob job;
        internal JobHandle handle;
        internal float startTime;
    }
    /*
        Main method for loading a chunk
        If it is created or under the process of creation, check its state
        Else, start the process of creation
        @param position the position from which to load the chunk
        @returns the created Chunk if it is ready, else null 
    */
    public Chunk LoadChunkAt(Vector2Int position)
    {

        if (!_jobs.ContainsKey(position))
        {
                ChunkGeneratorJob job = new ChunkGeneratorJob
                {
                    Position = new Vector2Int(position.x * Chunk.Dimensions.x, position.y * Chunk.Dimensions.z),
                    chunk = new NativeArray<Block>(Chunk.Dimensions.x * Chunk.Dimensions.y * Chunk.Dimensions.z, Allocator.Persistent),
                    seed = seed,
                };
                JobHandle handle = job.Schedule();

                if (handle.IsCompleted)
                {
                    handle.Complete();
                    _jobs.Remove(position);
                    Chunk c = new Chunk(job.chunk.ToArray(), position);
                    job.chunk.Dispose();
                    return c;
                }
                _jobs.Add(position, new JobHolder
                {
                    job = job,
                    handle = handle,
                    startTime = Time.time
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
                Chunk c = new Chunk(holder.job.chunk.ToArray(), position);
                holder.job.chunk.Dispose();
                return c;
            }
        }
        return null;
    }
}




/*
    Main structure for handling the multithreaded generation of a Chunk.
    Uses Unity Job System
*/
public struct ChunkGeneratorJob : IJob
{
    public Vector2Int Position;
    public NativeArray<Block> chunk;
    public float seed;
    public void Execute()
    {
        Generation3d(heightMapGeneration(seed, 0.003f));
    }
    //Combining of multiple perlin layers for a more realistic landscape
    private float combinePerlin(float x, float y, float scaleFactor, int count, float seed)
    {
        float maxVal = 0f;
        float currFactor = 1f;
        float currVal = 0f;
        for (int i = 0; i < count; i++)
        {
            currVal += currFactor * Mathf.PerlinNoise(x / currFactor + seed, y / currFactor + seed);
            maxVal += currFactor;
            currFactor *= scaleFactor;
        }
        return currVal / maxVal;
    }
    //3d perlin noise generation
    private float Perlin3d(float x, float y, float z, float scale, float seed)
    {
        x = (x + Position.x + seed) * scale;
        y = (y + seed) * scale;
        z = (z + Position.y + seed) * scale;

        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);

        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);

        return (ab + bc + ac + ba + cb + ca) / 6f;
    }


    private Dictionary<Vector2Int, int> heightMapGeneration(float seed, float scale)
    {
        Dictionary<Vector2Int, int> map = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < Chunk.Dimensions.x * Chunk.Dimensions.z; i++)
        {
            int x = i % Chunk.Dimensions.x;
            int z = (i / Chunk.Dimensions.x) % Chunk.Dimensions.z;
            int height = Mathf.FloorToInt(combinePerlin(((float)x + Position.x) * scale, ((float)z + Position.y) * scale, .5f, 5, seed) * Chunk.Dimensions.y);
            map.Add(new Vector2Int(x, z), height);
        }
        return map;
    }
    private void Generation3d(Dictionary<Vector2Int, int> map)
    {
        SimplexNoiseGenerator noise = new SimplexNoiseGenerator(WorldGeneratorScript.noise.GetSeed());
        for (int x = 0; x < Chunk.Dimensions.x; x++)
        {
            for (int z = 0; z < Chunk.Dimensions.z; z++)
            {
                for (int y = 0; y < map[new Vector2Int(x, z)]; y++)
                {
                    if (noise.coherentNoise(x + Position.x, y, z + Position.y) > -.05f)
                    {
                        chunk[Chunk.FlattenIndex(new Vector3Int(x, y, z))] = Block.Grass;
                    }
                    else
                    {
                        chunk[Chunk.FlattenIndex(new Vector3Int(x, y, z))] = Block.Air;
                    }
                }
            }
        }
    }
}