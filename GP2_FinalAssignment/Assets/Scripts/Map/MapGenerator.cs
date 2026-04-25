using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MapGrid;
using System.IO;

/// <summary>
/// Map generation tool
/// </summary>
public class MapGenerator : MonoBehaviour
{
    //Number of map chunks
    //Number of grid cells in one map chunk
    //Size of a single cell in meters
    //Pixels per cell texture
    //The entire world is square
    private int mapSize;        //Number of map chunks in a row or column
    private int mapChunkSize;   //Number of grid cells per map chunk
    private float cellSize; //Size of each cell in meters

    private float noiseLacunarity;//Noise frequency parameter (controls terrain density)
    private int mapSeed;//Map seed
    private int spawnSeed;//Object spawning seed
    private int mapHeight; //Map height
    private int mapWidth; //Map width
    private float marshLimit;//Marsh threshold (e.g., >0.5 is marsh, <0.5 is forest)
    private MapGrid mapGrid; //Map logical grid and vertex data structure
    private Material mapMaterial;//Default map material (forest)
    private Material marshMaterial;//Marsh material (dynamically generated texture)
    private Mesh chunkMesh;//Mesh data for map chunks

    private Texture2D forestTexutre;//Forest texture
    private Texture2D[] marshTextures;//Array of marsh transition textures (selected based on transition type)
    private MapConfig mapConfig;//Configuration file for scene object spawning
    //Need a list to record spawned scene objects for future management or clearing
    private List<GameObject> mapObjects = new List<GameObject>();//Stores instances of scene objects (trees, stones, etc.), not the chunks themselves

    //Constructor with parameters
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
    /// Generates map data, primarily data shared across all map chunks
    /// </summary>
    [ContextMenu("Generate Map")] //This attribute allows you to call the GenerateMap method from the Unity Editor's context menu.
    public void GenerateMapData()
    {
        //Generate noise map for height/terrain distribution
        //Pass parameters to the noise generator to get a 2D array of decimals between 0~1 (similar to a topographic map)
        //float[,] noiseMap = GenerateNoiseMap(mapWidth, mapHeight, noiseLacunarity, mapseed);
        //Apply map seed
        Random.InitState(mapSeed);
        float[,] noiseMap = GenerateNoiseMap(mapSize * mapChunkSize, mapSize * mapChunkSize, noiseLacunarity);

        //Generate grid data
        //Pass the width, height, and size into a new MapGrid instance.
        //This executes the MapGrid constructor, generating vertices and cells.
        //mapGrid = new MapGrid(mapHeight, mapWidth, cellSize);
        mapGrid = new MapGrid(mapSize * mapChunkSize, mapSize * mapChunkSize, cellSize);

        //Confirm vertex types and calculate texture index numbers for surrounding grids
        //Pass noise map and limit to grid to determine which transition texture each cell uses
        //E.g., if left is marsh and right is forest, it calculates a specific index
        //int[,] cellTextureIndexMap = mapGrid.CalculateCellTextureIndex(noiseMap, marshLimit);
        mapGrid.CalculateMapVertexType(noiseMap, marshLimit);
        //Initialize default material scale
        mapMaterial.mainTexture = forestTexutre;
        mapMaterial.SetTextureScale("_MainTex", new Vector2(cellSize * mapChunkSize, cellSize * mapChunkSize));
        //Instantiate a marsh material
        marshMaterial = new Material(mapMaterial);
        marshMaterial.SetTextureScale("_MainTex", Vector2.one);
        chunkMesh = GenerateMapMesh(mapChunkSize, mapChunkSize, cellSize);
        //Use spawn seed for randomized generation
        Random.InitState(spawnSeed);

        //Mesh mesh = new Mesh();
        //mesh.vertices = new Vector3[]
        //{ 
        //    new Vector3(0,0,0),
        //    new Vector3(0,1,0),
        //    new Vector3(1,1,0),
        //    new Vector3(1,0,0)
        //};

        //mesh.triangles = new int[]
        //{
        //    0,1,2,
        //    0,2,3
        //};
        //meshFilter.mesh = mesh; //Assign the generated mesh to the MeshFilter component.
    }

    /// <summary>
    /// Generates a map chunk
    /// </summary>
    public MapChunkController GenerateMapChunk(Vector2Int chunkIndex, Transform parent)
    {
        //Generate map chunk object
        GameObject mapChunkObj = new GameObject("Chunk_" + chunkIndex.ToString());
        //mapChunkObj.transform.SetParent(parent);
        MapChunkController mapChunk = mapChunkObj.AddComponent<MapChunkController>();
        mapChunkObj.AddComponent<MeshFilter>().mesh = chunkMesh;
        //Add collider
        mapChunkObj.AddComponent<MeshCollider>();

        //mapChunkObj.AddComponent<MapChunkController>();
        //Generate a map mesh
        //Call the private GenerateMapMesh method below to create a Mesh object and display it via meshFilter
        //mapChunkObj.AddComponent<MeshFilter>().mesh = GenerateMapMesh(mapHeight, mapWidth);

        //Determine coordinates
        Vector3 position = new Vector3(chunkIndex.x * mapChunkSize * cellSize, 0, chunkIndex.y * mapChunkSize * cellSize); //Note: this should be y/z coordinate
        mapChunk.transform.position = position;
        mapChunkObj.transform.SetParent(parent);

        //Generate map chunk texture
        //TODO Optimization
        //Generate texture for the map chunk
        Texture2D mapTexture;
        MonoManager.Instance.StartCoroutine
        (
            GenerateMapTexture(chunkIndex, (tex, isAllForest) => {

                float worldSize = mapChunkSize * cellSize; //Meters of the current chunk in the world

                if (isAllForest)
                {
                    mapChunkObj.AddComponent<MeshRenderer>().sharedMaterial = mapMaterial;

                    //Notify minimap: generate a pure forest chunk (pass null to use default texture)
                    if (MinimapManager.Instance != null)
                        MinimapManager.Instance.RegisterChunk(position, null, worldSize);
                }
                else
                {
                    mapTexture = tex;
                    Material material = new Material(marshMaterial);
                    material.mainTexture = tex;
                    mapChunkObj.AddComponent<MeshRenderer>().material = material;

                    //Notify minimap: generate a chunk texture with specific terrain (marsh, etc.)
                    if (MinimapManager.Instance != null)
                        MinimapManager.Instance.RegisterChunk(position, tex, worldSize);
                }
            })
        );
        //MonoManager.Instance.StartCoroutine
        //(
        //    GenerateMapTexture(chunkIndex, (tex, isAllForest) => {
        //        //If entirely forest, no need to instantiate a new material
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

        //Generate scene object data
        List<MapChunkMapObjectModel> mapObjectModelList = SpawnMapObject(chunkIndex);
        mapChunk.Init(chunkIndex, position + new Vector3((mapChunkSize * cellSize) / 2, 0, (mapChunkSize * cellSize) / 2), mapObjectModelList);

        //[NEW]: Notify AI Manager that this terrain is ready, asking if it should spawn deer herds
        if (AIManager.Instance != null)
        {
            float worldSize = mapChunkSize * cellSize; //Calculate terrain width

            //Pass terrain coordinates and size, and parent it under this Chunk object
            AIManager.Instance.TrySpawnHerdOnChunk(position, worldSize, mapChunkObj.transform);
        }

        //Generate scene objects
        //SpawnMapObject(mapGrid, mapConfig, spawnSeed);
        return mapChunk;
    }

    //public GameObject testObj;
    //[ContextMenu("TestVertex")]
    //public void TestVertex()
    //{
    //    //This method is used to test the functionality of obtaining vertex data via world coordinates.
    //    //It displays a button in the Unity Editor; when clicked, it calls the GetVertexByWorldPosition method,
    //    //passing in the world coordinates of the testObj, and prints the corresponding vertex position.
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
        //Determine where the vertex is
        //This array defines the vertex positions of the mesh. Each Vector3 represents the coordinates of a vertex.
        //Define corner coordinates. Currently hardcoded as a rectangle consisting of 4 points.
        mesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, height * cellSize),
            new Vector3(width * cellSize, 0, height * cellSize),
            new Vector3(width * cellSize, 0, 0),
        };
        //Determine which points form a triangle
        //This array defines which vertices form a triangle. Each set of three integers represents the indices of the three vertices of a triangle.
        mesh.triangles = new int[]
        {
            0,1,2,
            0,2,3
        };

        //This array defines the UV coordinates of each vertex for texture mapping. Each Vector2 represents the UV coordinates of a vertex.
        mesh.uv = new Vector2[]
        {
            new Vector3(0,0), //Note: original code used Vector3; UV typically uses Vector2
            new Vector3(0,1),
            new Vector3(1,1),
            new Vector3(1,0),
        };

        //Calculate normal
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// Generate noise map
    /// </summary>
    private float[,] GenerateNoiseMap(int width, int height, float lacunarity)
    {
        //Apply seed
        //Random.InitState(seed);//This function sets the random number generator state based on the seed. Using the same seed ensures consistent noise generation.
        lacunarity += 0.1f;//Slightly increase frequency to prevent external 0 input from resulting in flat noise.

        //This noise map serves the vertices
        float[,] noiseMap = new float[width - 1, height - 1];
        //Find a random "starting intercept point" between -10,000 and 10,000 in infinite noise space
        float offsetX = Random.Range(-10000f, 10000f);
        float offsetY = Random.Range(-10000f, 10000f);

        //Traverse every vertex of this canvas
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                //Mathf.PerlinNoise returns a decimal between 0~1.
                //Multiply x,y by lacunarity and add offset, then pass into the algorithm.
                //Store in the 2D array.
                noiseMap[x, y] = Mathf.PerlinNoise(x * lacunarity + offsetX, y * lacunarity + offsetY);
            }
        }
        return noiseMap;
    }

    /// <summary>
    /// Generate map texture across multiple frames
    /// If this chunk is entirely forest, return forest texture directly
    /// </summary>
    private IEnumerator GenerateMapTexture(Vector2Int chunkIndex, System.Action<Texture2D, bool> callBack)
    {
        //Current chunk offset to find specific grid cells
        int cellOffsetX = chunkIndex.x * mapChunkSize + 1;
        int cellOffsetY = chunkIndex.y * mapChunkSize + 1;

        //Is it a complete forest chunk
        //bool isAllForest = true;
        //Regardless of forest status, we force texture generation to ensure absolute visual consistency!
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

        ////Check if only forest-type cells exist
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

        ////Map width and height
        //int mapWidth = cellTextureIndexMap.GetLength(0);
        //int mapHeight = cellTextureIndexMap.GetLength(1);


        //Texture2D mapTexture = null;
        //Texture2D mapTexture = new Texture2D(mapWidth * textureCellSize, mapHeight * textureCellSize, TextureFormat.RGB24, false);
        //If entirely forest, return forest texture directly
        //if (!isAllForest)
        //{
        //    //Textures are rectangular
        //    int textureCellSize = forestTexutre.width;
        //    //Width and height of the entire chunk (square)
        //    int textureSize = mapChunkSize * textureCellSize;
        //    mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

        //    //Traverse every cell
        //    for (int y = 0; y < mapChunkSize; y++)
        //    {
        //        //Execute one column per frame; draw pixels for one column
        //        yield return null;
        //        //Pixel offset
        //        int pixelOffsetY = y * textureCellSize;
        //        for (int x = 0; x < mapChunkSize; x++)
        //        {

        //            int pixelOffsetX = x * textureCellSize;
        //            int textureIndex = mapGrid.GetCell(x + cellOffsetX, y + cellOffsetY).TextureIndex - 1;
        //            //Draw pixels within each cell
        //            //Access every pixel point
        //            for (int y1 = 0; y1 < textureCellSize; y1++)
        //            {
        //                for (int x1 = 0; x1 < textureCellSize; x1++)
        //                {

        //                    //Set color for specific pixel point
        //                    //Determine if forest or marsh
        //                    //It is forest ||
        //                    //It is marsh but transparent; in this case draw groundTexture color at the same position
        //                    if (textureIndex < 0)
        //                    {
        //                        Color color = forestTexutre.GetPixel(x1, y1);
        //                        mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, color);
        //                    }
        //                    else
        //                    {
        //                        //Marsh texture color
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
    /// Generate various map objects
    /// </summary>
    private List<MapChunkMapObjectModel> SpawnMapObject(Vector2Int chunkIndex)
    {
        //Use seed for randomized generation
        Random.InitState(spawnSeed + chunkIndex.x * 1000 + chunkIndex.y);
        List<MapChunkMapObjectModel> mapChunkObjectList = new List<MapChunkMapObjectModel>();

        int offsetX = chunkIndex.x * mapChunkSize;
        int offsetY = chunkIndex.y * mapChunkSize;

        //Traverse map vertices
        for (int x = 1; x < mapChunkSize; x++)
        {
            for (int y = 1; y < mapChunkSize; y++)
            {
                MapVertex mapVertex = mapGrid.GetVertex(x + offsetX, y + offsetY);

                //Find spawning rules
                TerrainSpawnRule currentRule = mapConfig.spawnRules.Find(rule => rule.terrainType == mapVertex.VertexType);
                if (currentRule == null) continue;

                //Get spawn pool
                List<MapObjectSpawnConfigModel> configModels = currentRule.spawnModels;

                //Calculate probability
                int randValue = Random.Range(1, 101); //Actual hit range is 1~100 
                float temp = 0;

                //Default to no object selected
                MapObjectSpawnConfigModel spawnModel = null;

                for (int i = 0; i < configModels.Count; i++)
                {
                    temp += configModels[i].probability;

                    //Use <= to ensure hit even at exact boundary
                    if (randValue <= temp)
                    {
                        spawnModel = configModels[i]; //Object hit
                        break;
                    }
                }

                //Double safety check
                //If randValue exceeds total probability, spawnModel remains null (empty space)
                //Only generate if object hit, isEmpty is false, and prefab exists!
                if (spawnModel != null && spawnModel.isEmpty == false && spawnModel.prefab != null)
                {
                    //Instantiate object
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
                DontDestroyOnLoad(go); //Do not destroy when switching scenes
            }
            return instance;
        }
    }
}

public static class MonoExtension
{
    //Extension methods must be static
    //First parameter must use 'this' keyword
    public static Coroutine StartCoroutine(this object obj, IEnumerator routine)
    {
        //Borrow the native MonoManager written above to start coroutines
        return MonoManager.Instance.StartCoroutine(routine);
    }
}