using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    //[Tooltip("每个新地块生成时，有多大几率出现鹿群 (0~1)")]
    //public float herdSpawnProbability = 0.2f; // 20%概率
    //[Range(0, 1)]
    //public float predatorSpawnProbability = 0.05f; // 极低概率，比如 5%

    public static AIManager Instance { get; private set; }

    [Header("全局列表")]
    public List<Transform> allGrassList = new List<Transform>();

    [Header("草的设置")]
    public GameObject[] grassPrefabs;
    public float grassSpawnRadius = 20f;

    [Header("生态分布概率 (0~1)")]
    [Tooltip("刷出鹿群的概率")]
    public float deerProbability = 0.2f;
    [Tooltip("刷出兔群的概率")]
    public float rabbitProbability = 0.25f;
    [Tooltip("刷出狮子的概率")]
    public float lionProbability = 0.05f;
    [Tooltip("刷出老虎的概率")]
    public float tigerProbability = 0.05f;

    [Header("预制体引用")]
    public GameObject deerPrefab;
    public GameObject rabbitPrefab;
    public GameObject lionPrefab;
    public GameObject tigerPrefab;

    [Header("群体数量设置")]
    [Tooltip("一个鹿群最少和最多有几只鹿")]
    public int minDeerCount = 2;
    public int maxDeerCount = 4;

    [Tooltip("一个兔群最少和最多有几只兔子")]
    public int minRabbitCount = 5;  // 默认最低 5 只
    public int maxRabbitCount = 12; // 默认最高 12 只

    private void Awake() => Instance = this;

    //private void Awake()
    //{
    //    Instance = this;
    //}

    // 注意：这里删除了 Start() 方法，因为我们不再一开始就全局盲目刷鹿了！

    /// <summary>
    /// 【新增核心】：当 MapGenerator 铺好一块地时，调用此方法尝试刷鹿
    /// </summary>
    /// <summary>
    /// 当 MapGenerator 铺好一块地时，调用此方法尝试刷鹿
    /// </summary>
    /// <summary>
    /// 地块生成时调用的核心方法
    /// </summary>
    public void TrySpawnHerdOnChunk(Vector3 chunkPos, float chunkSizeWorld, Transform chunkParent)
    {
        // 计算地块中心
        float halfSize = chunkSizeWorld / 2f;
        Vector3 chunkCenter = chunkPos + new Vector3(halfSize, 0, halfSize);

        // --- 逻辑：每一个 if 都是独立的，互不干扰 ---

        // 1. 独立判定鹿群
        if (Random.value < deerProbability)
        {
            SpawnDeers(chunkCenter, chunkParent);
        }

        if (Random.value < rabbitProbability)
        {
            // 给兔子一个偏移，防止和鹿叠在一起
            Vector3 rabbitCenter = chunkCenter + new Vector3(-3f, 0, -3f);
            SpawnRabbits(rabbitCenter, chunkParent);
        }

        // 2. 独立判定狮子 (注意：这里必须用 if，绝对不能用 else if)
        if (Random.value < lionProbability)
        {
            SpawnSinglePredator(lionPrefab, chunkCenter, chunkParent, "Lion");
        }

        // 3. 独立判定老虎 (注意：这里必须用 if，绝对不能用 else if)
        if (Random.value < tigerProbability)
        {
            // 给老虎一个偏移量，防止一出生就和狮子模型重叠
            Vector3 tigerPos = chunkCenter + new Vector3(4f, 0, 4f);
            SpawnSinglePredator(tigerPrefab, tigerPos, chunkParent, "Tiger");
        }
    }

    //public void TrySpawnHerdOnChunk(Vector3 chunkPos, float chunkSizeWorld, Transform chunkParent)
    //{
    //    float halfSize = chunkSizeWorld / 2f;
    //    Vector3 chunkCenter = chunkPos + new Vector3(halfSize, 0, halfSize);

    //    // 1. 尝试生成鹿群 (保持原有逻辑)
    //    if (Random.value < herdSpawnProbability)
    //    {
    //        SpawnDeers(chunkCenter, chunkParent);
    //    }

    //    // 2. 尝试生成掠食者 (新增逻辑)
    //    // 只有没刷鹿的地块，或者运气极好时，才刷掠食者，避免一出生就打架
    //    if (Random.value < predatorSpawnProbability)
    //    {
    //        SpawnPredator(chunkCenter, chunkParent);
    //    }
    //}

    //public void TrySpawnHerdOnChunk(Vector3 chunkPos, float chunkSizeWorld, Transform chunkParent)
    //{
    //    if (deerPrefab == null) return;

    //    // 掷骰子：如果运气不好，这块地就不刷鹿了
    //    if (Random.value > herdSpawnProbability) return;

    //    // 1. 计算这块地的中心点 (假设 chunkPos 是地块左下角)
    //    float halfSize = chunkSizeWorld / 2f;
    //    Vector3 chunkCenter = chunkPos + new Vector3(halfSize, 0, halfSize);

    //    // 2. 在这块地中心生成一个群体锚点
    //    GameObject anchorObj = new GameObject("DeerHerd_Dynamic");
    //    anchorObj.transform.position = chunkCenter;
    //    anchorObj.transform.SetParent(chunkParent);
    //    HerdGroup groupAnchor = anchorObj.AddComponent<HerdGroup>();

    //    // 3. 随机刷 2~3 只鹿
    //    int herdSize = Random.Range(2, 4);
    //    for (int j = 0; j < herdSize; j++)
    //    {
    //        // 注意看！deerCircle 是在这里生成的，所以后面的代码才能用到它
    //        Vector2 deerCircle = Random.insideUnitCircle * 3f;

    //        Vector3 spawnPos = chunkCenter + new Vector3(deerCircle.x, 0, deerCircle.y);

    //        // 生成鹿
    //        GameObject deerObj = Instantiate(deerPrefab, spawnPos, Quaternion.identity);
    //        deerObj.transform.SetParent(chunkParent);

    //        // 绑定锚点
    //        DeerAI ai = deerObj.GetComponent<DeerAI>();
    //        if (ai != null) ai.herdGroupAnchor = groupAnchor.transform;
    //    }
    //}

    //private void SpawnPredator(Vector3 position, Transform parent)
    //{
    //    // 随机选狮子还是老虎
    //    GameObject prefab = (Random.value > 0.5f) ? lionPrefab : tigerPrefab;
    //    if (prefab == null) return;

    //    GameObject predator = Instantiate(prefab, position, Quaternion.identity);
    //    predator.transform.SetParent(parent);

    //    // 如果有初始化逻辑可以在这里写
    //    Debug.Log($"生成了掠食者: {prefab.name} at {position}");
    //}
    private void SpawnSinglePredator(GameObject prefab, Vector3 pos, Transform parent, string debugName)
    {
        if (prefab == null) return;
        GameObject predator = Instantiate(prefab, pos, Quaternion.identity);
        predator.transform.SetParent(parent);
        // Debug.Log($"生态系统：地块生成了 {debugName}");
    }
    private void SpawnDeers(Vector3 center, Transform parent)
    {
        if (deerPrefab == null) return;

        GameObject anchorObj = new GameObject("DeerHerd_Dynamic");
        anchorObj.transform.position = center;
        anchorObj.transform.SetParent(parent);
        anchorObj.AddComponent<HerdGroup>();

        int herdSize = Random.Range(2, 4);
        for (int j = 0; j < herdSize; j++)
        {
            Vector2 circle = Random.insideUnitCircle * 4f; // 稍微散开一点
            Vector3 spawnPos = center + new Vector3(circle.x, 0, circle.y);
            GameObject deer = Instantiate(deerPrefab, spawnPos, Quaternion.identity);
            deer.transform.SetParent(parent);

            DeerAI ai = deer.GetComponent<DeerAI>();
            if (ai != null) ai.herdGroupAnchor = anchorObj.transform;
        }
    }

    private void SpawnRabbits(Vector3 center, Transform parent)
    {
        if (rabbitPrefab == null) return;

        GameObject anchorObj = new GameObject("RabbitHerd_Dynamic");
        anchorObj.transform.position = center;
        anchorObj.transform.SetParent(parent);
        anchorObj.AddComponent<HerdGroup>();

        // 【修改这里】：用你设置的变量代替原本写死的 Random.Range(3, 6)
        // 注意：Unity 的整数 Random.Range 最大值是不包含的，所以要 +1
        int herdSize = Random.Range(minRabbitCount, maxRabbitCount + 1);

        for (int j = 0; j < herdSize; j++)
        {
            // 如果兔子变多了，把出生时的散开半径稍微调大一点，比如从 3f 调到 4f
            Vector2 circle = Random.insideUnitCircle * 4f;
            Vector3 spawnPos = center + new Vector3(circle.x, 0, circle.y);
            GameObject rabbit = Instantiate(rabbitPrefab, spawnPos, Quaternion.identity);
            rabbit.transform.SetParent(parent);

            RabbitAI ai = rabbit.GetComponent<RabbitAI>();
            if (ai != null) ai.herdGroupAnchor = anchorObj.transform;
        }
    }


    // --- 草的管理逻辑 ---

    public void RegisterGrass(Transform grass) => allGrassList.Add(grass);
    public void UnregisterGrass(Transform grass) => allGrassList.Remove(grass);

    public Transform GetNearestGrass(Vector3 aiPosition)
    {
        // 1. 安全清理列表：使用倒序循环删除已经被吃掉的草（彻底解决报错）
        for (int i = allGrassList.Count - 1; i >= 0; i--)
        {
            // Unity 重载了 ==，如果物体被 Destroy，这里会判定为 null
            if (allGrassList[i] == null)
            {
                allGrassList.RemoveAt(i);
            }
        }

        Transform nearest = null;
        float minDistance = float.MaxValue;

        // 2. 遍历找最近的草
        foreach (var grass in allGrassList)
        {
            if (grass == null) continue; // 终极安全锁，防止遍历中途出意外

            float dist = Vector3.Distance(aiPosition, grass.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = grass;
            }
        }
        return nearest;
    }

    public void RespawnGrass()
    {
        if (grassPrefabs.Length == 0) return;
        Vector3 center = Player_Controller.Instance ? Player_Controller.Instance.playerTransform.position : Vector3.zero;
        Vector2 randomCircle = Random.insideUnitCircle * grassSpawnRadius;
        Vector3 spawnPos = center + new Vector3(randomCircle.x, 0, randomCircle.y);
        Instantiate(grassPrefabs[Random.Range(0, grassPrefabs.Length)], spawnPos, Quaternion.identity);
    }
}