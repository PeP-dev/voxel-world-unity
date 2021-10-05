using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
public class WorldGeneratorScript : MonoBehaviour
{
    private Block[] world;
    public int viewDistance = 5;
    public Dictionary<Vector2Int, Chunk> _chunks { get; private set; }
    public Dictionary<Vector2Int, MeshData> _meshs { get; private set; }
    public Dictionary<Vector2Int, GameObject> _gameObjects { get; private set; }
    public int queuedMeshs;
    public int queuedChunks;
    public float averageTimeMesh;
    public float averageTimeChunks;


    private GenerationJobManager generationJob;
    private GameObject[] players;
    private MeshManagerJob meshManagerJob;
    public Transform worldTransform { get; private set; }
    public Material worldMaterial { get; private set; }
    public Material material;
    public static SimplexNoiseGenerator noise = new SimplexNoiseGenerator();

    private void Start()
    {
        noise = new SimplexNoiseGenerator();
        JobsUtility.JobWorkerCount = 3;
        players = GameObject.FindGameObjectsWithTag("Player");
        _gameObjects = new Dictionary<Vector2Int, GameObject>();
        meshManagerJob = new MeshManagerJob();
        generationJob = new GenerationJobManager();
        _chunks = new Dictionary<Vector2Int, Chunk>();
        _meshs = new Dictionary<Vector2Int, MeshData>();
        worldTransform = GetComponent<Transform>();
    }
    private void OnDestroy()
    {
        foreach(var holder in generationJob._jobs.Values)
        {
            holder.handle.Complete();
            holder.job.chunk.Dispose();
        }
        foreach (var holder in meshManagerJob._jobs.Values)
        {
            holder.handle.Complete();
            holder.job.vertices.Dispose();
            holder.job.triangles.Dispose();
            holder.job.colors.Dispose();
            holder.job.blocks.Dispose();
        }
    }
    private void Update()
    {
        queuedChunks = generationJob._jobs.Count;
        queuedMeshs = meshManagerJob._jobs.Count;
        averageTimeChunks = generationJob.averageTime;
        averageTimeMesh = meshManagerJob.averageTime;
        foreach (GameObject player in players)
        {
            Vector3 playerPos = player.transform.position;
            int chunkPosX = Mathf.FloorToInt(playerPos.x / Chunk.Dimensions.x);
            int chunkPosY = Mathf.FloorToInt(playerPos.z / Chunk.Dimensions.z);
            for (int i =  -viewDistance; i <= viewDistance; i++)
            {
                int val = viewDistance - Mathf.Abs(i);
                for (int j = -val; j <= val ; j++)
                {
                    Vector2Int pos = new Vector2Int(i + chunkPosX, j + chunkPosY);
                    if (!_chunks.ContainsKey(pos))
                    {
                        Chunk c = generationJob.LoadChunkAt(pos);
                        if (c != null)
                        {
                            _chunks.Add(pos, c);
                        }
                    }
                    else
                    {
                        if (!_meshs.ContainsKey(pos))
                        {
                            MeshData m = meshManagerJob.GenerateMeshAt(pos, _chunks[pos].blocks);
                            if (m != null)
                            {

                                _meshs.Add(pos, m);
                                GameObject go = _chunks[pos].GenerateGameObject(m);
                                go.layer = gameObject.layer;
                                go.transform.parent = transform;
                                go.GetComponent<MeshRenderer>().material = material;
                                go.AddComponent<ChunkWatcher>();
                                go.GetComponent<ChunkWatcher>().addWorld(this);
                                go.GetComponent<ChunkWatcher>().addPosition(pos);
                                go.transform.position = worldTransform.position + new Vector3(pos.x * Chunk.Dimensions.x, 0, pos.y * Chunk.Dimensions.z);
                                _gameObjects.Add(pos, go);
                            }
                        }
                    }
                }
            }
        }
    }
    public bool WithinPlayersRange(Vector2Int position)
    {
        Vector2Int playerPos = new Vector2Int();
        foreach (GameObject player in players)
        {
            playerPos = new Vector2Int(Mathf.FloorToInt(player.transform.position.x / Chunk.Dimensions.x), Mathf.FloorToInt(player.transform.position.z / Chunk.Dimensions.z));
            if (Mathf.Abs(playerPos.x - position.x) + Mathf.Abs(playerPos.y - position.y) <= viewDistance)
            {
                return true;
            }
        }
        return false;
    }
}
