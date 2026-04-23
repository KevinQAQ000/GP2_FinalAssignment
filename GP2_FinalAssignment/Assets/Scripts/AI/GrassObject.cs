using UnityEngine;

public class GrassObject : MonoBehaviour
{
    private void Start()
    {
        // 草刚长出来，赶紧去居委会登记
        if (AIManager.Instance != null)
        {
            AIManager.Instance.RegisterGrass(this.transform);
        }
    }

    private void OnDestroy()
    {
        // 草被销毁（被吃掉或者退出游戏）时注销
        if (AIManager.Instance != null)
        {
            AIManager.Instance.UnregisterGrass(this.transform);
        }
    }
}