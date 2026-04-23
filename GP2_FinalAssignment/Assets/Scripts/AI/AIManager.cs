using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    [Header("草的生成设置")]
    public GameObject[] grassPrefabs;
    [Tooltip("草在玩家周围多大范围内刷新")]
    public float grassSpawnRadius = 20f;

    [Header("鹿的动态生成设置")]
    public GameObject deerPrefab;
    [Tooltip("每个新地块生成时，有多大几率出现鹿群 (0~1)")]
    public float herdSpawnProbability = 0.2f; // 20%概率

    [Header("全局数据（不用填）")]
    public List<Transform> allGrassList = new List<Transform>();

    private void Awake()
    {
        Instance = this;
    }

    // 注意：这里删除了 Start() 方法，因为我们不再一开始就全局盲目刷鹿了！

    /// <summary>
    /// 【新增核心】：当 MapGenerator 铺好一块地时，调用此方法尝试刷鹿
    /// </summary>
    /// <summary>
    /// 当 MapGenerator 铺好一块地时，调用此方法尝试刷鹿
    /// </summary>
    public void TrySpawnHerdOnChunk(Vector3 chunkPos, float chunkSizeWorld, Transform chunkParent)
    {
        if (deerPrefab == null) return;

        // 掷骰子：如果运气不好，这块地就不刷鹿了
        if (Random.value > herdSpawnProbability) return;

        // 1. 计算这块地的中心点 (假设 chunkPos 是地块左下角)
        float halfSize = chunkSizeWorld / 2f;
        Vector3 chunkCenter = chunkPos + new Vector3(halfSize, 0, halfSize);

        // 2. 在这块地中心生成一个群体锚点
        GameObject anchorObj = new GameObject("DeerHerd_Dynamic");
        anchorObj.transform.position = chunkCenter;
        anchorObj.transform.SetParent(chunkParent);
        HerdGroup groupAnchor = anchorObj.AddComponent<HerdGroup>();

        // 3. 随机刷 2~3 只鹿
        int herdSize = Random.Range(2, 4);
        for (int j = 0; j < herdSize; j++)
        {
            // 注意看！deerCircle 是在这里生成的，所以后面的代码才能用到它
            Vector2 deerCircle = Random.insideUnitCircle * 3f;

            Vector3 spawnPos = chunkCenter + new Vector3(deerCircle.x, 0, deerCircle.y);

            // 生成鹿
            GameObject deerObj = Instantiate(deerPrefab, spawnPos, Quaternion.identity);
            deerObj.transform.SetParent(chunkParent);

            // 绑定锚点
            DeerAI ai = deerObj.GetComponent<DeerAI>();
            if (ai != null) ai.herdGroupAnchor = groupAnchor.transform;
        }
    }

    // --- 草的管理逻辑 ---

    public void RegisterGrass(Transform grass)
    {
        if (!allGrassList.Contains(grass)) allGrassList.Add(grass);
    }

    public void UnregisterGrass(Transform grass)
    {
        if (allGrassList.Contains(grass)) allGrassList.Remove(grass);
    }

    public Transform GetNearestGrass(Vector3 aiPosition)
    {
        Transform nearest = null;
        float minDistance = float.MaxValue;

        // 【关键】：先清理掉列表里所有已经被 Destroy 的草
        allGrassList.RemoveAll(item => item == null);

        foreach (var grass in allGrassList)
        {
            // 额外双重保险
            if (grass == null) continue;

            float dist = Vector3.Distance(aiPosition, grass.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = grass;
            }
        }
        return nearest;
    }

    /// <summary>
    /// 【安全修复】：草被吃掉后，必须在玩家附近刷新，不能刷到未生成的虚空里去！
    /// </summary>
    public void RespawnGrass()
    {
        if (grassPrefabs == null || grassPrefabs.Length == 0) return;

        // 获取玩家当前坐标作为刷新中心
        Vector3 center = Vector3.zero;
        if (Player_Controller.Instance != null)
        {
            center = Player_Controller.Instance.playerTransform.position;
        }

        int randomIndex = Random.Range(0, grassPrefabs.Length);
        Vector2 randomCircle = Random.insideUnitCircle * grassSpawnRadius;

        // 基于玩家位置偏移
        Vector3 spawnPos = center + new Vector3(randomCircle.x, 0, randomCircle.y);
        Instantiate(grassPrefabs[randomIndex], spawnPos, Quaternion.identity);
    }
}