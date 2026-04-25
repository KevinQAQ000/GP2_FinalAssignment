using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene item generation configuration file (ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "Scene item generation", menuName = "Config/Scene Item Configuration Table")]
public class MapConfig : ScriptableObject
{
    [Header("List of generation rules for different terrains")]
    public List<TerrainSpawnRule> spawnRules = new List<TerrainSpawnRule>();//List of generation rules for different terrain types
}



[Serializable]
public class TerrainSpawnRule
{
    [Tooltip("Specify which terrain type to spawn items on")]
    public MapGrid.MapVertexType terrainType;//Terrain type

    [Header("Item spawn pool for this terrain")]
    public List<MapObjectSpawnConfigModel> spawnModels = new List<MapObjectSpawnConfigModel>();//List of item spawn configurations
}

[Serializable]
public class MapObjectSpawnConfigModel
{
    [Header("Is Empty (Check this to spawn nothing under this probability)")]
    public bool isEmpty = false;//Whether to spawn nothing

    [Tooltip("The 3D model prefab to be spawned")]
    public GameObject prefab;//Item prefab to spawn

    [Header("Spawn Probability (e.g., 30 means 30%)")]
    [Range(0, 100)]
    public int probability;//Spawn probability
}