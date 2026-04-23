using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 地图块数据
/// </summary>
public class MapChunkData
{
    public List<MapChunkMapObjectModel> MapObjectList = new List<MapChunkMapObjectModel>();
}
public class MapChunkMapObjectModel
{
    public GameObject Prefab;//预制体
    public Vector3 Position;//位置
}



public class MapChunkController : MonoBehaviour
{
    public Vector2Int ChunkIndex { get; private set; }
    public Vector3 CentrePosition { get; private set; }

    private MapChunkData mapChunkData;
    private List<GameObject> mapObjectList;

    private bool isActive = false;
    public void Init(Vector2Int chunkIndex, Vector3 centrePosition, List<MapChunkMapObjectModel> MapObjectList)
    {
        ChunkIndex = chunkIndex;
        CentrePosition = centrePosition;
        mapChunkData = new MapChunkData();
        mapChunkData.MapObjectList = MapObjectList;
        mapObjectList = new List<GameObject>(MapObjectList.Count);
    }

    public void SetActive(bool active)
    {
        if (isActive != active)
        {
            isActive = active;
            gameObject.SetActive(isActive);

            // 提前获取数据列表
            List<MapChunkMapObjectModel> ObjectList = mapChunkData.MapObjectList;

            if (isActive)
            {
                // 激活时：确保列表是空的，防止重复添加导致后面回收出错
                mapObjectList.Clear();

                for (int i = 0; i < ObjectList.Count; i++)
                {
                    GameObject go = PoolManager.Instance.GetGameObject(ObjectList[i].Prefab, transform);
                    if (go != null) // 安全检查
                    {
                        go.transform.position = ObjectList[i].Position;
                        mapObjectList.Add(go);
                    }
                }
            }
            else
            {
                // 【核心修复】：回收时，直接遍历实际存放物体的列表
                // 使用 mapObjectList.Count 替代 ObjectList.Count
                for (int i = 0; i < mapObjectList.Count; i++)
                {
                    // 增加非空检查，防止物体在运行中被意外销毁
                    if (mapObjectList[i] != null)
                    {
                        PoolManager.Instance.PushGameObject(mapObjectList[i]);
                    }
                }
                // 全部回收后清空列表
                mapObjectList.Clear();
            }
        }
    }

}
