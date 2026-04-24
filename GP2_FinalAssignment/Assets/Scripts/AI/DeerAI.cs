using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeerAI : MonoBehaviour
{
    [Header("基础移动")]
    public float moveSpeed = 3f;
    public float wanderRadius = 10f;
    public float eatDistance = 1.5f;

    [Header("生存属性")]
    public float hungerTimer = 0f;
    public float hungerTimerMax = 20f;

    [Header("感官范围")]
    public float playerDetectRange = 3f;
    public float predatorDetectRange = 12f;
    public float fleeSpeedMultiplier = 2.5f;
    public LayerMask threatLayer;

    [Header("引用")]
    public Transform herdGroupAnchor;
    public Animator animator;

    // 内部逻辑变量
    private Vector3 currentTargetPos;
    private Transform targetGrass;
    private bool isWaiting = false;
    private string currentAnimState = "";

    [Header("避障设置")]
    public LayerMask obstacleLayer;
    public float detectionDistance = 2.0f;

    // --- 【物理规范化新增变量】 ---
    private Rigidbody rb;
    private Vector3 targetVelocity; // 大脑计算出的目标速度，交给腿（FixedUpdate）去执行

    private void Awake()
    {
        // 获取刚体组件
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // 1. 生存计时
        hungerTimer += Time.deltaTime;

        // 2. 引用保护
        if (targetGrass == null) targetGrass = null;
        if (herdGroupAnchor == null) herdGroupAnchor = null;

        // 3. 核心行为决策（相当于 AI 的大脑，只思考不走路）
        HandleAIBehavior();
    }
    private void FixedUpdate()
    {
        if (rb != null)
        {
            // 将大脑思考出的 targetVelocity 应用给刚体。
            // 保持原本的 Y 轴速度（重力下落），只改变水平方向的速度
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
    }

    private void HandleAIBehavior()
    {
        if (isWaiting)
        {
            if (CheckPredatorThreat())
            {
                StopAllCoroutines();
                isWaiting = false;
            }
            else return;
        }

        // --- 优先级 1：检测掠食者 ---
        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        foreach (var t in threats)
        {
            if (t.CompareTag("Predator"))
            {
                FleeFrom(t.transform.position, true);
                return;
            }
        }

        // --- 优先级 2：检测玩家 ---
        foreach (var t in threats)
        {
            if (t.CompareTag("Player"))
            {
                float dist = Vector3.Distance(transform.position, t.transform.position);
                if (dist < playerDetectRange)
                {
                    FleeFrom(t.transform.position, false);
                    return;
                }
            }
        }

        // --- 优先级 3：基础生存 ---
        HandleBasicNeeds();
    }

    private bool CheckPredatorThreat()
    {
        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        foreach (var t in threats)
        {
            if (t.CompareTag("Predator")) return true;
        }
        return false;
    }

    private void HandleBasicNeeds()
    {
        if (hungerTimer >= hungerTimerMax)
        {
            if (targetGrass == null) FindNearestGrass();
            else MoveTo(targetGrass.position, true, moveSpeed);
        }
        else
        {
            if (currentTargetPos == Vector3.zero || Vector3.Distance(transform.position, currentTargetPos) < 0.5f)
            {
                PickNewWanderPos();
            }
            else
            {
                MoveTo(currentTargetPos, false, moveSpeed);
            }
        }
    }

    //private void FleeFrom(Vector3 dangerPos, bool isPanic)
    //{
    //    Vector3 fleeDir = (transform.position - dangerPos).normalized;
    //    fleeDir.y = 0;

    //    float speed = isPanic ? moveSpeed * fleeSpeedMultiplier : moveSpeed * 1.2f;

    //    // 关键逻辑：如果是狮子靠近播 run，玩家靠近播 walk
    //    string anim = isPanic ? "run" : "walk";

    //    transform.position += fleeDir * speed * Time.deltaTime;

    //    if (fleeDir != Vector3.zero)
    //        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fleeDir), 0.15f);

    //    PlayAnimation(anim);
    //    currentTargetPos = Vector3.zero; // 重置闲逛目标
    //}
    // 在类顶部加这个变量，用于记录平滑方向
    private Vector3 currentMoveDir;
    private void FleeFrom(Vector3 dangerPos, bool isPanic)
    {
        Vector3 idealFleeDir = (transform.position - dangerPos).normalized;
        idealFleeDir.y = 0;

        // 获取避障滑行方向
        Vector3 targetDir = GetSlideDirection(idealFleeDir);

        float speed = isPanic ? moveSpeed * fleeSpeedMultiplier : moveSpeed * 1.3f;

        // --- 【修改】：不再修改 transform，而是设置目标速度 ---
        targetVelocity = targetDir * speed;

        // 转向依然可以通过 Transform 处理，这不会引起物理冲突
        if (targetDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), 0.15f);
        }

        PlayAnimation(isPanic ? "run" : "walk");
    }


    /// <summary>
    /// 终极避障：利用法线投影实现“贴墙滑行”
    /// </summary>
    private Vector3 GetSlideDirection(Vector3 idealDir)
    {
        if (Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.4f, idealDir, out RaycastHit hit, detectionDistance, obstacleLayer))
        {
            Vector3 slideDir = Vector3.ProjectOnPlane(idealDir, hit.normal).normalized;
            slideDir.y = 0;

            if (slideDir.sqrMagnitude < 0.01f)
            {
                return Quaternion.Euler(0, Random.value > 0.5f ? 90 : -90, 0) * idealDir;
            }
            return slideDir;
        }
        return idealDir;
    }
    //private void MoveTo(Vector3 target, bool isEating, float speed)
    //{
    //    float dist = Vector3.Distance(transform.position, target);
    //    if (dist <= eatDistance)
    //    {
    //        if (isEating) Eat();
    //        else StartCoroutine(WaitAtDestination());
    //        return;
    //    }

    //    Vector3 dir = (target - transform.position).normalized;
    //    dir.y = 0;
    //    transform.position += dir * speed * Time.deltaTime;

    //    if (dir != Vector3.zero)
    //        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f);

    //    PlayAnimation("walk");
    //}
    private void MoveTo(Vector3 target, bool isEating, float speed)
    {
        float dist = Vector3.Distance(transform.position, target);
        if (dist <= eatDistance)
        {
            // --- 【修改】：到达目的地，刹车 ---
            targetVelocity = Vector3.zero;

            if (isEating) Eat();
            else StartCoroutine(WaitAtDestination());
            return;
        }

        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;

        if (!IsObstacleInFront(dir))
        {
            // --- 【修改】：前方无阻，设置移动速度 ---
            targetVelocity = dir * speed;
        }
        else
        {
            // 前方有空气墙，刹车并换个地方逛
            targetVelocity = Vector3.zero;
            if (!isEating) PickNewWanderPos();
        }

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f);

        PlayAnimation("walk");
    }

    // 探测前方是否有空气墙
    private bool IsObstacleInFront(Vector3 direction)
    {
        float deerRadius = 0.5f;
        return Physics.SphereCast(transform.position + Vector3.up * 1f, deerRadius, direction, out _, detectionDistance, obstacleLayer);
    }

    private void Eat()
    {
        if (targetGrass != null)
        {
            Destroy(targetGrass.gameObject);
            targetGrass = null;
            hungerTimer = 0f;
            if (AIManager.Instance != null) AIManager.Instance.RespawnGrass();
            StartCoroutine(WaitAtDestination());
        }
    }

    private void FindNearestGrass()
    {
        if (AIManager.Instance != null)
            targetGrass = AIManager.Instance.GetNearestGrass(transform.position);
    }

    private void PickNewWanderPos()
    {
        Vector3 origin = (herdGroupAnchor != null) ? herdGroupAnchor.position : transform.position;
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        currentTargetPos = origin + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    private IEnumerator WaitAtDestination()
    {
        isWaiting = true;
        // --- 【修改】：待机时确保完全停下 ---
        targetVelocity = Vector3.zero;
        currentTargetPos = Vector3.zero;

        PlayAnimation("idle");
        yield return new WaitForSeconds(Random.Range(2f, 5f));
        isWaiting = false;
    }

    // 动画播放器：使用 CrossFade 实现平滑过渡
    private void PlayAnimation(string animName)
    {
        if (animator != null && currentAnimState != animName)
        {
            currentAnimState = animName;
            animator.CrossFade(animName, 0.2f);
        }
    }

    // 在 Scene 窗口画出感知圆圈，方便调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerDetectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, predatorDetectRange);
    }
}