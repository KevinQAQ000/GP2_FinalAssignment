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

            List<MapChunkMapObjectModel> ObjectList = mapChunkData.MapObjectList;
            // 从对象池中获取所有物体
            if (isActive)
            {
                for (int i = 0; i < ObjectList.Count; i++)
                {
                    GameObject go = PoolManager.Instance.GetGameObject(ObjectList[i].Prefab, transform);
                    go.transform.position = ObjectList[i].Position;
                    mapObjectList.Add(go);
                }
            }
            // 把所有物体放回对象池
            else
            {
                for (int i = 0; i < ObjectList.Count; i++)
                {
                    PoolManager.Instance.PushGameObject(mapObjectList[i]);
                }
                mapObjectList.Clear();
            }
        }

    }

}
