using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    // 地图尺寸
    public int mapSize;        // 一行或者一列有多少个地图块
    public int mapChunkSize;   // 一个地图块有多少个格子
    public float cellSize;     // 一个格子多少米

    // 地图的随机参数
    public float noiseLacunarity;  // 噪音间隙
    public int mapSeed;            // 地图种子
    public int spawnSeed;          // 随时地图对象的种子
    public float marshLimit;       // 沼泽的边界

    // 地图的美术资源
    public Material mapMaterial;
    public Texture2D forestTexutre;
    public Texture2D[] marshTextures;
    public MapConfig mapConfig;   //地图配置

    private MapGenerator mapGenerator;
    public int viewDinstance;       // 玩家可视距离，单位是Chunk
    public Transform viewer;        // 观察者
    private Vector3 lastViewerPos = Vector3.one * -1;
    public Dictionary<Vector2Int, MapChunkController> mapChunkDic;  // 全部已有的地图块

    public float updateChunkTime = 1f;
    private bool canUpdateChunk = true;
    private float chunkSizeOnWord;  // 在世界中实际的地图块尺寸 单位米
    private List<MapChunkController> lastVisibleChunkList = new List<MapChunkController>();


    void Start()
    {
        // 初始化地图生成器
        mapGenerator = new MapGenerator(mapSize, mapChunkSize, cellSize,noiseLacunarity,mapSeed,spawnSeed,marshLimit,mapMaterial,forestTexutre,marshTextures,mapConfig) ;
        mapGenerator.GenerateMapData();
        mapChunkDic = new Dictionary<Vector2Int, MapChunkController>();
        chunkSizeOnWord = mapChunkSize * cellSize;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVisibleChunk();
    }

    // 根据观察者的位置来刷新那些地图块可以看到
    private void UpdateVisibleChunk()
    {
        // 如果观察者没有移动过，不需要刷新
        if (viewer.position == lastViewerPos) return;
        // 如果时间没到 不允许更新
        if (canUpdateChunk == false) return;

        // 当前观察者所在的地图快，
        Vector2Int currChunkIndex = GetMapChunkIndexByWorldPosition(viewer.position);

        // 关闭全部不需要显示的地图块
        for (int i = lastVisibleChunkList.Count-1; i >= 0; i--)
        {
            Vector2Int chunkIndex = lastVisibleChunkList[i].ChunkIndex;
            if (Mathf.Abs(chunkIndex.x - currChunkIndex.x)>viewDinstance
                || Mathf.Abs(chunkIndex.y - currChunkIndex.y)>viewDinstance)
            {
                lastVisibleChunkList[i].SetActive(false);
                lastVisibleChunkList.RemoveAt(i);
            }
        }

        int startX = currChunkIndex.x - viewDinstance;
        int startY = currChunkIndex.y - viewDinstance;
        // 开启需要显示的地图块
        for (int x = 0; x < 2*viewDinstance+1; x++)
        {
            for (int y = 0; y < 2 * viewDinstance + 1; y++)
            {
                canUpdateChunk = false;
                Invoke("RestCanUpdateChunkFlag", updateChunkTime);
                Vector2Int chunkIndex = new Vector2Int(startX + x, startY + y);
                // 之前加载过
                if (mapChunkDic.TryGetValue(chunkIndex,out MapChunkController chunk))
                {
                    // 这个地图是不是已经在显示列表
                    if (lastVisibleChunkList.Contains(chunk) == false)
                    {
                        lastVisibleChunkList.Add(chunk);
                        chunk.SetActive(true);
                    }
                }
                // 之前没有加载
                else
                {
                    chunk = GenerateMapChunk(chunkIndex);
                    if (chunk!=null)
                    {
                        chunk.SetActive(true);
                        lastVisibleChunkList.Add(chunk);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 根据世界坐标获取地图块的索引
    /// </summary>
    private Vector2Int GetMapChunkIndexByWorldPosition(Vector3 worldPostion)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(worldPostion.x / chunkSizeOnWord), 1, mapSize);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldPostion.z / chunkSizeOnWord), 1, mapSize);
        return new Vector2Int(x,y);
    }

    /// <summary>
    /// 生成地图块
    /// </summary>
    private MapChunkController GenerateMapChunk(Vector2Int index)
    {
        // 检查坐标的合法性
        if (index.x > mapSize || index.y > mapSize) return null;
        if (index.x < 0 || index.y < 0) return null;
        MapChunkController chunk = mapGenerator.GenerateMapChunk(index, transform);
        mapChunkDic.Add(index, chunk);
        return chunk;
    }


    private void RestCanUpdateChunkFlag()
    {
        canUpdateChunk = true;
    }
}
