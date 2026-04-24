using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RabbitAI : MonoBehaviour
{
    [Header("基础移动")]
    public float moveSpeed = 1.2f; // 兔子正常走动要慢，像吃草散步
    public float wanderRadius = 6f;
    public float eatDistance = 0.8f;

    [Header("生存属性")]
    public float hungerTimer = 0f;
    public float hungerTimerMax = 20f;

    [Header("感官范围")]
    public float playerDetectRange = 4f;
    public float predatorDetectRange = 12f;
    public float fleeSpeedMultiplier = 3.5f; // 跑路时才爆发出高速度
    public LayerMask threatLayer;

    [Header("群聚行为 (Boids) - 不走散的黄金比例")]
    public bool enableBoids = true;
    public float boidRadius = 8.0f;       // 【扩大视野】：让兔子能看到更远处的同伴，防止断开连接
    public float separationWeight = 1.5f; // 排斥力（防止穿模）
    public float alignmentWeight = 3.0f;  // 对齐力（大家朝同一个方向走的最核心参数）
    public float cohesionWeight = 2.0f;   // 凝聚力（向群体中心靠拢，防止走散）
    public float targetWeight = 1.0f;     // 自身目标的权重

    [Header("引用")]
    public Transform herdGroupAnchor; // 群体公共锚点
    public Animator animator;

    private Vector3 currentTargetPos;
    private Transform targetGrass;
    private bool isWaiting = false;
    private string currentAnimState = "";

    [Header("避障设置")]
    public LayerMask obstacleLayer;
    public float detectionDistance = 1.5f;
    private float raycastHeight = 0.2f;

    [HideInInspector] public Vector3 currentMoveDir; // 公开移动意图供同伴读取

    private void Update()
    {
        hungerTimer += Time.deltaTime;
        if (targetGrass == null) targetGrass = null;

        HandleAIBehavior();
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

        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        foreach (var t in threats)
        {
            if (t == null) continue; // 【新增防报错锁】：无视被销毁的幽灵
            if (t.CompareTag("Predator"))
            {
                FleeFrom(t.transform.position, true);
                return;
            }
        }

        foreach (var t in threats)
        {
            if (t == null) continue; // 【新增防报错锁】
            if (t.CompareTag("Player"))
            {
                if (Vector3.Distance(transform.position, t.transform.position) < playerDetectRange)
                {
                    FleeFrom(t.transform.position, false);
                    return;
                }
            }
        }

        HandleBasicNeeds();
    }

    private bool CheckPredatorThreat()
    {
        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        foreach (var t in threats)
        {
            if (t == null) continue; // 【新增防报错锁】
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
            if (currentTargetPos == Vector3.zero || Vector3.Distance(transform.position, currentTargetPos) < 1.0f)
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
        Vector3 idealDir = (transform.position - dangerPos).normalized;
        idealDir.y = 0;

        Vector3 boidsDir = GetBoidsDirection(idealDir, true);
        Vector3 targetDir = GetSlideDirection(boidsDir);
        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;

        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 10f);
        currentMoveDir.y = 0;

        float speed = isPanic ? moveSpeed * fleeSpeedMultiplier : moveSpeed * 1.5f;
        transform.position += currentMoveDir * speed * Time.deltaTime;

        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.2f);

        PlayAnimation(isPanic ? "run" : "walk");
        currentTargetPos = Vector3.zero;
    }

    private void MoveTo(Vector3 target, bool isEating, float speed)
    {
        float dist = Vector3.Distance(transform.position, target);
        if (dist <= eatDistance)
        {
            currentMoveDir = Vector3.zero;
            if (isEating) Eat();
            else PickNewWanderPos();
            return;
        }

        Vector3 idealDir = (target - transform.position).normalized;
        idealDir.y = 0;

        if (!isEating && IsObstacleInFront(idealDir))
        {
            currentMoveDir = Vector3.zero;
            PickNewWanderPos();
            return;
        }

        Vector3 boidsDir = GetBoidsDirection(idealDir, false);
        Vector3 targetDir = GetSlideDirection(boidsDir);
        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;

        // 放慢转向速度，让群体的动作看起来更从容、丝滑
        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 3.5f);
        currentMoveDir.y = 0;

        transform.position += currentMoveDir * speed * Time.deltaTime;

        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.1f);

        PlayAnimation("walk");
    }

    private Vector3 GetBoidsDirection(Vector3 idealDir, bool isFleeing)
    {
        if (!enableBoids) return idealDir;

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int neighborCount = 0;

        Collider[] neighbors = Physics.OverlapSphere(transform.position + Vector3.up * raycastHeight, boidRadius);
        foreach (var col in neighbors)
        {
            if (col == null) continue; // 【新增防报错锁：这是最容易触发报错的地方】
            if (col.gameObject == this.gameObject) continue;

            RabbitAI otherRabbit = col.GetComponent<RabbitAI>();
            if (otherRabbit != null && otherRabbit.herdGroupAnchor == this.herdGroupAnchor)
            {
                if (otherRabbit.currentMoveDir.sqrMagnitude < 0.1f) continue;

                neighborCount++;
                Vector3 diff = transform.position - otherRabbit.transform.position;
                diff.y = 0;

                float dist = diff.magnitude;
                if (dist > 0)
                {
                    // 使用 Mathf.Max 防止距离过近导致排斥力无限大而引发乱窜
                    separation += diff.normalized / Mathf.Max(dist, 0.2f);
                }

                alignment += otherRabbit.currentMoveDir.normalized;
                cohesion += otherRabbit.transform.position;
            }
        }

        if (neighborCount > 0)
        {
            alignment = (alignment / neighborCount).normalized;
            Vector3 centerOfMass = cohesion / neighborCount;
            cohesion = (centerOfMass - transform.position).normalized;
            cohesion.y = 0;
        }

        float currentTargetWeight = isFleeing ? targetWeight * 4f : targetWeight * 0.2f;

        Vector3 finalDir = (idealDir * currentTargetWeight) +
                           (separation * separationWeight) +
                           (alignment * alignmentWeight) +
                           (cohesion * cohesionWeight);

        finalDir.y = 0;
        return finalDir.normalized;
    }

    private bool IsObstacleInFront(Vector3 direction)
    {
        return Physics.SphereCast(transform.position + Vector3.up * raycastHeight, 0.3f, direction, out _, detectionDistance, obstacleLayer);
    }

    private Vector3 GetSlideDirection(Vector3 idealDir)
    {
        if (Physics.SphereCast(transform.position + Vector3.up * raycastHeight, 0.3f, idealDir, out RaycastHit hit, detectionDistance, obstacleLayer))
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
        Vector3 origin = transform.position;

        // 【核心黑科技】：移动群体锚点，确保永远不会走散
        if (herdGroupAnchor != null)
        {
            // 把公共锚点往大部队前方推，产生“大雁南飞”的流体迁徙感
            Vector3 pushTarget = transform.position + transform.forward * 4f;
            herdGroupAnchor.position = Vector3.Lerp(herdGroupAnchor.position, pushTarget, 0.2f);

            // 所有兔子都在这个“移动中心”附近找随机点
            origin = herdGroupAnchor.position;
        }

        // 随机寻找 3 米内的新目标
        Vector2 randomCircle = Random.insideUnitCircle * 3f;
        currentTargetPos = origin + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    private IEnumerator WaitAtDestination()
    {
        isWaiting = true;
        currentMoveDir = Vector3.zero;
        currentTargetPos = Vector3.zero;
        PlayAnimation("idle");
        yield return new WaitForSeconds(Random.Range(2f, 4f));
        isWaiting = false;
    }

    private void PlayAnimation(string animName)
    {
        if (animator != null && currentAnimState != animName)
        {
            currentAnimState = animName;
            animator.CrossFade(animName, 0.2f);
        }
    }
}