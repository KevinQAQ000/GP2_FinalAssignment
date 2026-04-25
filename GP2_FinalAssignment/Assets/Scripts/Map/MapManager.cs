using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;//Essential for file read/write

public class MapManager : MonoBehaviour
{
    //Map dimensions
    public int mapSize;        //Number of map chunks in a row or column
    public int mapChunkSize;   //Number of grid cells in one map chunk
    public float cellSize;     //Meters per grid cell

    //Map randomization parameters
    public float noiseLacunarity;  //Noise lacunarity
    public int mapSeed;            //Map seed
    public int spawnSeed;          //Seed for random map objects
    public float marshLimit;       //Boundary for marshes

    //Map art resources
    public Material mapMaterial;//Map material
    public Texture2D forestTexutre;//Forest texture
    public Texture2D[] marshTextures;//Marsh textures
    public MapConfig mapConfig;   //Map configuration

    private MapGenerator mapGenerator;//Map generator
    public int viewDinstance;       //Player view distance in Chunks
    public Transform viewer;        //Viewer (usually the player)
    private Vector3 lastViewerPos = Vector3.one * -1;//Viewer position in last frame. Initialized to an unlikely coordinate to ensure a refresh at start.
    public Dictionary<Vector2Int, MapChunkController> mapChunkDic;  //All existing map chunks

    public float updateChunkTime = 1f;//Interval to refresh map chunks in seconds
    private bool canUpdateChunk = true;//Flag to control refresh frequency combined with updateChunkTime
    private float mapSizeOnWorld;//Actual map size in world (meters)
    private float chunkSizeOnWorld;  //Actual chunk size in world (meters)
    private List<MapChunkController> lastVisibleChunkList = new List<MapChunkController>();//List of visible chunks in the last frame

    //Save data information
    private bool shouldRestorePosition = false;//Whether to restore player position (set during loading, used after map initialization)
    private Vector3 savedPlayerPos;//Player position read from save file, used after map initialization

    public static MapManager Instance { get; private set; }//Singleton for easy access from other scripts
    //Calculate actual map size in the world in meters
    public float MapSizeOnWorld { get { return mapSize * mapChunkSize * cellSize; } }

    private void Awake()
    {
        Instance = this;
        LoadGameData();//Read save data first (modify variables)

    }

    void Start()
    {
        StartCoroutine(RestorePlayerPositionDelayed());//Restore player coordinates after map initialization
        GenerateAirWalls(); //Generate boundary air walls

        //At this point, Awake of all objects has finished, and Player_Controller exists
        if (shouldRestorePosition && Player_Controller.Instance != null)
        {
            //Prevent CharacterController conflicts
            CharacterController cc = Player_Controller.Instance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;//Disable character controller to avoid interference from the physics system during positioning

            //Teleport!
            Player_Controller.Instance.playerTransform.position = savedPlayerPos;

            //Re-enable character controller after restoration
            if (cc != null) cc.enabled = true;
            Debug.Log("✅ Successfully restored player coordinates in Start：" + savedPlayerPos);
        }
        //Initialize map generator
        mapGenerator = new MapGenerator(mapSize, mapChunkSize, cellSize, noiseLacunarity, mapSeed, spawnSeed, marshLimit, mapMaterial, forestTexutre, marshTextures, mapConfig);

        mapGenerator.GenerateMapData();//Generate map data
        mapChunkDic = new Dictionary<Vector2Int, MapChunkController>();//Initialize map chunk dictionary
        chunkSizeOnWorld = mapChunkSize * cellSize;//Calculate actual chunk size in the world
    }

    //Update is called once per frame
    void Update()
    {
        UpdateVisibleChunk();//Refresh visible map chunks
    }

    //Refresh which map chunks are visible based on the viewer's position
    private void UpdateVisibleChunk()
    {
        //No refresh needed if viewer hasn't moved
        if (viewer.position == lastViewerPos) return;
        //Do not update if the cooldown time hasn't elapsed
        if (canUpdateChunk == false) return;

        //Get the index of the chunk the viewer is currently in
        Vector2Int currChunkIndex = GetMapChunkIndexByWorldPosition(viewer.position);

        //Deactivate all chunks that no longer need to be displayed
        for (int i = lastVisibleChunkList.Count - 1; i >= 0; i--)
        {
            //If the chunk coordinate distance from the viewer exceeds view distance, deactivate it
            Vector2Int chunkIndex = lastVisibleChunkList[i].ChunkIndex;
            //Comparing chunk coordinates (chunkIndex), not world coordinates! View distance is unit-based on chunks.
            if (Mathf.Abs(chunkIndex.x - currChunkIndex.x) > viewDinstance
                || Mathf.Abs(chunkIndex.y - currChunkIndex.y) > viewDinstance)
            {
                //Chunk is out of view range, deactivate it
                lastVisibleChunkList[i].SetActive(false);
                lastVisibleChunkList.RemoveAt(i);
            }
        }

        int startX = currChunkIndex.x - viewDinstance;
        int startY = currChunkIndex.y - viewDinstance;
        //Activate chunks that need to be displayed
        for (int x = 0; x < 2 * viewDinstance + 1; x++)
        {
            for (int y = 0; y < 2 * viewDinstance + 1; y++)
            {
                canUpdateChunk = false;//Disallow updates until updateChunkTime has passed
                Invoke("RestCanUpdateChunkFlag", updateChunkTime);
                Vector2Int chunkIndex = new Vector2Int(startX + x, startY + y);//Check if this visible chunk was previously loaded

                //Previously loaded
                if (mapChunkDic.TryGetValue(chunkIndex, out MapChunkController chunk))
                {
                    //Check if this chunk is already in the visible list
                    if (lastVisibleChunkList.Contains(chunk) == false)
                    {
                        lastVisibleChunkList.Add(chunk);
                        chunk.SetActive(true);
                    }
                }
                //Not previously loaded
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
    /// Get map chunk index based on world position
    /// </summary>
    ///Divide world coordinates by meters per chunk and use RoundToInt to find the grid row/column.
    ///Clamp is used to prevent coordinates from exceeding world boundaries.
    private Vector2Int GetMapChunkIndexByWorldPosition(Vector3 worldPostion)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(worldPostion.x / chunkSizeOnWorld), 1, mapSize);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldPostion.z / chunkSizeOnWorld), 1, mapSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Generate map chunk
    /// </summary>
    private MapChunkController GenerateMapChunk(Vector2Int index)
    {
        //Check coordinate validity
        if (index.x > mapSize - 1 || index.y > mapSize - 1) return null;
        if (index.x < 0 || index.y < 0) return null;
        //Generate the map chunk
        MapChunkController chunk = mapGenerator.GenerateMapChunk(index, transform);
        //Add generated chunk to the dictionary
        mapChunkDic.Add(index, chunk);
        return chunk;
    }


    private void RestCanUpdateChunkFlag()//Resets canUpdateChunk to true after updateChunkTime seconds to allow refreshing
    {
        canUpdateChunk = true;
    }

    private void LoadGameData()//Reads data from save file and overrides map-related variables
    {
        //Check if save file exists
        string path = Application.persistentDataPath + "/gamesave.json";
        if (File.Exists(path))//Save file exists, proceed with reading
        {
            //Read save file content and deserialize into SaveData object
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            //Override map variables
            this.mapSize = data.mapSize;
            this.mapSeed = data.mapSeed;
            this.spawnSeed = data.spawnSeed;
            this.marshLimit = data.marshLimit;

            this.mapChunkSize = data.mapChunkSize;
            this.cellSize = data.cellSize;

            //Only store the position in a variable; do not teleport here!
            if (data.hasSavedPosition)
            {
                //Set flag to restore player position after map initialization.
                //Restoration must occur after map generation to avoid being reset or interfered with.
                shouldRestorePosition = true;
                savedPlayerPos = new Vector3(data.playerX, data.playerY, data.playerZ);
            }
        }
    }

    //Double-insurance coroutine. Sometimes teleportation in Start fails because the physics engine hasn't reacted.
    //Waits one frame (yield return null) then teleports again to ensure success.
    private IEnumerator RestorePlayerPositionDelayed()
    {
        //Wait one frame to ensure Awake/Start of all objects and map initialization are finished
        yield return null;

        //Re-read position data (or take from the already loaded variable)
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

    private void GenerateAirWalls()//Generates boundary air walls to prevent player from leaving the map
    {
        float totalSize = mapSize * mapChunkSize * cellSize;
        float halfSize = totalSize / 2f;

        //Map center
        Vector3 actualCenter = new Vector3(halfSize, 0, halfSize);

        //Clean up old walls
        GameObject oldFolder = GameObject.Find("AirWalls_Boundary");
        if (oldFolder != null) DestroyImmediate(oldFolder);
        GameObject wallFolder = new GameObject("AirWalls_Boundary");

        float h = 100f; //Wall height
        float t = 5f;   //Wall thickness

        //Offset outward by "half thickness" based on halfSize
        //This ensures the wall surface aligns exactly with the floor edge rather than its center
        float pushOut = halfSize + (t / 2f);

        //Generate four walls
        CreateWall(wallFolder.transform, actualCenter + new Vector3(0, 0, pushOut), new Vector3(totalSize + t, h, t), "Wall_North");
        CreateWall(wallFolder.transform, actualCenter + new Vector3(0, 0, -pushOut), new Vector3(totalSize + t, h, t), "Wall_South");
        CreateWall(wallFolder.transform, actualCenter + new Vector3(pushOut, 0, 0), new Vector3(t, h, totalSize + t), "Wall_East");
        CreateWall(wallFolder.transform, actualCenter + new Vector3(-pushOut, 0, 0), new Vector3(t, h, totalSize + t), "Wall_West");
    }

    //Generates a single wall with specified parent, position, size, and name
    private void CreateWall(Transform parent, Vector3 pos, Vector3 size, string name)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);

        //Set position. Height is size.y/2 to ensure it extends upward from the ground.
        wall.transform.position = new Vector3(pos.x, size.y / 2f, pos.z);

        //Add BoxCollider and configure size and trigger properties
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = false;

        //Add Rigidbody for extra collision assurance
        Rigidbody rb = wall.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void OnDrawGizmos()
    {
        //Draw a red box in the editor for visual alignment assistance
        float totalSize = mapSize * mapChunkSize * cellSize;
        float halfSize = totalSize / 2f;

        //Center of the gizmo box
        Vector3 gizmoCenter = new Vector3(halfSize, 25, halfSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(gizmoCenter, new Vector3(totalSize, 50, totalSize));
    }
}