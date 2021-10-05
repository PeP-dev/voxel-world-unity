using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkWatcher : MonoBehaviour
{
    private WorldGeneratorScript world;
    private Vector2Int pos;
    public void addWorld(WorldGeneratorScript world)
    {
        this.world = world;
    }
    public void addPosition(Vector2Int pos)
    {
        this.pos = pos;
    }

    void Update()
    {
        if (!world.WithinPlayersRange(pos))
        {
            world._gameObjects.Remove(pos);
            world._chunks.Remove(pos);
            world._meshs.Remove(pos);
            Destroy(this.gameObject);
        }
    }
}
