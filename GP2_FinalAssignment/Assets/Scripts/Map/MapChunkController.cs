using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapChunkController : MonoBehaviour
{
    public Vector2Int ChunkIndex { get; private set; }
    public Vector3 CentrePosition { get; private set; }
    private bool isActive = false;
    public void Init(Vector2Int chunkIndex, Vector3 centrePosition)
    {
        ChunkIndex = chunkIndex;
        CentrePosition = centrePosition;
    }

    public void SetActive(bool active)
    {
        if (isActive != active)
        {
            isActive = active;
            gameObject.SetActive(isActive);

            // TODO:基于对象池去生成所有的地图对象，花草树木之类的
        }

    }

}
