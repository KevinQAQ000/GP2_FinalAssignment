using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    //[Tooltip("Probability of a deer herd appearing when a new chunk is generated (0~1)")]
    //public float herdSpawnProbability = 0.2f; //20% probability
    //[Range(0, 1)]
    //public float predatorSpawnProbability = 0.05f; //Very low probability, e.g., 5%

    public static AIManager Instance { get; private set; }//External read-only access to ensure singleton safety

    [Header("Global Lists")]
    public List<Transform> allGrassList = new List<Transform>();

    [Header("Grass Settings")]
    public GameObject[] grassPrefabs;
    public float grassSpawnRadius = 20f;//Spawn range for grass, a circle centered on the player

    [Header("Ecosystem Distribution Probability (0~1)")]
    [Tooltip("Probability of spawning a deer herd")]
    public float deerProbability = 0.2f;
    [Tooltip("Probability of spawning a rabbit herd")]
    public float rabbitProbability = 0.25f;
    [Tooltip("Probability of spawning a lion")]
    public float lionProbability = 0.05f;
    [Tooltip("Probability of spawning a tiger")]
    public float tigerProbability = 0.05f;

    [Header("Prefab References")]
    public GameObject deerPrefab;
    public GameObject rabbitPrefab;
    public GameObject lionPrefab;
    public GameObject tigerPrefab;

    [Header("Herd Count Settings")]
    [Tooltip("Minimum and maximum number of deer in a herd")]
    public int minDeerCount = 2;
    public int maxDeerCount = 4;

    [Tooltip("Minimum and maximum number of rabbits in a herd")]
    public int minRabbitCount = 5;  //Default minimum 5
    public int maxRabbitCount = 12; //Default maximum 12

    private void Awake() => Instance = this;

    //The Start() method has been removed; no more blind global spawning at the beginning!

    /// <summary>
    /// Core Method: Called when MapGenerator prepares a chunk to attempt spawning herds
    /// </summary>
    public void TrySpawnHerdOnChunk(Vector3 chunkPos, float chunkSizeWorld, Transform chunkParent)
    {
        //Calculate chunk center; animals will spawn near this point
        float halfSize = chunkSizeWorld / 2f;
        Vector3 chunkCenter = chunkPos + new Vector3(halfSize, 0, halfSize);


        //Independent check for deer herds
        if (Random.value < deerProbability)
        {
            SpawnDeers(chunkCenter, chunkParent);
        }

        //Independent check for rabbit herds
        if (Random.value < rabbitProbability)
        {
            //Offset rabbits to prevent overlapping with deer
            Vector3 rabbitCenter = chunkCenter + new Vector3(-3f, 0, -3f);
            SpawnRabbits(rabbitCenter, chunkParent);
        }

        //Independent check for lions (Note: must use 'if', never 'else if')
        if (Random.value < lionProbability)
        {
            SpawnSinglePredator(lionPrefab, chunkCenter, chunkParent, "Lion");
        }

        //Independent check for tigers (Note: must use 'if', never 'else if')
        if (Random.value < tigerProbability)
        {
            //Offset tigers to prevent overlapping with lions upon spawning
            Vector3 tigerPos = chunkCenter + new Vector3(4f, 0, 4f);
            SpawnSinglePredator(tigerPrefab, tigerPos, chunkParent, "Tiger");
        }
    }

    //Separate methods to handle lions and tigers respectively, facilitating future expansion of predator types
    private void SpawnSinglePredator(GameObject prefab, Vector3 pos, Transform parent, string debugName)
    {
        if (prefab == null) return;//Safety check to prevent errors if prefab is missing
        GameObject predator = Instantiate(prefab, pos, Quaternion.identity);//Spawn predator
        predator.transform.SetParent(parent);//Set parent to keep hierarchy clean
        //Debug.Log($"Ecosystem: {debugName} spawned on chunk");
    }

    private void SpawnDeers(Vector3 center, Transform parent)
    {
        if (deerPrefab == null) return;

        GameObject anchorObj = new GameObject("DeerHerd_Dynamic");//Create an empty object as the deer herd anchor
        anchorObj.transform.position = center;//Place anchor at chunk center
        anchorObj.transform.SetParent(parent);//Set parent to keep hierarchy clean
        anchorObj.AddComponent<HerdGroup>();//Add HerdGroup component so AIs can find it

        int herdSize = Random.Range(minDeerCount, maxDeerCount + 1);//Randomize herd size
        for (int j = 0; j < herdSize; j++)//Loop to generate each deer
        {
            Vector2 circle = Random.insideUnitCircle * 4f; //Spread them out slightly
            Vector3 spawnPos = center + new Vector3(circle.x, 0, circle.y);//Calculate spawn position based on center plus random offset
            GameObject deer = Instantiate(deerPrefab, spawnPos, Quaternion.identity);//Spawn deer
            deer.transform.SetParent(parent);

            DeerAI ai = deer.GetComponent<DeerAI>();//Get the deer's AI component
            if (ai != null) ai.herdGroupAnchor = anchorObj.transform;//Link the deer's herdGroupAnchor to the generated anchor so it knows its herd
        }
    }

    private void SpawnRabbits(Vector3 center, Transform parent)//Spawn rabbit herd logic, similar to SpawnDeers
    {
        if (rabbitPrefab == null) return;

        GameObject anchorObj = new GameObject("RabbitHerd_Dynamic");
        anchorObj.transform.position = center;
        anchorObj.transform.SetParent(parent);
        anchorObj.AddComponent<HerdGroup>();

        //Use configured variables instead of hardcoded values
        //Note: Unity's integer Random.Range is max-exclusive, so we use +1
        int herdSize = Random.Range(minRabbitCount, maxRabbitCount + 1);

        for (int j = 0; j < herdSize; j++)
        {
            //If there are more rabbits, increase the spawn spread radius slightly
            Vector2 circle = Random.insideUnitCircle * 4f;
            Vector3 spawnPos = center + new Vector3(circle.x, 0, circle.y);
            GameObject rabbit = Instantiate(rabbitPrefab, spawnPos, Quaternion.identity);
            rabbit.transform.SetParent(parent);

            RabbitAI ai = rabbit.GetComponent<RabbitAI>();
            if (ai != null) ai.herdGroupAnchor = anchorObj.transform;
        }
    }


    //Grass Management Logic

    public void RegisterGrass(Transform grass) => allGrassList.Add(grass);//Called by other scripts (e.g., Grass.cs) in Start() to register themselves
    public void UnregisterGrass(Transform grass) => allGrassList.Remove(grass);//Called in OnDestroy() to unregister

    public Transform GetNearestGrass(Vector3 aiPosition)
    {
        //Safe list cleanup: use reverse loop to remove grass that has been eaten (fixes errors)
        for (int i = allGrassList.Count - 1; i >= 0; i--)
        {
            //Unity overrides ==; if object is Destroyed, this evaluates to null
            if (allGrassList[i] == null)
            {
                allGrassList.RemoveAt(i);
            }
        }

        Transform nearest = null;
        float minDistance = float.MaxValue;

        //Traverse to find the nearest grass
        foreach (var grass in allGrassList)
        {
            if (grass == null) continue; //Ultimate safety lock to prevent mid-traversal issues

            float dist = Vector3.Distance(aiPosition, grass.position);//Calculate distance between AI and grass
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = grass;
            }
        }
        return nearest;
    }

    public void RespawnGrass()//Can be called when grass is eaten or on a timer
    {
        if (grassPrefabs.Length == 0) return;//Safety check
        //Randomly spawn grass in a circular area centered on the player
        Vector3 center = Player_Controller.Instance ? Player_Controller.Instance.playerTransform.position : Vector3.zero;
        //Random.insideUnitCircle returns a random point within a radius of 1; multiply by grassSpawnRadius
        Vector2 randomCircle = Random.insideUnitCircle * grassSpawnRadius;
        //Add the random offset to the center position
        Vector3 spawnPos = center + new Vector3(randomCircle.x, 0, randomCircle.y);
        //Instantiate grass
        Instantiate(grassPrefabs[Random.Range(0, grassPrefabs.Length)], spawnPos, Quaternion.identity);
    }
}