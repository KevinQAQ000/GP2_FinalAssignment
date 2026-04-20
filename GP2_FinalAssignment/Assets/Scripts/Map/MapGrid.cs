using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  Grid, mainly including vertices and cells
/// </summary>
public class MapGrid
{
    //顶点数据字典 ，key是网格坐标 (x, y)，Value是顶点对象。用于快速查找某个交叉点。
    //你提供一个坐标（比如第2排第3列 Vector2Int），它就瞬间把那个位置的顶点（MapVertex）或格子（MapCell）的数据交给你。查询速度极快。
    //Vector2Int: 存储两个整数(x, y) 的结构，适合表示网格坐标
    //MapVertex: 自定义的类，存储这个点的信息（如位置）
    public Dictionary<Vector2Int, MapVertex> vertexDic = new Dictionary<Vector2Int, MapVertex>();
    //格子数据,Key是网格坐标 (x, y)，Value是格子对象。格子通常位于四个顶点的中心。
    public Dictionary<Vector2Int, MapCell> cellDic = new Dictionary<Vector2Int, MapCell>();

    //构造函数，创建一个 MapGrid 实例时会调用这个函数。它负责初始化网格的顶点和格子。
    //当其他脚本写下 new MapGrid(10, 10, 1.0f) 时，这段代码就会执行。它把传进来的长、宽、格子大小存到自己脑子里。
    public MapGrid(int mapHeight, int mapWidth, float cellSize)
    {
        //将传入的参数赋值给类内部的属性，方便后面其他函数使用
        MapHeight = mapHeight; // Map height
        MapWidth = mapWidth;   // Map width
        CellSize = cellSize;   // Cell size

        //第一层循环：控制宽度方向（X轴）
        for (int x = 1; x < mapWidth; x++) // Width
        {
            //第二层循环：控制高度方向（Z轴）
            for (int z = 1; z < mapHeight; z++) // Height
            {
                //在 (x, z) 坐标点添加一个顶点
                AddVertex(x, z);
                //在 (x, z) 坐标点添加一个格子
                AddCell(x, z);
            }
        }

        //增加一行一列
        //修补边缘。因为顶点构成了格子的角，4个顶点才能围出1个格子。
        //如果顶点是 10x10，通过上面的循环只能生成 9x9 的格子。
        //这两行是为了把最上面一行和最右边一列的格子补齐。
        for (int x = 1; x <= mapWidth; x++)
        {
            AddCell(x, mapHeight);
        }
        //补全最右边那一列格子
        for (int z = 1; z < mapWidth; z++)
        {
            AddCell(mapWidth, z);
        }


        #region Test code
        ////为了在 Unity 里生成真实的球体和方块 直观感受到网格的存在
        //foreach (var item in vertexDic.Values)
        //{
        //    GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere); // Create a sphere to represent the vertex
        //    temp.transform.position = item.Position; // Set the sphere's position to the vertex position
        //    temp.transform.localScale = Vector3.one * 0.25f; // Set the sphere's scale to 0.25x
        //}
        //foreach (var item in cellDic.Values)
        //{
        //    GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    temp.transform.position = item.Position - new Vector3(0, 0.49f, 0);
        //    temp.transform.localScale = new Vector3(CellSize, 1, CellSize);
        //}

        #endregion
    }

    //get 允许外部读取这些值
    //private set 只有我这个类内部能修改它们，别人只能看不能改
    public int MapHeight { get; private set; }
    public int MapWidth { get; private set; }
    public float CellSize { get; private set; }

    #region 顶点
    //顶点生成与查询
    public void AddVertex(int x, int y) // Add vertex 逻辑坐标转化为物理坐标
    {
        //将一个新的顶点添加到 vertexDic 字典中，键是网格坐标 (x, y)，值是一个新的 MapVertex 对象。
        vertexDic.Add
        (
            new Vector2Int(x, y), new MapVertex() // Vertex data
            {
                //计算世界坐标：
                //x * CellSize: 网格位置乘以每个格子的宽度。比如第2个点，Size是2，那它就在世界空间的 4 的位置。
                //0: 地图目前是平的，所以 Y轴（高度）为 0
                //y * CellSize: 网格的 Y 对应 3D 空间的 Z 轴
                Position = new Vector3(x * CellSize, 0, y * CellSize)
            }
        );
    }

    /// <summary>
    /// 获取顶点，如果找不到返回Null
    /// </summary>
    public MapVertex GetVertex(Vector2Int index) // Get vertex via grid coordinates
    {
        MapVertex vertex = null;
        vertexDic.TryGetValue(index, out vertex);
        return vertex;
    }
    public MapVertex GetVertex(int x, int y)
    {
        return GetVertex(new Vector2Int(x, y));
    }

    /// <summary>
    /// 通过世界坐标获取顶点
    /// </summary>
    public MapVertex GetVertexByWorldPosition(Vector3 position)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(position.x / CellSize), 1, MapWidth);
        int y = Mathf.Clamp(Mathf.RoundToInt(position.z / CellSize), 1, MapHeight);
        return GetVertex(x, y);
    }

    /// <summary>
    /// 设置顶点类型
    /// </summary>
    private void SetVertexType(Vector2Int vertexIndex, MapVertexType mapVertexType)
    {
        MapVertex vertex = GetVertex(vertexIndex);
        if (vertex.VertexType != mapVertexType)
        {
            vertex.VertexType = mapVertexType;// 把顶点的类型改变（比如从森林变成沼泽）
            // 只有沼泽需要计算
            if (vertex.VertexType == MapVertexType.Marsh)// 只有变成沼泽才触发
            {
                // 计算附近的贴图权重

                MapCell tempCell = GetLeftBottomMapCell(vertexIndex);
                if (tempCell != null) tempCell.TextureIndex += 1;

                tempCell = GetRightBottomMapCell(vertexIndex);
                if (tempCell != null) tempCell.TextureIndex += 2;

                tempCell = GetLeftTopMapCell(vertexIndex);
                if (tempCell != null) tempCell.TextureIndex += 4;

                tempCell = GetRightTopMapCell(vertexIndex);
                if (tempCell != null) tempCell.TextureIndex += 8;
            }
        }
    }

    /// <summary>
    /// 设置顶点类型
    /// </summary>
    private void SetVertexType(int x, int y, MapVertexType mapVertexType)
    {
        SetVertexType(new Vector2Int(x, y), mapVertexType);
    }

    #endregion

    //**************************************************************************************/

    #region 格子
    private void AddCell(int x, int y)// 生成格子数据
    {
        float offset = CellSize / 2;//Cell的位置是以格子中心为基准的，所以需要一个偏移量
        cellDic.Add
        (
            // offset: 格子的中心点偏移量。
            // 顶点在角落（0,0），但格子的模型（Cube）中心点是在正中间的。
            // 所以格子的坐标要往左下角回退半个身位，才能正好对齐四个顶点。
            new Vector2Int(x, y), new MapCell()
            {
                // x * CellSize - offset: 
                // 比如格子坐标是 (1,1)，Size 是 1。
                // 它的顶点在 (1,0,1)，它的中心就在 (0.5, 0, 0.5)
                Position = new Vector3(x * CellSize - offset, 0, y * CellSize - offset)//Cell的位置是以格子中心为基准的，所以需要一个偏移量
            }
        );
    }

    public MapCell GetCell(Vector2Int index)// Get cell via grid coordinates
    {
        MapCell cell = null;
        cellDic.TryGetValue(index, out cell);
        return cell;
    }

    public MapCell GetCell(int x, int y)
    {
        return GetCell(new Vector2Int(x, y));
    }

    /// <summary>
    /// 获取左下角格子
    /// </summary>
    public MapCell GetLeftBottomMapCell(Vector2Int vertexIndex)
    {
        return cellDic[vertexIndex];
    }

    /// <summary>
    /// 获取右下角格子
    /// </summary>
    public MapCell GetRightBottomMapCell(Vector2Int vertexIndex)
    {
        return cellDic[new Vector2Int(vertexIndex.x + 1, vertexIndex.y)];
    }

    /// <summary>
    /// 获取左上角格子
    /// </summary>
    public MapCell GetLeftTopMapCell(Vector2Int vertexIndex)
    {
        return cellDic[new Vector2Int(vertexIndex.x, vertexIndex.y + 1)];
    }

    /// <summary>
    /// 获取右上角格子
    /// </summary>
    public MapCell GetRightTopMapCell(Vector2Int vertexIndex)
    {
        return cellDic[new Vector2Int(vertexIndex.x + 1, vertexIndex.y + 1)];
    }

    #endregion

    /// <summary>
    /// 计算格子贴图的索引数字
    /// </summary>
    public int[,] CalculateCellTextureIndex(float[,] noiseMap, float limit)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        for (int x = 1; x < width; x++)
        {
            for (int z = 1; z < height; z++)
            {
                // 基于噪声中的值确定这个顶点的类型
                // 大于边界是沼泽，否则是森林
                if (noiseMap[x, z] >= limit)
                {
                    SetVertexType(x, z, MapVertexType.Marsh);
                }
                else
                {
                    SetVertexType(x, z, MapVertexType.Forest);
                }
            }
        }

        // 到这里，可以确定所有格子对应的贴图索引
        int[,] textureIndexMap = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                textureIndexMap[x, z] = GetCell(x + 1, z + 1).TextureIndex;
            }
        }

        return textureIndexMap;
    }
     
    //**************************************************************************************

    /// <summary>
    /// 顶点类型
    /// </summary>
    public enum MapVertexType
    {
        Forest, //森林
        Marsh,  //沼泽
    }

    /// <summary>
    /// Map vertices
    /// </summary>
    public class MapVertex
    {
        public Vector3 Position; // This is the world coordinates
        public MapVertexType VertexType;
    }
    /// <summary>
    /// 地图格子
    /// </summary>
    public class MapCell
    {
        public Vector3 Position;
        public int TextureIndex;
    }
}