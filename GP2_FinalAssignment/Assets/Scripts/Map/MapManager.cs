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

}