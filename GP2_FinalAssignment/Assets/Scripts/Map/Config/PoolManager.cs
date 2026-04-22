using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 原生极简版：对象池管理器 (无缝替代 JKFrame 等第三方插件)
/// </summary>
public class PoolManager : MonoBehaviour
{
    // --- 自动单例模式 ---
    private static PoolManager instance;
    public static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 如果场景里没有，就自动创建一个名为 PoolManager 的隐形物体来充当仓库
                GameObject go = new GameObject("PoolManager");
                instance = go.AddComponent<PoolManager>();
                DontDestroyOnLoad(go); // 保证切换场景时仓库不被销毁
            }
            return instance;
        }
    }

    // --- 核心大仓库 ---
    // 字典的 Key 是预制体的名字，Value 是存放闲置物体的队列 (Queue)
    private Dictionary<string, Queue<GameObject>> poolDic = new Dictionary<string, Queue<GameObject>>();

    /// <summary>
    /// 从对象池拿取物体
    /// </summary>
    public GameObject GetGameObject(GameObject prefab, Transform parent)
    {
        GameObject go;
        string poolKey = prefab.name; // 用预制体的名字作为找东西的钥匙

        // 如果仓库里有这个类型的池子，且池子里还有闲置的物品
        if (poolDic.ContainsKey(poolKey) && poolDic[poolKey].Count > 0)
        {
            go = poolDic[poolKey].Dequeue(); // 从队列里拿出一个
            go.transform.SetParent(parent);  // 放到目标地图块下面
            go.SetActive(true);              // 激活显示
        }
        else
        {
            // 如果仓库被拿空了，或者压根还没建这个池子，就老老实实新造一个
            go = Instantiate(prefab, parent);
            // 【关键】：把生成出来的物体名字改得跟预制体一模一样，去掉后面的(Clone)
            // 这样等它回收到仓库时，我们才知道它属于哪个池子
            go.name = poolKey;
        }

        return go;
    }

    /// <summary>
    /// 把物体塞回对象池
    /// </summary>
    public void PushGameObject(GameObject go)
    {
        string poolKey = go.name; // 通过名字知道它该回哪个池子

        go.SetActive(false); // 隐藏物体
        go.transform.SetParent(this.transform); // 把物体挪到 PoolManager 物体下面，保持场景干净

        // 如果仓库里还没给这种物体建队列，就新建一个
        if (!poolDic.ContainsKey(poolKey))
        {
            poolDic.Add(poolKey, new Queue<GameObject>());
        }

        // 把物体塞进对应的队列里，等下次再用
        poolDic[poolKey].Enqueue(go);
    }
}