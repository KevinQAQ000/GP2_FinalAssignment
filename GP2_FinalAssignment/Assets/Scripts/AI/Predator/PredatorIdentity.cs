using UnityEngine;

public enum PredatorType { Lion, Tiger }

public class PredatorIdentity : MonoBehaviour
{
    public PredatorType type;
    private PredatorAI ai;

    void Awake()
    {
        ai = GetComponent<PredatorAI>();
        ApplyIdentity();
    }

    void ApplyIdentity()
    {
        if (ai == null) return;

        switch (type)
        {
            case PredatorType.Lion:
                // 狮子：草原之王，耐力好，发现距离远，追逐快
                ai.detectRange = 50f;
                ai.sneakRange = 10f;
                ai.chaseRange = 8f;
                ai.runSpeed = 7f;          // 冲刺极快
                ai.hungerThreshold = 40f;
                break;

            case PredatorType.Tiger:
                // 老虎：丛林伏击者，擅长潜行，更早进入潜行状态
                ai.detectRange = 36f;
                ai.sneakRange = 15f;       // 很远就开始潜行压低身体
                ai.chaseRange = 5f;        // 爆发距离短
                ai.runSpeed = 5.5f;
                ai.hungerThreshold = 30f;
                break;
        }
    }
}