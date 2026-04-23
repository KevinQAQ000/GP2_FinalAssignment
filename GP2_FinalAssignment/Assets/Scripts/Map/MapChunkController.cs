using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 数据类保持不变...
[System.Serializable]
public class MapChunkMapObjectModel { public GameObject Prefab; public Vector3 Position; }
public class MapChunkData { public List<MapChunkMapObjectModel> MapObjectList = new List<MapChunkMapObjectModel>(); }

public class MapChunkController : MonoBehaviour
{
    public Vector2Int ChunkIndex { get; private set; }
    public Vector3 CentrePosition { get; private set; }

    private MapChunkData mapChunkData;
    private List<GameObject> mapObjectList = new List<GameObject>();
    private bool isActive = false;

    // 核心修复：退出保护
    private static bool isApplicationQuitting = false;

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    public void Init(Vector2Int chunkIndex, Vector3 centrePosition, List<MapChunkMapObjectModel> MapObjectList)
    {
        ChunkIndex = chunkIndex;
        CentrePosition = centrePosition;
        mapChunkData = new MapChunkData();
        mapChunkData.MapObjectList = MapObjectList;
        if (mapObjectList == null) mapObjectList = new List<GameObject>();
        mapObjectList.Clear();
    }

    private void OnDestroy()
    {
        // 如果是退出游戏，直接跳过回收逻辑，防止报错
        if (isApplicationQuitting) return;

        if (isActive)
        {
            ClearObjects();
        }
    }

    private void ClearObjects()
    {
        // 增加安全检查：确保对象池单例还活着
        if (mapObjectList == null || PoolManager.Instance == null) return;

        for (int i = 0; i < mapObjectList.Count; i++)
        {
            if (mapObjectList[i] != null)
            {
                PoolManager.Instance.PushGameObject(mapObjectList[i]);
            }
        }
        mapObjectList.Clear();
    }

    public void SetActive(bool active)
    {
        if (isActive != active)
        {
            isActive = active;
            gameObject.SetActive(isActive);

            if (mapChunkData == null) return;
            List<MapChunkMapObjectModel> dataList = mapChunkData.MapObjectList;

            if (isActive)
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    GameObject go = PoolManager.Instance.GetGameObject(dataList[i].Prefab, transform);
                    if (go != null)
                    {
                        go.transform.position = dataList[i].Position;
                        mapObjectList.Add(go);
                    }
                }
            }
            else
            {
                ClearObjects();
            }
        }
    }
}