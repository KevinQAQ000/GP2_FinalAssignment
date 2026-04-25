using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    //Singleton pattern for easy access by MinimapElement
    public static MinimapManager Instance { get; private set; }

    [Header("UI References")]
    public RectTransform mapContent; //Minimap content container; all chunks and icons are placed here

    [Header("Parameters")]
    public float scaleRatio = 5f; //Conversion ratio from world meters to UI pixels

    [Header("Icon Prefabs (UI Image)")] //0-Tree, 1-Rock, 2-Grass
    public GameObject treeIconPrefab; //Image component, Anchor and Pivot should be set to (0.5, 0.5)
    public GameObject rockIconPrefab;
    public GameObject grassIconPrefab;

    [Header("Terrain Settings")]
    [Tooltip("Texture displayed when a chunk is pure forest")]
    public Texture2D defaultForestTexture; //Default texture for pure forest chunks if MapGenerator doesn't provide one

    //Mapping from world Transform objects to their corresponding minimap icons for easy position updates and removal
    //Key: World object Transform, Value: Icon GameObject instance
    private Dictionary<Transform, GameObject> iconDict = new Dictionary<Transform, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        //Update minimap position every frame to keep the player centered
        if (Player_Controller.Instance != null && Player_Controller.Instance.playerTransform != null)
        {
            //Get player's world position
            Vector3 playerPos = Player_Controller.Instance.playerTransform.position;
            //Move the entire container in reverse to keep the player at the center
            mapContent.anchoredPosition = new Vector2(-playerPos.x * scaleRatio, -playerPos.z * scaleRatio);
        }
    }

    /// <summary>
    /// Called when MapGenerator completes a texture to lay the terrain at the bottom of the minimap
    /// </summary>
    public void RegisterChunk(Vector3 worldPos, Texture2D tex, float chunkSizeWorld)
    {
        //Create a new GameObject to display the chunk texture
        GameObject chunkObj = new GameObject("Minimap_Chunk");
        //Place it under mapContent
        chunkObj.transform.SetParent(mapContent);

        //Force as the first sibling to ensure it stays below all icons (trees, rocks)
        chunkObj.transform.SetAsFirstSibling();

        //Use RawImage to display the Texture2D
        RawImage img = chunkObj.AddComponent<RawImage>();
        //Use default texture if MapGenerator provides none (e.g., pure forest)
        img.texture = tex != null ? tex : defaultForestTexture;

        //Configure RectTransform
        RectTransform rt = chunkObj.GetComponent<RectTransform>();
        //Lock anchors to center
        rt.anchorMin = Vector2.one * 0.5f;
        rt.anchorMax = Vector2.one * 0.5f;
        //Since textures are generated from the bottom-left, set Pivot to (0,0)
        rt.pivot = Vector2.zero;

        //Calculate UI size: world meters * scale ratio
        float uiSize = chunkSizeWorld * scaleRatio;
        rt.sizeDelta = new Vector2(uiSize, uiSize);

        //Set position to accurately snap onto the corresponding UI map coordinates
        rt.anchoredPosition = new Vector2(worldPos.x * scaleRatio, worldPos.z * scaleRatio);
    }

    /// <summary>
    /// Register an object on the minimap and generate a corresponding icon
    /// </summary>
    /// <param name="target">The world object's Transform</param>
    /// <param name="objectType">Object type: 0-Tree, 1-Rock, 2-Grass</param>
    public void RegisterObject(Transform target, int objectType)
    {
        GameObject prefab = null; //Select prefab based on object type
        if (objectType == 0) prefab = treeIconPrefab;
        else if (objectType == 1) prefab = rockIconPrefab;
        else if (objectType == 2) prefab = grassIconPrefab;

        if (prefab != null) //If the prefab exists, generate the icon
        {
            //Instantiate icon under mapContent
            GameObject iconObj = Instantiate(prefab, mapContent);
            RectTransform rt = iconObj.GetComponent<RectTransform>();

            //Force anchors/pivot to center to prevent position offset caused by icon size
            rt.anchorMin = Vector2.one * 0.5f;
            rt.anchorMax = Vector2.one * 0.5f;
            rt.pivot = Vector2.one * 0.5f;

            //Convert world coordinates to UI coordinates using the scale ratio
            //Place the icon dot based on the real object's world position
            rt.anchoredPosition = new Vector2(target.position.x * scaleRatio, target.position.z * scaleRatio);
            iconDict.Add(target, iconObj);
        }
    }

    /// <summary>
    /// Unregister an object from the minimap and destroy its icon
    /// </summary>
    public void UnregisterObject(Transform target)
    {
        //If the object exists in the dictionary, destroy its corresponding icon
        if (iconDict.TryGetValue(target, out GameObject obj))
        {
            Destroy(obj);
            iconDict.Remove(target);
        }
    }
}