using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景物品生成配置文件 (ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "Scene item generation", menuName = "Config/Scene Item Configuration Table")]
public class MapConfig : ScriptableObject
{
    [Header("List of generation rules for different terrains")]
    public List<TerrainSpawnRule> spawnRules = new List<TerrainSpawnRule>();
}



[Serializable]
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