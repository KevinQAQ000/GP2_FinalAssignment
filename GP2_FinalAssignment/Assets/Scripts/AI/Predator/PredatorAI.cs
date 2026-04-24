using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredatorAI : MonoBehaviour
{
    public enum PredatorType { Lion, Tiger }

    [Header("身份设置")]
    public PredatorType identity;

    [Header("生存参数")]
    public float hungerTimer = 0f;
    public float hungerThreshold = 30f;
    public float fullDuration = 100f;

    [Header("动态属性")]
    public float detectRange;
    public float sneakRange;
    public float chaseRange = 6f;
    public float attackRange = 1.5f;

    [Header("移动速度")]
    public float walkSpeed = 2f;
    public float sneakSpeed = 1.2f;
    public float runSpeed;

    [Header("巡视设置")]
    public float patrolRadius = 15f;
    private Vector3 patrolTarget;
    private bool isWaiting = false;

    [Header("引用")]
    public Animator animator;
    public LayerMask preyLayer;

    private Transform targetPrey;
    private bool isEating = false;
    private string currentAnim = "";

    [Header("避障设置")]
    public LayerMask obstacleLayer;
    public float detectionDistance = 1.5f;

    // 用于平滑转向的内部变量
    private Vector3 currentMoveDir;

    private void Start()
    {
        ApplyIdentitySettings();
        PickNewPatrolPoint();
    }

    private void ApplyIdentitySettings()
    {
        if (identity == PredatorType.Lion)
        {
            detectRange = 25f; sneakRange = 10f; runSpeed = 7.5f;
        }
        else
        {
            detectRange = 18f; sneakRange = 15f; runSpeed = 6f;
        }
    }

    private void Update()
    {
        if (isEating) return;

        hungerTimer += Time.deltaTime;

        if (hungerTimer < hungerThreshold)
        {
            PatrolTerritory();
        }
        else
        {
            HuntingLogic();
        }
    }

    private void PatrolTerritory()
    {
        if (isWaiting) return;

        float distToTarget = Vector3.Distance(transform.position, patrolTarget);

        if (distToTarget < 1f)
        {
            currentMoveDir = Vector3.zero;
            StartCoroutine(WaitAtPatrolPoint());
        }
        else
        {
            MoveTo(patrolTarget, walkSpeed, "walk");
        }
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        PlayAnimation("idle");
        yield return new WaitForSeconds(Random.Range(3f, 7f));
        PickNewPatrolPoint();
        isWaiting = false;
    }

    private void PickNewPatrolPoint()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        patrolTarget = transform.position + new Vector3(randomPoint.x, 0, randomPoint.y);
    }

    private void HuntingLogic()
    {
        if (targetPrey == null)
        {
            FindPrey();
            PatrolTerritory();
            return;
        }

        float dist = Vector3.Distance(transform.position, targetPrey.position);

        if (dist > detectRange + 5f)
        {
            targetPrey = null;
        }
        else if (dist <= attackRange)
        {
            currentMoveDir = Vector3.zero;
            AttackPrey();
        }
        else if (dist <= chaseRange)
        {
            MoveTo(targetPrey.position, runSpeed, "run");
        }
        else if (dist <= sneakRange)
        {
            MoveTo(targetPrey.position, sneakSpeed, "sneak");
        }
        else
        {
            MoveTo(targetPrey.position, walkSpeed, "walk");
        }
    }

    //private void FindPrey()
    //{
    //    Collider[] cols = Physics.OverlapSphere(transform.position, detectRange, preyLayer);
    //    if (cols.Length > 0)
    //    {
    //        targetPrey = cols[0].transform;
    //        isWaiting = false; // �����������������ȴ�״̬
    //        StopAllCoroutines();
    //    }
    //}

    private void FindPrey()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, detectRange, preyLayer);
        foreach (var col in cols)
        {
            Vector3 dirToPrey = (col.transform.position - transform.position).normalized;
            // 视线遮挡检测
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPrey, detectRange, obstacleLayer))
            {
                targetPrey = col.transform;
                isWaiting = false;
                StopAllCoroutines();
                break;
            }
        }
    }

    //private void MoveTo(Vector3 pos, float speed, string anim)
    //{
    //    Vector3 dir = (pos - transform.position).normalized;
    //    dir.y = 0;
    //    transform.position += dir * speed * Time.deltaTime;

    //    if (dir != Vector3.zero)
    //        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f);

    //    PlayAnimation(anim);
    //}
    private void MoveTo(Vector3 pos, float speed, string anim)
    {
        Vector3 idealDir = (pos - transform.position).normalized;
        idealDir.y = 0;

        // 如果巡逻时撞墙，直接换个目标点
        if (anim == "walk" && Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.4f, idealDir, out _, detectionDistance, obstacleLayer))
        {
            currentMoveDir = Vector3.zero;
            PickNewPatrolPoint();
            return;
        }

        // 猎杀时，使用贴墙滑行
        Vector3 targetDir = GetSlideDirection(idealDir);

        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;

        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 8f);
        currentMoveDir.y = 0;

        // --- 恢复使用 Transform 位移 ---
        transform.position += currentMoveDir * speed * Time.deltaTime;

        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.15f);

        PlayAnimation(anim);
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

    //private bool IsObstacleInFront(Vector3 direction)
    //{
    //    return Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, detectionDistance, obstacleLayer);
    //}

    private void AttackPrey()
    {
        if (targetPrey != null)
        {
            Destroy(targetPrey.gameObject);
            targetPrey = null;
            StartCoroutine(EatRoutine());
        }
    }

    private IEnumerator EatRoutine()
    {
        isEating = true;
        currentMoveDir = Vector3.zero;
        PlayAnimation("eat");
        hungerTimer = -fullDuration;
        yield return new WaitForSeconds(5f);
        isEating = false;
        PickNewPatrolPoint();
    }

    private void PlayAnimation(string name)
    {
        if (animator != null && currentAnim != name)
        {
            currentAnim = name;
            animator.CrossFade(name, 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}