using UnityEngine;

public class DeerAI : MonoBehaviour
{
    [Header("鹿的属性")]
    public float moveSpeed = 2f;
    [Tooltip("鹿围绕群体中心散步的范围")]
    public float wanderRadius = 6f;
    [Tooltip("吃草的距离阈值")]
    public float eatDistance = 0.5f;
    [Tooltip("多久饿一次")]
    public float hungerTimerMax = 10f;

    // 【新增】：由 AIManager 在生成时赋值，代表群体的中心
    [Header("AI 引用（不用填）")]
    public Transform herdGroupAnchor;

    private BTNode rootNode;
    private Vector3 currentTargetPos; // AI 移动的目标
    private Transform targetGrass; // 盯上的草

    private float hungerTimer = 0f;
    private bool isWaiting = false; // 散步时的停顿状态
    private float waitTimer = 0f;

    private void Start()
    {
        currentTargetPos = transform.position; // 初始目标设为当前位置

        // 安全检查：防止如果你是手动把鹿拖进场景的，导致它没锚点
        if (herdGroupAnchor == null)
        {
            Debug.LogWarning(gameObject.name + " 没有群体锚点，将围绕自己的出生点散步。");
            // 创建一个临时的出生点锚点，防止报错
            GameObject temp = new GameObject(gameObject.name + "_SelfAnchor");
            temp.transform.position = transform.position;
            herdGroupAnchor = temp.transform;
        }

        ConstructBehaviorTree();
    }

    private void Update()
    {
        hungerTimer += Time.deltaTime;
        float fixedHeight = 0f; 
        transform.position = new Vector3(transform.position.x, fixedHeight, transform.position.z);
        if (rootNode != null)
        {
            rootNode.Evaluate();
        }
    }

    private void ConstructBehaviorTree()
    {
        // 吃草序列
        BTSequence eatGrassSequence = new BTSequence(
            new BTAction(CheckHunger),
            new BTAction(FindGrass),
            new BTAction(MoveToTarget),
            new BTAction(EatGrass)
        );

        // 散步序列
        BTSequence wanderSequence = new BTSequence(
            new BTAction(PickWanderDestination),
            new BTAction(MoveToTarget),
            new BTAction(IdleWait)
        );

        rootNode = new BTSelector(eatGrassSequence, wanderSequence);
    }

    // ==========================================
    // 具体的 AI 行为实现
    // ==========================================

    private NodeState CheckHunger()
    {
        if (hungerTimer >= hungerTimerMax) return NodeState.Success;
        return NodeState.Failure;
    }

    private NodeState FindGrass()
    {
        if (targetGrass != null) return NodeState.Success;
        if (AIManager.Instance == null) return NodeState.Failure;

        targetGrass = AIManager.Instance.GetNearestGrass(transform.position);
        if (targetGrass != null)
        {
            currentTargetPos = targetGrass.position;
            return NodeState.Success;
        }
        return NodeState.Failure;
    }

    private NodeState MoveToTarget()
    {
        float dist = Vector3.Distance(transform.position, currentTargetPos);
        if (dist <= eatDistance) return NodeState.Success;

        Vector3 dir = (currentTargetPos - transform.position).normalized;
        dir.y = 0;

        // ==========================================
        // 【新增】：超声波/触须避障算法 (SphereCast)
        // ==========================================
        Vector3 rayStart = transform.position + Vector3.up * 0.5f; // 从鹿的胸口高度发射（防止扫到地面）
        float bodyRadius = 0.3f; // 鹿的身体宽度，相当于雷达的粗细
        float detectDistance = 1.5f; // 提前多远开始绕路

        // 发射一个圆柱形的射线探测前方，忽略触发器（防止被没有体积的草挡住）
        if (Physics.SphereCast(rayStart, bodyRadius, dir, out RaycastHit hit, detectDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // 安全判断：
            // 1. hit.normal.y < 0.5f 确保撞到的是墙壁或树干（法线朝水平），而不是有坡度的地面
            // 2. hit.transform != targetGrass 确保它不会躲避自己正要去吃的那棵草
            if (hit.normal.y < 0.5f && hit.transform != targetGrass)
            {
                // 【核心数学魔法】：把笔直撞墙的方向，顺着墙壁的切线方向“抹平”，让鹿贴着树干滑过去！
                dir = Vector3.ProjectOnPlane(dir, hit.normal).normalized;
            }
        }
        // ==========================================

        // 执行移动
        transform.position += dir * moveSpeed * Time.deltaTime;

        // 执行转身（转向实际移动的方向）
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f);
        }

        return NodeState.Running;
    }

    private NodeState EatGrass()
    {
        if (targetGrass != null)
        {
            Destroy(targetGrass.gameObject);
            targetGrass = null;
            hungerTimer = 0f;
            if (AIManager.Instance != null) AIManager.Instance.RespawnGrass();
            return NodeState.Success;
        }
        return NodeState.Failure;
    }

    // =========================================================
    // 【修改核心逻辑】：挑选散步目标点改为围绕“群体中心锚点”
    // =========================================================
    private NodeState PickWanderDestination()
    {
        if (isWaiting) return NodeState.Success; // 如果正在发呆，说明目标点已定

        // 安全检查：如果万一没锚点（防止手动拖拽错误），围绕自己当前位置选点
        if (herdGroupAnchor == null)
        {
            Vector2 selfCircle = Random.insideUnitCircle * wanderRadius;
            currentTargetPos = transform.position + new Vector3(selfCircle.x, 0, selfCircle.y);
            return NodeState.Success;
        }

        // --- 核心修改 ---
        // 围绕群体锚点（群中心）选一个随机点
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        currentTargetPos = herdGroupAnchor.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        //Debug.Log(gameObject.name + "选择了新群体散步目标: " + currentTargetPos);
        return NodeState.Success;
    }

    private NodeState IdleWait()
    {
        if (!isWaiting)
        {
            isWaiting = true;
            waitTimer = Random.Range(2f, 5f);
        }
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0)
        {
            isWaiting = false;
            return NodeState.Success;
        }
        return NodeState.Running;
    }
}