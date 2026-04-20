using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public MeshRenderer meshRenderer; // Map renderer
    public MeshFilter meshFilter; // This is the MeshFilter component for the map, used to generate the map's mesh.
    public int mapHeight; // Map height
    public int mapWidth; // Map width
    public float cellSize; // Cell size
    MapGrid grid; // This is a reference to the MapGrid class, which is responsible for managing the grid structure of the map.
    public float lacunarity;// 声明噪声频率参数（控制地形起伏的密集程度）
    public int seed;// 声明随机种子（用来复刻特定地形）
    [Range(0f, 1f)]
    public float limit;// 声明界限值（比如大于 0.5 是沼泽，小于 0.5 是森林）

    public Texture2D groundTexutre;
    public Texture2D[] marshTextures;

    /// <summary>
    /// Generate map
    /// </summary>
    [ContextMenu("Generate Map")] // This attribute allows you to call the GenerateMap method from the Unity Editor's context menu.
    public void GenerateMap()
    {
        // Generate a map mesh
        //调用下面的GenerateMapMesh私有方法创建一个 Mesh 对象，并交给 meshFilter 显示出来
        meshFilter.mesh = GenerateMapMesh(mapHeight, mapWidth);

        // 生成网格
        // 面板填好的长、宽、大小传进去，新建一个 MapGrid 实例。
        // 这时会执行 MapGrid 的构造函数，生成那些小球（顶点）和方块（格子）
        grid = new MapGrid(mapHeight, mapWidth, cellSize);
        // 生成噪声图 高度/地貌分布图
        //把参数传给噪声生成器，得到一张填满 0~1 之间小数的二维表格（类似于地形高低起伏图）
        float[,] noiseMap = GenerateNoiseMap(mapWidth, mapHeight, lacunarity, seed);
        //确认顶点的类型、以及计算顶点周围网格的贴图的索引数字得到
        //把生成的噪声图和 limit 界限值交给 grid，算出每个格子到底该用哪个过渡贴图
        //比如：左边是沼泽右边是森林，它就会算出特定的序号
        int[,] cellTextureIndexMap = grid.CalculateCellTextureIndex(noiseMap, limit);


        // 基于网格的贴图索引数字 生成地图贴图
        Texture2D mapTexture = GenerateMapTexture(cellTextureIndexMap, groundTexutre, marshTextures);
        meshRenderer.sharedMaterial.mainTexture = mapTexture;

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

    public GameObject testObj;
    [ContextMenu("TestVertex")]
    public void TestVertex()
    {
        // This method is used to test the functionality of obtaining vertex data via world coordinates.
        // It displays a button in the Unity Editor; when clicked, it calls the GetVertexByWorldPosition method,
        // passing in the world coordinates of the testObj, and prints the corresponding vertex position.
        print(grid.GetVertexByWorldPosition(testObj.transform.position).Position);
    }
    [ContextMenu("TestCell")]
    public void TestCell(Vector2Int index)
    {
        print(grid.GetRightTopMapCell(index).Position);
    }

    //**************************************************************************************
    /// <summary>
    /// Generate terrain mesh
    /// </summary>
    private Mesh GenerateMapMesh(int height, int width) // 修正拼写: wdith -> width
    {
        Mesh mesh = new Mesh();
        // Determine where the vertex is
        // This array defines the vertex positions of the mesh. Each Vector3 represents the coordinates of a vertex.
        // 确定四个角的坐标点。目前写死了一个由 4 个点组成的矩形。
        mesh.vertices = new Vector3[]
        {
            new Vector3(0,0,0),
            new Vector3(0,0,height),
            new Vector3(width,0,height), // 修正拼写: wdith -> width
            new Vector3(width,0,0),     // 修正拼写: wdith -> width
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
    private float[,] GenerateNoiseMap(int width, int height, float lacunarity, int seed)
    {
        // 应用种子
        Random.InitState(seed);//这个函数会根据传入的 seed 值来设置随机数生成器的状态。这样，在每次使用相同的 seed 时，生成的随机数序列都会相同，从而确保了噪声图的一致性。
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
    /// 生成地图贴图
    /// </summary>
    private Texture2D GenerateMapTexture(int[,] cellTextureIndexMap, Texture2D groundTexture, Texture2D[] marshTextures)
    {
        // 地图宽高
        int mapWidth = cellTextureIndexMap.GetLength(0);
        int mapHeight = cellTextureIndexMap.GetLength(1);
        // 贴图都是矩形
        int textureCellSize = groundTexture.width;
        Texture2D mapTexture = new Texture2D(mapWidth * textureCellSize, mapHeight * textureCellSize);

        // 遍历每一个格子
        for (int y = 0; y < mapHeight; y++)
        {
            int offsetY = y * textureCellSize;
            for (int x = 0; x < mapWidth; x++)
            {
                int offsetX = x * textureCellSize;
                int textureIndex = cellTextureIndexMap[x, y] - 1;
                // 绘制每一个格子内的像素
                // 访问每一个像素点
                for (int y1 = 0; y1 < textureCellSize; y1++)
                {
                    for (int x1 = 0; x1 < textureCellSize; x1++)
                    {
                        // 设置某个像素点的颜色
                        // 确定是森林还是沼泽
                        // 这个地方是森林 ||
                        // 这个地方是沼泽但是是透明的，这种情况需要绘制groundTexture同位置的像素颜色
                        if (textureIndex < 0)
                        {
                            Color color = groundTexture.GetPixel(x1, y1);
                            mapTexture.SetPixel(x1 + offsetX, y1 + offsetY, color);
                        }
                        else
                        {
                            // 是沼泽贴图的颜色
                            Color color = marshTextures[textureIndex].GetPixel(x1, y1);
                            if (color.a == 0)
                            {
                                mapTexture.SetPixel(x1 + offsetX, y1 + offsetY, groundTexture.GetPixel(x1, y1));
                            }
                            else
                            {
                                mapTexture.SetPixel(x1 + offsetX, y1 + offsetY, color);
                            }
                        }

                    }
                }
            }
        }


        mapTexture.filterMode = FilterMode.Point;
        mapTexture.wrapMode = TextureWrapMode.Clamp;
        mapTexture.Apply();
        return mapTexture;
    }

}