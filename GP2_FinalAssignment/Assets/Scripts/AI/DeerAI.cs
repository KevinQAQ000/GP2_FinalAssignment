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

    private Vector3 currentTargetPos;
    private Transform targetGrass;
    private bool isWaiting = false;
    private string currentAnimState = "";

    [Header("避障设置")]
    public LayerMask obstacleLayer;
    public float detectionDistance = 2.0f;

    // 用于平滑转向的内部变量
    private Vector3 currentMoveDir;

    private void Update()
    {
        hungerTimer += Time.deltaTime;

        if (targetGrass == null) targetGrass = null;
        if (herdGroupAnchor == null) herdGroupAnchor = null;

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
            if (t.CompareTag("Predator"))
            {
                FleeFrom(t.transform.position, true);
                return;
            }
        }

        foreach (var t in threats)
        {
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
        foreach (var t in threats) if (t.CompareTag("Predator")) return true;
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
    //private Vector3 currentMoveDir;
    private void FleeFrom(Vector3 dangerPos, bool isPanic)
    {
        Vector3 idealDir = (transform.position - dangerPos).normalized;
        idealDir.y = 0;

        // 获得贴墙滑行的方向
        Vector3 targetDir = GetSlideDirection(idealDir);

        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;

        // 平滑转向缓冲，防止抽搐
        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 8f);
        currentMoveDir.y = 0; // 绝对锁死 Y 轴，防止飞天

        float speed = isPanic ? moveSpeed * fleeSpeedMultiplier : moveSpeed * 1.3f;

        // --- 恢复使用 Transform 位移 ---
        transform.position += currentMoveDir * speed * Time.deltaTime;

        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.15f);

        PlayAnimation(isPanic ? "run" : "walk");
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
            currentMoveDir = Vector3.zero;
            if (isEating) Eat();
            else StartCoroutine(WaitAtDestination());
            return;
        }

        Vector3 idealDir = (target - transform.position).normalized;
        idealDir.y = 0;

        // 闲逛撞墙直接换点
        if (!isEating && IsObstacleInFront(idealDir))
        {
            currentMoveDir = Vector3.zero;
            PickNewWanderPos();
            return;
        }

        // 正常移动使用贴墙滑行
        Vector3 targetDir = GetSlideDirection(idealDir);
        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;
        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 8f);
        currentMoveDir.y = 0;

        // --- 恢复使用 Transform 位移 ---
        transform.position += currentMoveDir * speed * Time.deltaTime;

        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.1f);

        PlayAnimation("walk");
    }

    private bool IsObstacleInFront(Vector3 dir)
    {
        return Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.4f, dir, out _, detectionDistance, obstacleLayer);
    }

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
        if (AIManager.Instance != null) targetGrass = AIManager.Instance.GetNearestGrass(transform.position);
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
        currentMoveDir = Vector3.zero;
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