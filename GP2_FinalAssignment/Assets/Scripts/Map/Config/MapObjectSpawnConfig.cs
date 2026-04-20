using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景物品生成配置文件 (ScriptableObject)
/// 使用方法：在 Project 窗口右键 -> Create -> Config -> 场景物品配置表
/// </summary>
[CreateAssetMenu(fileName = "场景物品生成配置", menuName = "Config/场景物品配置表")]
public class MapObjectSpawnConfig : ScriptableObject
{
    [Header("不同地形的生成规则列表")]
    // 用 List 替代 Dictionary，完美兼容 Unity 原生面板
    public List<TerrainSpawnRule> spawnRules = new List<TerrainSpawnRule>();
}

// ---------------- 内部数据结构 ----------------

[Serializable] // 必须加上这个标签，Unity 面板才会显示这个类
public class TerrainSpawnRule
{
    [Tooltip("指定在哪种地形上生成物品")]
    public MapGrid.MapVertexType terrainType;

    [Header("该地形下的物品生成池")]
    public List<MapObjectSpawnConfigModel> spawnModels = new List<MapObjectSpawnConfigModel>();
}

[Serializable]
public class MapObjectSpawnConfigModel
{
    [Header("是否为空 (勾选代表在这个概率下不生成任何东西)")]
    public bool isEmpty = false;

    [Tooltip("要生成的 3D 模型预制体")]
    public GameObject prefab;

    [Header("生成概率 (如填写 30 代表 30%)")]
    [Range(0, 100)]
    public int probability;
}