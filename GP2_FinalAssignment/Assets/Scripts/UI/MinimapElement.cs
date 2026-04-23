using UnityEngine;
using System.Collections; // 别忘了引入协程需要的命名空间

/// <summary>
/// 挂在需要显示在小地图上的物品预制体上
/// </summary>
public class MinimapElement : MonoBehaviour
{
    [Tooltip("0代表树，1代表石头，2代表草")]
    public int objectType = 0;

    // 当物品从对象池被拿出来，并 SetActive(true) 时触发
    private void OnEnable()
    {
        StartCoroutine(DelayRegister());
    }

    private IEnumerator DelayRegister()
    {
        // 挂起当前代码，直到这完整的一帧结束
        yield return null;

        if (MinimapManager.Instance != null)
        {
            MinimapManager.Instance.RegisterObject(transform, objectType);
        }
    }

    // 当玩家走远，物品被放回对象池 SetActive(false) 时触发
    private void OnDisable()
    {
        // 卸载是不需要延迟的，隐藏了就立刻从小地图删掉
        if (MinimapManager.Instance != null)
        {
            MinimapManager.Instance.UnregisterObject(transform);
        }
    }
}