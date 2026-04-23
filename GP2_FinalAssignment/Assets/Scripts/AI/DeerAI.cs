using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float playerDetectRange = 3f;    // 玩家靠近时的避让距离（已缩小一半）
    public float predatorDetectRange = 12f; // 掠食者感知距离
    public float fleeSpeedMultiplier = 2.5f; // 逃离掠食者时的速度倍率
    public LayerMask threatLayer;            // 威胁层（包含 Player 和 Predator）

    [Header("引用")]
    public Transform herdGroupAnchor;   // 群体锚点
    public Animator animator;

    // 内部逻辑变量
    private Vector3 currentTargetPos;
    private Transform targetGrass;
    private bool isWaiting = false;
    private string currentAnimState = ""; // 记录当前动画，防止重复播放

    private void Update()
    {
        // 1. 生存计时
        hungerTimer += Time.deltaTime;

        // 2. 引用保护：防止地块卸载导致空引用
        if (targetGrass == null) targetGrass = null;
        if (herdGroupAnchor == null) herdGroupAnchor = null;

        // 3. 核心行为决策
        HandleAIBehavior();
    }

    private void HandleAIBehavior()
    {
        // 如果正在原地待机，只有发现掠食者才能打断
        if (isWaiting)
        {
            if (CheckPredatorThreat())
            {
                StopAllCoroutines();
                isWaiting = false;
            }
            else return;
        }

        // --- 优先级 1：检测掠食者 (狮子/老虎) ---
        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        foreach (var t in threats)
        {
            if (t.CompareTag("Predator"))
            {
                FleeFrom(t.transform.position, true); // 恐慌逃跑模式
                return;
            }
        }

        // --- 优先级 2：检测玩家 (缓慢避让) ---
        foreach (var t in threats)
        {
            if (t.CompareTag("Player"))
            {
                float dist = Vector3.Distance(transform.position, t.transform.position);
                if (dist < playerDetectRange)
                {
                    FleeFrom(t.transform.position, false); // 避让模式
                    return;
                }
            }
        }

        // --- 优先级 3：基础生存逻辑 ---
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

    private void FleeFrom(Vector3 dangerPos, bool isPanic)
    {
        Vector3 fleeDir = (transform.position - dangerPos).normalized;
        fleeDir.y = 0;

        float speed = isPanic ? moveSpeed * fleeSpeedMultiplier : moveSpeed * 1.2f;

        // 关键逻辑：如果是狮子靠近播 run，玩家靠近播 walk
        string anim = isPanic ? "run" : "walk";

        transform.position += fleeDir * speed * Time.deltaTime;

        if (fleeDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fleeDir), 0.15f);

        PlayAnimation(anim);
        currentTargetPos = Vector3.zero; // 重置闲逛目标
    }

    private void MoveTo(Vector3 target, bool isEating, float speed)
    {
        float dist = Vector3.Distance(transform.position, target);
        if (dist <= eatDistance)
        {
            if (isEating) Eat();
            else StartCoroutine(WaitAtDestination());
            return;
        }

        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        transform.position += dir * speed * Time.deltaTime;

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f);

        PlayAnimation("walk");
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
        // 画出玩家感知范围（蓝色）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerDetectRange);

        // 画出掠食者感知范围（红色）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, predatorDetectRange);
    }
}