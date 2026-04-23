using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance { get; private set; }

    [Header("UI 引用")]
    public RectTransform mapContent;

    [Header("参数")]
    public float scaleRatio = 5f;

    [Header("图标预制体 (UI Image)")]
    public GameObject treeIconPrefab;
    public GameObject rockIconPrefab;
    public GameObject grassIconPrefab;

    [Header("地貌设置")]
    [Tooltip("当一个地块是纯森林时显示的贴图")]
    public Texture2D defaultForestTexture;

    private Dictionary<Transform, GameObject> iconDict = new Dictionary<Transform, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        if (Player_Controller.Instance != null && Player_Controller.Instance.playerTransform != null)
        {
            Vector3 playerPos = Player_Controller.Instance.playerTransform.position;
            // 整个容器反向移动，让玩家永远在中心
            mapContent.anchoredPosition = new Vector2(-playerPos.x * scaleRatio, -playerPos.z * scaleRatio);
        }
    }

    /// <summary>
    /// 当 MapGenerator 生成好贴图后调用，将地貌铺在小地图底部
    /// </summary>
    public void RegisterChunk(Vector3 worldPos, Texture2D tex, float chunkSizeWorld)
    {
        GameObject chunkObj = new GameObject("Minimap_Chunk");
        chunkObj.transform.SetParent(mapContent);

        // 关键：强制设为第一个子节点，保证它在所有图标（树、石头）的下面
        chunkObj.transform.SetAsFirstSibling();

        // 使用 RawImage 显示 Texture2D
        RawImage img = chunkObj.AddComponent<RawImage>();
        img.texture = tex != null ? tex : defaultForestTexture;

        RectTransform rt = chunkObj.GetComponent<RectTransform>();
        // 锁定锚点
        rt.anchorMin = Vector2.one * 0.5f;
        rt.anchorMax = Vector2.one * 0.5f;
        // 贴图是以左下角为生成点的，所以 Pivot 设为 (0,0)
        rt.pivot = Vector2.zero;

        // 计算 UI 大小：世界米数 * 比例
        float uiSize = chunkSizeWorld * scaleRatio;
        rt.sizeDelta = new Vector2(uiSize, uiSize);

        // 设置位置
        rt.anchoredPosition = new Vector2(worldPos.x * scaleRatio, worldPos.z * scaleRatio);
    }

    public void RegisterObject(Transform target, int objectType)
    {
        GameObject prefab = null;
        if (objectType == 0) prefab = treeIconPrefab;
        else if (objectType == 1) prefab = rockIconPrefab;
        else if (objectType == 2) prefab = grassIconPrefab;

        if (prefab != null)
        {
            GameObject iconObj = Instantiate(prefab, mapContent);
            RectTransform rt = iconObj.GetComponent<RectTransform>();

            // 强行修正锚点防止偏移
            rt.anchorMin = Vector2.one * 0.5f;
            rt.anchorMax = Vector2.one * 0.5f;
            rt.pivot = Vector2.one * 0.5f;

            rt.anchoredPosition = new Vector2(target.position.x * scaleRatio, target.position.z * scaleRatio);
            iconDict.Add(target, iconObj);
        }
    }

    public void UnregisterObject(Transform target)
    {
        if (iconDict.TryGetValue(target, out GameObject obj))
        {
            Destroy(obj);
            iconDict.Remove(target);
        }
    }
}