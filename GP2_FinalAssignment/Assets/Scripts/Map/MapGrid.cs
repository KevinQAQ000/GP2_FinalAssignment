using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  Grid, mainly including vertices and cells
/// </summary>
public class MapGrid
{
    // 顶点数据
    public Dictionary<Vector2Int, MapVertex> vertexDic = new Dictionary<Vector2Int, MapVertex>();
    // 格子数据
    public Dictionary<Vector2Int, MapCell> cellDic = new Dictionary<Vector2Int, MapCell>();

    public MapGrid(int mapHeight, int mapWidth, float cellSize)
    {
        MapHeight = mapHeight; // Map height
        MapWidth = mapWidth;   // Map width
        CellSize = cellSize;   // Cell size

        // Generate vertex data
        for (int x = 1; x < mapWidth; x++) // Width
        {
            for (int z = 1; z < mapHeight; z++) // Height
            {
                AddVertex(x, z);
                AddCell(x, z);
            }
        }

        // 增加一行一列
        for (int x = 1; x <= mapWidth; x++)
        {
            AddCell(x, mapHeight);
        }
        for (int z = 1; z < mapWidth; z++)
        {
            AddCell(mapWidth, z);
        }


        #region Test code
        foreach (var item in vertexDic.Values)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere); // Create a sphere to represent the vertex
            temp.transform.position = item.Position; // Set the sphere's position to the vertex position
            temp.transform.localScale = Vector3.one * 0.25f; // Set the sphere's scale to 0.25x
        }
        foreach (var item in cellDic.Values)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.transform.position = item.Position - new Vector3(0, 0.49f, 0);
            temp.transform.localScale = new Vector3(CellSize, 1, CellSize);
        }

        #endregion
    }

    public int MapHeight { get; private set; }
    public int MapWidth { get; private set; }
    public float CellSize { get; private set; }

    #region 顶点
    public void AddVertex(int x, int y) // Add vertex
    {
        vertexDic.Add(new Vector2Int(x, y)
            , new MapVertex() // Vertex data
            {
                Position = new Vector3(x * CellSize, 0, y * CellSize) // World coordinates
            });
    }

    public MapVertex GetVertex(Vector2Int index) // Get vertex via grid coordinates
    {
        return vertexDic[index];
    }
    public MapVertex GetVertex(int x, int y)
    {
        return GetVertex(new Vector2Int(x, y));
    }

    /// <summary>
    /// Obtaining vertices using world coordinates
    /// </summary>
    public MapVertex GetVertexByWorldPosition(Vector3 position) // Get vertex via world position
    {
        // 修正：x 使用 MapWidth，y 使用 MapHeight
        int x = Mathf.Clamp(Mathf.RoundToInt(position.x / CellSize), 1, MapWidth);
        int y = Mathf.Clamp(Mathf.RoundToInt(position.z / CellSize), 1, MapHeight);
        return GetVertex(x, y);
    }
    #endregion

    #region 格子
    private void AddCell(int x, int y)// Add cell
    {
        float offset = CellSize / 2;//Cell的位置是以格子中心为基准的，所以需要一个偏移量
        cellDic.Add(new Vector2Int(x, y), new MapCell()
            {
                Position = new Vector3(x * CellSize - offset, 0, y * CellSize - offset)//Cell的位置是以格子中心为基准的，所以需要一个偏移量
            }
        );
    }

    public MapCell GetCell(Vector2Int index)// Get cell via grid coordinates
    {
        return cellDic[index];
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

    //**************************************************************************************

    /// <summary>
    /// Map vertices
    /// </summary>
    public class MapVertex
    {
        public Vector3 Position; // This is the world coordinates
    }
    /// <summary>
    /// 地图格子
    /// </summary>
    public class MapCell
    {
        public Vector3 Position;
    }
}