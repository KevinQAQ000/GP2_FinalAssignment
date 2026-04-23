using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;// 读写文件必备

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
    private float mapSizeOnWorld;// 在世界中实际的地图尺寸 单位米
    private float chunkSizeOnWorld;  // 在世界中实际的地图块尺寸 单位米
    private List<MapChunkController> lastVisibleChunkList = new List<MapChunkController>();

    //存档信息
    private bool shouldRestorePosition = false;
    private Vector3 savedPlayerPos;

    public static MapManager Instance { get; private set; }
    public float MapSizeOnWorld { get { return mapSize * mapChunkSize * cellSize; } }

    private void Awake()
    {
        Instance = this;
        LoadGameData();// 先读存档（修改变量）
        
    }

    void Start()
    {
        StartCoroutine(RestorePlayerPositionDelayed());// 等地图初始化完了再恢复玩家坐标
        GenerateAirWalls(); // 生成边界空气墙

        // 此时所有物体的 Awake 都跑完了，Player_Controller 绝对存在了！
        if (shouldRestorePosition && Player_Controller.Instance != null)
        {
            // 防 CharacterController 冲突的经典写法
            CharacterController cc = Player_Controller.Instance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // 放心大胆地传送！
            Player_Controller.Instance.playerTransform.position = savedPlayerPos;

            if (cc != null) cc.enabled = true;
            Debug.Log("✅ 成功在 Start 中恢复玩家坐标：" + savedPlayerPos);
        }
        // 初始化地图生成器
        mapGenerator = new MapGenerator(mapSize, mapChunkSize, cellSize, noiseLacunarity, mapSeed, spawnSeed, marshLimit, mapMaterial, forestTexutre, marshTextures, mapConfig);
        // 先读存档（修改变量）
        //LoadGameData();
        mapGenerator.GenerateMapData();
        mapChunkDic = new Dictionary<Vector2Int, MapChunkController>();
        chunkSizeOnWorld = mapChunkSize * cellSize;
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
        for (int i = lastVisibleChunkList.Count - 1; i >= 0; i--)
        {
            Vector2Int chunkIndex = lastVisibleChunkList[i].ChunkIndex;
            if (Mathf.Abs(chunkIndex.x - currChunkIndex.x) > viewDinstance
                || Mathf.Abs(chunkIndex.y - currChunkIndex.y) > viewDinstance)
            {
                lastVisibleChunkList[i].SetActive(false);
                lastVisibleChunkList.RemoveAt(i);
            }
        }

        int startX = currChunkIndex.x - viewDinstance;
        int startY = currChunkIndex.y - viewDinstance;
        // 开启需要显示的地图块
        for (int x = 0; x < 2 * viewDinstance + 1; x++)
        {
            for (int y = 0; y < 2 * viewDinstance + 1; y++)
            {
                canUpdateChunk = false;
                Invoke("RestCanUpdateChunkFlag", updateChunkTime);
                Vector2Int chunkIndex = new Vector2Int(startX + x, startY + y);
                // 之前加载过
                if (mapChunkDic.TryGetValue(chunkIndex, out MapChunkController chunk))
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
                    if (chunk != null)
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
        int x = Mathf.Clamp(Mathf.RoundToInt(worldPostion.x / chunkSizeOnWorld), 1, mapSize);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldPostion.z / chunkSizeOnWorld), 1, mapSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 生成地图块
    /// </summary>
    private MapChunkController GenerateMapChunk(Vector2Int index)
    {
        // 检查坐标的合法性
        if (index.x > mapSize - 1 || index.y > mapSize - 1) return null;
        if (index.x < 0 || index.y < 0) return null;
        MapChunkController chunk = mapGenerator.GenerateMapChunk(index, transform);
        mapChunkDic.Add(index, chunk);
        return chunk;
    }


    private void RestCanUpdateChunkFlag()
    {
        canUpdateChunk = true;
    }

    private void LoadGameData()
    {
        string path = Application.persistentDataPath + "/gamesave.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 覆盖地图变量
            this.mapSize = data.mapSize;
            this.mapSeed = data.mapSeed;
            this.spawnSeed = data.spawnSeed;
            this.marshLimit = data.marshLimit;

            this.mapChunkSize = data.mapChunkSize;
            this.cellSize = data.cellSize;

            // 【关键修改】：只把位置存进变量里，千万别在这里直接传送！
            if (data.hasSavedPosition)
            {
                shouldRestorePosition = true;
                savedPlayerPos = new Vector3(data.playerX, data.playerY, data.playerZ);
            }
        }
    }

    private IEnumerator RestorePlayerPositionDelayed()
    {
        // 等待一帧，确保所有物体的 Awake/Start 都跑完了，地图也初始化了
        yield return null;

        // 重新读取一次位置数据（或者从刚才读好的变量里拿）
        string path = Application.persistentDataPath + "/gamesave.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data.hasSavedPosition && Player_Controller.Instance != null)
            {
                CharacterController cc = Player_Controller.Instance.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                Player_Controller.Instance.playerTransform.position = new Vector3(data.playerX, data.playerY, data.playerZ);

                if (cc != null) cc.enabled = true;
            }
        }
    }

    private void GenerateAirWalls()
    {
        float totalSize = mapSize * mapChunkSize * cellSize;
        float halfSize = totalSize / 2f;

        // 1. 地图中心
        Vector3 actualCenter = new Vector3(halfSize, 0, halfSize);

        // 2. 清理旧墙
        GameObject oldFolder = GameObject.Find("AirWalls_Boundary");
        if (oldFolder != null) DestroyImmediate(oldFolder);
        GameObject wallFolder = new GameObject("AirWalls_Boundary");

        float h = 100f; // 墙高
        float t = 5f;   // 墙厚 (Thickness)

        // 【关键微调】：在 halfSize 的基础上，额外向外偏移“半个墙厚”
        // 这样墙的表面会精准切在地毯边缘，而不是墙的中心在边缘
        float pushOut = halfSize + (t / 2f);

        // 3. 生成四堵墙
        CreateWall(wallFolder.transform, actualCenter + new Vector3(0, 0, pushOut), new Vector3(totalSize + t, h, t), "Wall_North");
        CreateWall(wallFolder.transform, actualCenter + new Vector3(0, 0, -pushOut), new Vector3(totalSize + t, h, t), "Wall_South");
        CreateWall(wallFolder.transform, actualCenter + new Vector3(pushOut, 0, 0), new Vector3(t, h, totalSize + t), "Wall_East");
        CreateWall(wallFolder.transform, actualCenter + new Vector3(-pushOut, 0, 0), new Vector3(t, h, totalSize + t), "Wall_West");
    }

    private void CreateWall(Transform parent, Vector3 pos, Vector3 size, string name)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);

        // 设置墙的位置，高度设为 size.y/2 确保它从地面向上延伸
        wall.transform.position = new Vector3(pos.x, size.y / 2f, pos.z);

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = false;

        // 增加刚体双重保障
        Rigidbody rb = wall.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void OnDrawGizmos()
    {
        // 在编辑器里画出红框，帮助你肉眼对齐
        float totalSize = mapSize * mapChunkSize * cellSize;
        float halfSize = totalSize / 2f;

        // 红框的中心点
        Vector3 gizmoCenter = new Vector3(halfSize, 25, halfSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(gizmoCenter, new Vector3(totalSize, 50, totalSize));
    }
}
