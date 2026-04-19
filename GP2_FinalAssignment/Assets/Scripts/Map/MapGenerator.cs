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

    /// <summary>
    /// Generate map
    /// </summary>
    [ContextMenu("Generate Map")] // This attribute allows you to call the GenerateMap method from the Unity Editor's context menu.
    public void GenerateMap()
    {
        // Generate a map mesh
        meshFilter.mesh = GenerateMapMesh(mapHeight, mapWidth);

        // 生成网格
        grid = new MapGrid(mapHeight, mapWidth, cellSize);

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

    //**************************************************************************************
    /// <summary>
    /// Generate terrain mesh
    /// </summary>
    private Mesh GenerateMapMesh(int height, int width) // 修正拼写: wdith -> width
    {
        Mesh mesh = new Mesh();
        // Determine where the vertex is
        // This array defines the vertex positions of the mesh. Each Vector3 represents the coordinates of a vertex.
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
}