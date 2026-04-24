using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredatorAI : MonoBehaviour
{
    public enum PredatorType { Lion, Tiger }

    [Header("��������")]
    public PredatorType identity;

    [Header("�������")]
    public float hungerTimer = 0f;
    public float hungerThreshold = 30f;
    public float fullDuration = 100f;

    [Header("��̬����")]
    public float detectRange;
    public float sneakRange;
    public float chaseRange = 6f;
    public float attackRange = 1.5f;

    [Header("�ƶ��ٶ�")]
    public float walkSpeed = 2f;
    public float sneakSpeed = 1.2f;
    public float runSpeed;

    [Header("Ѳ������")]
    public float patrolRadius = 15f;    // Ѳ�Ӱ뾶
    private Vector3 patrolTarget;       // ��ǰѲ�ӵ�Ŀ�ĵ�
    private bool isWaiting = false;     // �Ƿ���Ŀ�ĵض���

    [Header("����")]
    public Animator animator;
    public LayerMask preyLayer;

    private Transform targetPrey;
    private bool isEating = false;
    private string currentAnim = "";

    [Header("��������")]
    public LayerMask obstacleLayer;       // ����ǽ�Ĳ㼶
    public float detectionDistance = 1.5f; // ����̽����� (������΢����һ�㣬����1.5)

    // --- �������淶������������ ---
    private Rigidbody rb;
    private Vector3 targetVelocity; // ���Լ������Ŀ���ٶȣ������ȣ�FixedUpdate��ȥִ��

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        ApplyIdentitySettings();
        PickNewPatrolPoint(); // ��ʼѡһ��Ѳ�ӵ�
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

        // ���ߺ���
        if (hungerTimer < hungerThreshold)
        {
            PatrolTerritory();
        }
        else
        {
            HuntingLogic();
        }
    }

    private void FixedUpdate()
    {
        if (rb != null && !isEating)
        {
            // ��Ŀ���ٶ�Ӧ�ø����壬���� Y ������
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
    }

    /// <summary>
    /// ���Ѳ���߼�
    /// </summary>
    private void PatrolTerritory()
    {
        if (isWaiting) return;

        float distToTarget = Vector3.Distance(transform.position, patrolTarget);

        if (distToTarget < 1f)
        {
            // ����Ѳ�ӵ㣬ɲ������Ϣ
            targetVelocity = Vector3.zero;
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
        targetVelocity = Vector3.zero; // ȷ���ȴ�ʱ��ȫֹͣ
        PlayAnimation("idle");
        yield return new WaitForSeconds(Random.Range(3f, 7f)); // ����ر�Ե�۲�һ���
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
            PatrolTerritory(); // û�ҵ�����ǰ������һ��Ѳ��һ����
            return;
        }

        float dist = Vector3.Distance(transform.position, targetPrey.position);

        if (dist > detectRange + 5f)
        {
            targetPrey = null;
        }
        else if (dist <= attackRange)
        {
            targetVelocity = Vector3.zero; // ׼��������ɲ��
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

            // �������߼������ (��ǽ��͸��)
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

        // ����Ǵ����й�(walk)״̬��ǰ����ǽ��û��Ҫɵɵ����ǽ���У�ֱ�ӻ���Ŀ��������
        if (anim == "walk" && Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.5f, idealDir, out _, detectionDistance, obstacleLayer))
        {
            targetVelocity = Vector3.zero;
            PickNewPatrolPoint();
            return;
        }

        // ��ɱ/Ǳ��ʱ��ʹ�ø߼���ǽ���б���
        Vector3 targetDir = GetSlideDirection(idealDir);

        // --- ���ĸĶ������ٸ� Transform����Ŀ���ٶ� ---
        targetVelocity = targetDir * speed;

        if (targetDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), 0.15f);

        PlayAnimation(anim);
    }
    private Vector3 GetSlideDirection(Vector3 idealDir)
    {
        if (Physics.SphereCast(transform.position + Vector3.up * 1f, 0.5f, idealDir, out RaycastHit hit, detectionDistance, obstacleLayer))
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
        targetVelocity = Vector3.zero; // �Է�ʱ�����ٶ�
        if (rb != null) rb.linearVelocity = Vector3.zero; // ˫�ر���

        PlayAnimation("eat");
        hungerTimer = -fullDuration;
        yield return new WaitForSeconds(5f);

        isEating = false;
        PickNewPatrolPoint(); // ����ѡһ���µ�Ѳ��
    }
    private void PlayAnimation(string name)
    {
        if (animator != null && currentAnim != name)
        {
            currentAnim = name;
            animator.CrossFade(name, 0.2f);
        }
    }

    // �ڱ༭���ﻭ����ط�Χ���������
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}