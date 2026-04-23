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

        // 【关键保护】：检查锚点是否还在。如果被地图系统销毁了，就把引用设为 null
        // 在 Unity 中，被销毁的物体不等于真正的 null，必须这样显式检查一下
        if (herdGroupAnchor == null)
        {
            herdGroupAnchor = null;
        }

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
        // 如果草已经被销毁了，强制设为 null
        if (targetGrass == null) targetGrass = null;

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
        // 关键保护：如果目标草突然没了，这个节点直接返回失败，让行为树去选别的动作
        if (targetGrass == null && hungerTimer >= hungerTimerMax)
        {
            targetGrass = null; // 彻底清除伪引用
            return NodeState.Failure;
        }

        float dist = Vector3.Distance(transform.position, currentTargetPos);
        if (dist <= eatDistance) return NodeState.Success;

        Vector3 dir = (currentTargetPos - transform.position).normalized;
        dir.y = 0;

        // 雷达避障
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        float bodyRadius = 0.3f;
        float detectDistance = 1.5f;

        if (Physics.SphereCast(rayStart, bodyRadius, dir, out RaycastHit hit, detectDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // 这里也要检查 targetGrass 是否还活着
            if (hit.normal.y < 0.5f && (targetGrass == null || hit.transform != targetGrass))
            {
                dir = Vector3.ProjectOnPlane(dir, hit.normal).normalized;
            }
        }

        transform.position += dir * moveSpeed * Time.deltaTime;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f);

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
        if (isWaiting) return NodeState.Success;

        // 核心检查：如果锚点（家）被销毁了，立即清除引用
        if (herdGroupAnchor == null) herdGroupAnchor = null;

        Vector3 origin;

        if (herdGroupAnchor != null)
        {
            // 只有确定锚点活着，才敢读取它的 position
            origin = herdGroupAnchor.position;
        }
        else
        {
            // 如果家（地块）被刷掉了，就以当前位置为中心，原地散步
            origin = transform.position;
        }

        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        currentTargetPos = origin + new Vector3(randomCircle.x, 0, randomCircle.y);

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