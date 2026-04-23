using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MapGrid;
using System.IO;

/// <summary>
/// 地图生成工具
/// </summary>
public class MapGenerator: MonoBehaviour
{
    //有几个地图块
    //一个地图有多少个格子
    //一个格子有多大
    //一个格子的贴图有多少像素
    //整个世界是方的
    private int mapSize;        // 一行或者一列有多少个地图块
    private int mapChunkSize;   // 一个地图块有多少个格子
    private float cellSize; // 一个格子多少米

    private float noiseLacunarity;// 声明噪声频率参数（控制地形起伏的密集程度）
    private int mapSeed;// 地图种子
    private int spawnSeed;// 地图物品种子
    private int mapHeight; // Map height
    private int mapWidth; // Map width
    private float marshLimit;// 沼泽界限值（比如大于 0.5 是沼泽，小于 0.5 是森林）
    private MapGrid mapGrid; // 地图逻辑网格和顶点数据结构
    private Material mapMaterial;
    private Material marshMaterial;
    private Mesh chunkMesh;

    private Texture2D forestTexutre;
    private Texture2D[] marshTextures;
    private MapConfig mapConfig;// 场景物品生成配置文件
    //需要一个列表来记录我们生成的场景物品，方便日后管理或清除
    private List<GameObject> mapObjects = new List<GameObject>();

    public MapGenerator(int mapSize, int mapChunkSize, float cellSize, float noiseLacunarity, int mapSeed, int spawnSeed, float marshLimit, Material mapMaterial, Texture2D forestTexutre, Texture2D[] marshTextures, MapConfig mapConfig)
    {
        this.mapSize = mapSize;
        this.mapChunkSize = mapChunkSize;
        this.cellSize = cellSize;
        this.noiseLacunarity = noiseLacunarity;
        this.mapSeed = mapSeed;
        this.spawnSeed = spawnSeed;
        this.marshLimit = marshLimit;
        this.mapMaterial = mapMaterial;
        this.forestTexutre = forestTexutre;
        this.marshTextures = marshTextures;
        this.mapConfig = mapConfig;
    }

    /// <summary>
    /// 生成地图数据，主要是所有地图块都通用的数据
    /// </summary>
    [ContextMenu("Generate Map")] // This attribute allows you to call the GenerateMap method from the Unity Editor's context menu.
    public void GenerateMapData()
    {
        // 生成噪声图 高度/地貌分布图
        //把参数传给噪声生成器，得到一张填满 0~1 之间小数的二维表格（类似于地形高低起伏图）
        //float[,] noiseMap = GenerateNoiseMap(mapWidth, mapHeight, noiseLacunarity, mapseed);
        // 应用地图种子
        Random.InitState(mapSeed);
        float[,] noiseMap = GenerateNoiseMap(mapSize * mapChunkSize, mapSize * mapChunkSize, noiseLacunarity);

        // 生成网格数据
        // 面板填好的长、宽、大小传进去，新建一个 MapGrid 实例。
        // 这时会执行 MapGrid 的构造函数，生成那些小球（顶点）和方块（格子）
        //mapGrid = new MapGrid(mapHeight, mapWidth, cellSize);
        mapGrid = new MapGrid(mapSize * mapChunkSize, mapSize * mapChunkSize, cellSize);

        //确认顶点的类型、以及计算顶点周围网格的贴图的索引数字得到
        //把生成的噪声图和 limit 界限值交给 grid，算出每个格子到底该用哪个过渡贴图
        //比如：左边是沼泽右边是森林，它就会算出特定的序号
        //int[,] cellTextureIndexMap = mapGrid.CalculateCellTextureIndex(noiseMap, marshLimit);
        mapGrid.CalculateMapVertexType(noiseMap, marshLimit);
        // 初始化默认材质的尺寸
        mapMaterial.mainTexture = forestTexutre;
        mapMaterial.SetTextureScale("_MainTex", new Vector2(cellSize * mapChunkSize, cellSize * mapChunkSize));
        // 实例化一个沼泽材质
        marshMaterial = new Material(mapMaterial);
        marshMaterial.SetTextureScale("_MainTex", Vector2.one);
        chunkMesh = GenerateMapMesh(mapChunkSize, mapChunkSize, cellSize);
        // 使用种子来进行随机生成
        Random.InitState(spawnSeed);

        // Mesh mesh = new Mesh();
        // mesh.vertices = new Vector3[]
        // { 
        //     new Vector3(0,0,0),
        //     new Vector3(0,1,0),
        //     new Vector3(1,1,0),
        //     new Vector3(1,0,0)
        // };

        // mesh.triangles = new int[]
        // {
        //     0,1,2,
        //     0,2,3
        // };
        // meshFilter.mesh = mesh; // Assign the generated mesh to the MeshFilter component.
    }

    /// <summary>
    /// 生成地图块
    /// </summary>
    public MapChunkController GenerateMapChunk(Vector2Int chunkIndex, Transform parent)
    {
        // 生成地图块物品
        GameObject mapChunkObj = new GameObject("Chunk_" + chunkIndex.ToString());
        //mapChunkObj.transform.SetParent(parent);
        MapChunkController mapChunk = mapChunkObj.AddComponent<MapChunkController>();
        mapChunkObj.AddComponent<MeshFilter>().mesh = chunkMesh;
        // 添加碰撞体
        mapChunkObj.AddComponent<MeshCollider>();

        //mapChunkObj.AddComponent<MapChunkController>();
        // Generate a map mesh
        //调用下面的GenerateMapMesh私有方法创建一个 Mesh 对象，并交给 meshFilter 显示出来
        //mapChunkObj.AddComponent<MeshFilter>().mesh = GenerateMapMesh(mapHeight, mapWidth);

        //确定坐标
        Vector3 position = new Vector3(chunkIndex.x * mapChunkSize * cellSize, 0, chunkIndex.y * mapChunkSize * cellSize); // 注意这里应该是 y
        mapChunk.transform.position = position;
        mapChunkObj.transform.SetParent(parent);

        //生成地图块贴图
        //TO DO 优化
        // 生成地图块的贴图
        Texture2D mapTexture;
        MonoManager.Instance.StartCoroutine
        (
            GenerateMapTexture(chunkIndex, (tex, isAllForest) => {

                float worldSize = mapChunkSize * cellSize; // 当前区块在世界里的米数

                if (isAllForest)
                {
                    mapChunkObj.AddComponent<MeshRenderer>().sharedMaterial = mapMaterial;

                    // 通知小地图：生成一个纯森林的地块（传入 null，它会使用默认贴图）
                    if (MinimapManager.Instance != null)
                        MinimapManager.Instance.RegisterChunk(position, null, worldSize);
                }
                else
                {
                    mapTexture = tex;
                    Material material = new Material(marshMaterial);
                    material.mainTexture = tex;
                    mapChunkObj.AddComponent<MeshRenderer>().material = material;

                    // 通知小地图：生成一个带有具体地形（沼泽等）的地块贴图
                    if (MinimapManager.Instance != null)
                        MinimapManager.Instance.RegisterChunk(position, tex, worldSize);
                }
            })
        );
        //MonoManager.Instance.StartCoroutine
        //(
        //    GenerateMapTexture(chunkIndex, (tex, isAllForest) => {
        //        // 如果完全是森林，没必要在实例化一个材质球
        //        if (isAllForest)
        //        {
        //            mapChunkObj.AddComponent<MeshRenderer>().sharedMaterial = mapMaterial;
        //        }
        //        else
        //        {
        //            mapTexture = tex;
        //            Material material = new Material(marshMaterial);
        //            material.mainTexture = tex;
        //            mapChunkObj.AddComponent<MeshRenderer>().material = material;

        //        }
        //    }));
        //meshRenderer.sharedMaterial.mainTexture = mapTexture;

        // 生成场景物体数据
        List<MapChunkMapObjectModel> mapObjectModelList = SpawnMapObject(chunkIndex);
        mapChunk.Init(chunkIndex, position + new Vector3((mapChunkSize * cellSize) / 2, 0, (mapChunkSize * cellSize) / 2), mapObjectModelList);

        // 【新增】：通知 AI 管理器，这块地铺好了，问它要不要刷鹿群
        if (AIManager.Instance != null)
        {
            float worldSize = mapChunkSize * cellSize; // 计算这块地有多宽

            // 传入这块地的坐标、尺寸，以及把它挂在这个 Chunk 物体下
            AIManager.Instance.TrySpawnHerdOnChunk(position, worldSize, mapChunkObj.transform);
        }

        //生成场景物体
        //SpawnMapObject(mapGrid, mapConfig, spawnSeed);
        return mapChunk;
    }

    //public GameObject testObj;
    //[ContextMenu("TestVertex")]
    //public void TestVertex()
    //{
    //    // This method is used to test the functionality of obtaining vertex data via world coordinates.
    //    // It displays a button in the Unity Editor; when clicked, it calls the GetVertexByWorldPosition method,
    //    // passing in the world coordinates of the testObj, and prints the corresponding vertex position.
    //    print(mapGrid.GetVertexByWorldPosition(testObj.transform.position).Position);
    //}
    //[ContextMenu("TestCell")]
    //public void TestCell(Vector2Int index)
    //{
    //    print(mapGrid.GetRightTopMapCell(index).Position);
    //}

    //**************************************************************************************
    /// <summary>
    /// Generate terrain mesh
    /// </summary>
    private Mesh GenerateMapMesh(int height, int width, float cellSize)
    {
        Mesh mesh = new Mesh();
        // Determine where the vertex is
        // This array defines the vertex positions of the mesh. Each Vector3 represents the coordinates of a vertex.
        // 确定四个角的坐标点。目前写死了一个由 4 个点组成的矩形。
        mesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, height * cellSize),
            new Vector3(width * cellSize, 0, height * cellSize),
            new Vector3(width * cellSize, 0, 0),
        };
        // Determine which points form a triangle
        // This array defines which vertices form a triangle. Each set of three integers represents the indices of the three vertices of a triangle.
        mesh.triangles = new int[]
        {
            0,1,2,
            0,2,3
        };

        // This array defines the UV coordinates of each vertex for texture mapping. Each Vector2 represents the UV coordinates of a vertex.
        mesh.uv = new Vector2[]
        {
            new Vector3(0,0), // 原代码此处使用了 Vector3，通常 UV 使用 Vector2
            new Vector3(0,1),
            new Vector3(1,1),
            new Vector3(1,0),
        };

        // Calculate normal
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// 生成噪声图
    /// </summary>
    private float[,] GenerateNoiseMap(int width, int height, float lacunarity)
    {
        // 应用种子
        //Random.InitState(seed);//这个函数会根据传入的 seed 值来设置随机数生成器的状态。这样，在每次使用相同的 seed 时，生成的随机数序列都会相同，从而确保了噪声图的一致性。
        lacunarity += 0.1f;//// 稍微增加一点频率，防止外部传 0 导致算出来的噪声全是平的。
        
        // 这里的噪声图是为了顶点服务的
        float[,] noiseMap = new float[width - 1, height - 1];
        //// 在无限大的噪声空间里，随机找一个负一万到正一万的坐标作为“起始截取点”
        float offsetX = Random.Range(-10000f, 10000f);
        float offsetY = Random.Range(-10000f, 10000f);

        //// 遍历这块画布的每一个顶点
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                // Mathf.PerlinNoise 返回一个 0~1 的小数。
                // 把坐标 x,y 乘以lacunarity加上offset，扔进算法里。
                // 存进刚才那张二维表格里。
                noiseMap[x, y] = Mathf.PerlinNoise(x * lacunarity + offsetX, y * lacunarity + offsetY);
            }
        }
        return noiseMap;
    }

    /// <summary>
    /// 分帧 生成地图贴图
    /// 如果这个地图块完全是森林，直接返回森林贴图
    /// </summary>
    private IEnumerator GenerateMapTexture(Vector2Int chunkIndex, System.Action<Texture2D, bool> callBack)
    {
        // 当前地块的偏移量 找到这个地图块具体的每一个格子
        int cellOffsetX = chunkIndex.x * mapChunkSize + 1;
        int cellOffsetY = chunkIndex.y * mapChunkSize + 1;

        // 是不是一张完整的森林地图块
        //bool isAllForest = true;
        // 无论是不是全森林，我们都统一强制生成贴图，确保画风绝对一致！
        bool isAllForest = false;

        int textureCellSize = forestTexutre.width;
        int textureSize = mapChunkSize * textureCellSize;
        Texture2D mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

        for (int y = 0; y < mapChunkSize; y++)
        {
            yield return null;
            int pixelOffsetY = y * textureCellSize;
            for (int x = 0; x < mapChunkSize; x++)
            {
                int pixelOffsetX = x * textureCellSize;
                int textureIndex = mapGrid.GetCell(x + cellOffsetX, y + cellOffsetY).TextureIndex - 1;

                for (int y1 = 0; y1 < textureCellSize; y1++)
                {
                    for (int x1 = 0; x1 < textureCellSize; x1++)
                    {
                        if (textureIndex < 0)
                        {
                            Color color = forestTexutre.GetPixel(x1, y1);
                            mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, color);
                        }
                        else
                        {
                            Color color = marshTextures[textureIndex].GetPixel(x1, y1);
                            if (color.a < 1f)
                            {
                                mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, forestTexutre.GetPixel(x1, y1));
                            }
                            else
                            {
                                mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, color);
                            }
                        }
                    }
                }
            }
        }
        mapTexture.filterMode = FilterMode.Point;
        mapTexture.wrapMode = TextureWrapMode.Clamp;
        mapTexture.Apply();

        //// 检查是否只有森林类型的格子
        //for (int y = 0; y < mapChunkSize; y++)
        //{
        //    if (isAllForest == false) break;
        //    for (int x = 0; x < mapChunkSize; x++)
        //    {
        //        MapCell cell = mapGrid.GetCell(x + cellOffsetX, y + cellOffsetY);
        //        if (cell != null && cell.TextureIndex != 0)
        //        {
        //            isAllForest = false;
        //            break;
        //        }
        //    }
        //}

        //// 地图宽高
        //int mapWidth = cellTextureIndexMap.GetLength(0);
        //int mapHeight = cellTextureIndexMap.GetLength(1);


        //Texture2D mapTexture = null;
        //Texture2D mapTexture = new Texture2D(mapWidth * textureCellSize, mapHeight * textureCellSize, TextureFormat.RGB24, false);
        // 如果这个地图块完全是森林，直接返回森林贴图
        //if (!isAllForest)
        //{
        //    // 贴图都是矩形
        //    int textureCellSize = forestTexutre.width;
        //    // 整个地图块的宽高,正方形
        //    int textureSize = mapChunkSize * textureCellSize;
        //    mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

        //    // 遍历每一个格子
        //    for (int y = 0; y < mapChunkSize; y++)
        //    {
        //        // 一帧只执行一列 只绘制一列的像素
        //        yield return null;
        //        // 像素偏移量
        //        int pixelOffsetY = y * textureCellSize;
        //        for (int x = 0; x < mapChunkSize; x++)
        //        {

        //            int pixelOffsetX = x * textureCellSize;
        //            int textureIndex = mapGrid.GetCell(x + cellOffsetX, y + cellOffsetY).TextureIndex - 1;
        //            // 绘制每一个格子内的像素
        //            // 访问每一个像素点
        //            for (int y1 = 0; y1 < textureCellSize; y1++)
        //            {
        //                for (int x1 = 0; x1 < textureCellSize; x1++)
        //                {

        //                    // 设置某个像素点的颜色
        //                    // 确定是森林还是沼泽
        //                    // 这个地方是森林 ||
        //                    // 这个地方是沼泽但是是透明的，这种情况需要绘制groundTexture同位置的像素颜色
        //                    if (textureIndex < 0)
        //                    {
        //                        Color color = forestTexutre.GetPixel(x1, y1);
        //                        mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, color);
        //                    }
        //                    else
        //                    {
        //                        // 是沼泽贴图的颜色
        //                        Color color = marshTextures[textureIndex].GetPixel(x1, y1);
        //                        if (color.a < 1f)
        //                        {
        //                            mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, forestTexutre.GetPixel(x1, y1));
        //                        }
        //                        else
        //                        {
        //                            mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, color);
        //                        }
        //                    }

        //                }
        //            }
        //        }
        //    }
        //    mapTexture.filterMode = FilterMode.Point;
        //    mapTexture.wrapMode = TextureWrapMode.Clamp;
        //    mapTexture.Apply();
        //}
        callBack?.Invoke(mapTexture, isAllForest);
    }

    /// <summary>
    /// 生成各种地图对象
    /// </summary>
    /// <summary>
    /// 生成各种地图对象
    /// </summary>
    private List<MapChunkMapObjectModel> SpawnMapObject(Vector2Int chunkIndex)
    {
        // 使用种子来进行随机生成
        Random.InitState(spawnSeed);
        List<MapChunkMapObjectModel> mapChunkObjectList = new List<MapChunkMapObjectModel>();

        int offsetX = chunkIndex.x * mapChunkSize;
        int offsetY = chunkIndex.y * mapChunkSize;

        // 遍历地图顶点
        for (int x = 1; x < mapChunkSize; x++)
        {
            for (int y = 1; y < mapChunkSize; y++)
            {
                MapVertex mapVertex = mapGrid.GetVertex(x + offsetX, y + offsetY);

                // 1. 找规则
                TerrainSpawnRule currentRule = mapConfig.spawnRules.Find(rule => rule.terrainType == mapVertex.VertexType);
                if (currentRule == null) continue;

                // 2. 拿到生成池
                List<MapObjectSpawnConfigModel> configModels = currentRule.spawnModels;

                // 3. 开始算概率
                int randValue = Random.Range(1, 101); // 实际命中数字是从 1~100 
                float temp = 0;

                // 【修改重点 1】：默认不选中任何物品，用 null 兜底
                MapObjectSpawnConfigModel spawnModel = null;

                for (int i = 0; i < configModels.Count; i++)
                {
                    temp += configModels[i].probability;

                    // 【修改重点 2】：用 <= 确保刚好踩中概率边界时也能命中
                    if (randValue <= temp)
                    {
                        spawnModel = configModels[i]; // 命中物品
                        break;
                    }
                }

                // 【修改重点 3】：双重安全检查
                // 如果摇出的数字大于所有概率总和，spawnModel 就是 null，什么都不生成（自动留空）
                // 只有当不仅命中了物品，且该物品不是空(isEmpty == false)，且挂载了模型时，才真正生成！
                if (spawnModel != null && spawnModel.isEmpty == false && spawnModel.prefab != null)
                {
                    // 实例化物品
                    Vector3 position = mapVertex.Position + new Vector3(Random.Range(-cellSize / 2, cellSize / 2), 0, Random.Range(-cellSize / 2, cellSize / 2));
                    mapChunkObjectList.Add(new MapChunkMapObjectModel() { Prefab = spawnModel.prefab, Position = position });
                }
            }
        }
        return mapChunkObjectList;
    }

}

//******************************************************************************
public class MonoManager : MonoBehaviour
{
    private static MonoManager instance;
    public static MonoManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("MonoManager");
                instance = go.AddComponent<MonoManager>();
                DontDestroyOnLoad(go); // 切换场景时不销毁
            }
            return instance;
        }
    }
}

public static class MonoExtension
{
    // 扩展方法必须是 static
    // 第一个参数必须带有 this 关键字
    public static Coroutine StartCoroutine(this object obj, IEnumerator routine)
    {
        // 借用我们之前写的原生 MonoManager 来开启协程
        return MonoManager.Instance.StartCoroutine(routine);
    }
}