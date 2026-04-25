using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid, mainly including vertices and cells
/// </summary>
public class MapGrid
{
    //Vertex data dictionary, key is grid coordinates (x, y), value is vertex object. Used for rapid lookup of intersection points.
    //Provide a coordinate (e.g., Row 2, Column 3 Vector2Int), and it instantly returns the vertex (MapVertex) or cell (MapCell) data. Very fast query speed.
    //Vector2Int: Structure storing two integers (x, y), suitable for representing grid coordinates.
    //MapVertex: Custom class storing information of this point (such as position).
    public Dictionary<Vector2Int, MapVertex> vertexDic = new Dictionary<Vector2Int, MapVertex>();
    //Cell data, key is grid coordinates (x, y), value is cell object. Cells are usually located at the center of four vertices.
    public Dictionary<Vector2Int, MapCell> cellDic = new Dictionary<Vector2Int, MapCell>();

    //Constructor, called when creating a MapGrid instance. It initializes the grid's vertices and cells.
    //When other scripts call "new MapGrid(10, 10, 1.0f)", this code executes, storing the passed height, width, and cell size.
    public MapGrid(int mapHeight, int mapWidth, float cellSize)//Constructor called when creating a MapGrid instance. It initializes vertices and cells.
    {
        //Assign passed parameters to internal properties for use by other functions
        MapHeight = mapHeight; //Map height
        MapWidth = mapWidth;   //Map width
        CellSize = cellSize;   //Cell size

        //First loop: Controls width direction (X-axis)
        for (int x = 1; x < mapWidth; x++) //Width
        {
            //Second loop: Controls height direction (Z-axis)
            for (int z = 1; z < mapHeight; z++) //Height
            {
                //Add a vertex at (x, z) coordinates
                AddVertex(x, z);
                //Add a cell at (x, z) coordinates
                AddCell(x, z);
            }
        }

        //Add an extra row and column
        //Repair edges. Since vertices form cell corners, 4 vertices enclose 1 cell.
        //If vertices are 10x10, the loop above only generates 9x9 cells.
        //These loops supplement the top row and rightmost column of cells.
        for (int x = 1; x <= mapWidth; x++)
        {
            AddCell(x, mapHeight);
        }
        //Complete the rightmost column of cells
        for (int z = 1; z < mapWidth; z++)
        {
            AddCell(mapWidth, z);
        }


        #region Test code
        ////To generate actual spheres and cubes in Unity to visualize the grid
        //foreach (var item in vertexDic.Values)
        //{
        //    GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere); //Create a sphere to represent the vertex
        //    temp.transform.position = item.Position; //Set the sphere's position to the vertex position
        //    temp.transform.localScale = Vector3.one * 0.25f; //Set the sphere's scale to 0.25x
        //}
        //foreach (var item in cellDic.Values)
        //{
        //    GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    temp.transform.position = item.Position - new Vector3(0, 0.49f, 0);
        //    temp.transform.localScale = new Vector3(CellSize, 1, CellSize);
        //}

        #endregion
    }

    //get allows external reading of these values
    //private set ensures only this class can modify them internally
    public int MapHeight { get; private set; }
    public int MapWidth { get; private set; }
    public float CellSize { get; private set; }

    #region Vertices
    //Vertex generation and query
    public void AddVertex(int x, int y) //Add vertex, converting grid coordinates to world position
    {
        //Add a new vertex to the vertexDic dictionary, key is grid (x, y), value is a new MapVertex object.
        vertexDic.Add
        (
            new Vector2Int(x, y), new MapVertex() //Vertex data
            {
                //Calculate world coordinates:
                //x * CellSize: Grid position multiplied by cell width. E.g., 2nd point with Size 2 is at world position 4.
                //0: Map is currently flat, so Y-axis (height) is 0.
                //y * CellSize: Grid Y corresponds to the 3D Z-axis.
                Position = new Vector3(x * CellSize, 0, y * CellSize)
            }
        );
    }

    /// <summary>
    /// Get vertex; returns null if not found
    /// </summary>
    public MapVertex GetVertex(Vector2Int index) //Get vertex via grid coordinates
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
    /// Get vertex via world position
    /// </summary>
    public MapVertex GetVertexByWorldPosition(Vector3 position)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(position.x / CellSize), 1, MapWidth);
        int y = Mathf.Clamp(Mathf.RoundToInt(position.z / CellSize), 1, MapHeight);
        return GetVertex(x, y);
    }

    /// <summary>
    /// Set vertex type
    /// </summary>
    private void SetVertexType(Vector2Int vertexIndex, MapVertexType mapVertexType)
    {
        MapVertex vertex = GetVertex(vertexIndex);
        if (vertex.VertexType != mapVertexType)
        {
            vertex.VertexType = mapVertexType;//Change vertex type (e.g., from Forest to Marsh)
            //Only Marsh requires calculations
            if (vertex.VertexType == MapVertexType.Marsh)//Only trigger when becoming Marsh
            {
                //Calculate nearby texture weights

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
    /// Set vertex type
    /// </summary>
    private void SetVertexType(int x, int y, MapVertexType mapVertexType)
    {
        SetVertexType(new Vector2Int(x, y), mapVertexType);
    }

    #endregion

    //**************************************************************************************/

    #region Cells
    private void AddCell(int x, int y)//Generate cell data
    {
        float offset = CellSize / 2;//Cell position is based on the center of the grid, so an offset is required
        cellDic.Add
        (
            //offset: The center point offset of the cell.
            //Vertices are at corners (0,0), but the cell model (Cube) center is in the middle.
            //So the cell coordinate must step back half a size from the bottom-left to align with the four vertices.
            new Vector2Int(x, y), new MapCell()
            {
                //x * CellSize - offset: 
                //E.g., grid coordinate is (1,1), Size is 1.
                //Its vertex is at (1,0,1), its center is at (0.5, 0, 0.5).
                Position = new Vector3(x * CellSize - offset, 0, y * CellSize - offset)//Cell position is based on the center, requiring an offset
            }
        );
    }

    public MapCell GetCell(Vector2Int index)//Get cell via grid coordinates
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
    /// Get bottom-left cell
    /// </summary>
    public MapCell GetLeftBottomMapCell(Vector2Int vertexIndex)
    {
        return cellDic[vertexIndex];
    }

    /// <summary>
    /// Get bottom-right cell
    /// </summary>
    public MapCell GetRightBottomMapCell(Vector2Int vertexIndex)
    {
        return cellDic[new Vector2Int(vertexIndex.x + 1, vertexIndex.y)];
    }

    /// <summary>
    /// Get top-left cell
    /// </summary>
    public MapCell GetLeftTopMapCell(Vector2Int vertexIndex)
    {
        return cellDic[new Vector2Int(vertexIndex.x, vertexIndex.y + 1)];
    }

    /// <summary>
    /// Get top-right cell
    /// </summary>
    public MapCell GetRightTopMapCell(Vector2Int vertexIndex)
    {
        return cellDic[new Vector2Int(vertexIndex.x + 1, vertexIndex.y + 1)];
    }

    #endregion

    /// <summary>
    /// Calculate cell texture index numbers
    /// </summary>
    public void CalculateMapVertexType(float[,] noiseMap, float limit)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        for (int x = 1; x < width; x++)
        {
            for (int z = 1; z < height; z++)
            {
                //Determine this vertex type based on the noise value
                //Marsh if greater than limit, otherwise Forest
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

        //By this point, texture indices for all cells are determined
        //int[,] textureIndexMap = new int[width, height];
        //for (int x = 0; x < width; x++)
        //{
        //    for (int z = 0; z < height; z++)
        //    {
        //        textureIndexMap[x, z] = GetCell(x + 1, z + 1).TextureIndex;
        //    }
        //}

        //return textureIndexMap;
    }

    //**************************************************************************************

    /// <summary>
    /// Vertex types
    /// </summary>
    public enum MapVertexType
    {
        Forest, //Forest
        Marsh,  //Marsh
    }

    /// <summary>
    /// Map vertex data
    /// </summary>
    public class MapVertex
    {
        public Vector3 Position; //World coordinates
        public MapVertexType VertexType;
    }
    /// <summary>
    /// Map cell data
    /// </summary>
    public class MapCell
    {
        public Vector3 Position;
        public int TextureIndex;
    }
}