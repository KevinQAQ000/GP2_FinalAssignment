using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredatorAI : MonoBehaviour
{
    public enum PredatorType { Lion, Tiger }

    [Header("Identity Settings")]
    public PredatorType identity;//Lion or Tiger, affects specific parameters

    [Header("Survival Parameters")]
    public float hungerTimer = 0f;//Hunger timer
    public float hungerThreshold = 30f;//Starts hunting once hunger exceeds this threshold
    public float fullDuration = 100f;//How long it stays full before getting hungry again

    [Header("Dynamic Attributes")]
    public float detectRange;//Detection range
    public float sneakRange;//Sneaking range
    public float chaseRange = 6f;//Chasing range
    public float attackRange = 1.5f;//Attack range

    [Header("Movement Speed")]
    public float walkSpeed = 2f;//Patrol speed
    public float sneakSpeed = 1.2f;//Sneak speed
    public float runSpeed;//Running speed

    [Header("Patrol Settings")]
    public float patrolRadius = 15f;//Patrol radius
    private Vector3 patrolTarget;//Current patrol target point
    private bool isWaiting = false;//Whether waiting at a patrol point

    [Header("References")]
    public Animator animator;//Animation controller
    public LayerMask preyLayer;//Prey layer

    private Transform targetPrey;//Current target prey
    private bool isEating = false;//Whether currently eating
    private string currentAnim = "";//Current animation state

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;//Obstacle layer
    public float detectionDistance = 1.5f;//Obstacle detection distance

    //Internal variable for smooth turning
    private Vector3 currentMoveDir;

    private void Start()
    {
        ApplyIdentitySettings();
        PickNewPatrolPoint();
    }

    private void ApplyIdentitySettings()//Apply specific parameters based on predator identity
    {
        if (identity == PredatorType.Lion)
        {
            detectRange = 25f; sneakRange = 10f; runSpeed = 7.5f;//Lion has longer vision but shorter sneak range, runs faster
        }
        else
        {
            detectRange = 18f; sneakRange = 15f; runSpeed = 6f;//Tiger has shorter vision but larger sneak range, runs slightly slower
        }
    }

    private void Update()
    {
        if (isEating) return;

        hungerTimer += Time.deltaTime;

        if (hungerTimer < hungerThreshold)//Continue patrolling if not hungry enough to hunt
        {
            PatrolTerritory();
        }
        else
        {
            HuntingLogic();
        }
    }

    private void PatrolTerritory()//Patrol logic
    {
        if (isWaiting) return;
        //Enter waiting state if close enough to the target point
        float distToTarget = Vector3.Distance(transform.position, patrolTarget);
        //If hitting a wall during patrol, immediately pick a new target point
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

    private IEnumerator WaitAtPatrolPoint()//Wait for a random duration at the patrol point
    {
        isWaiting = true;
        PlayAnimation("idle");
        yield return new WaitForSeconds(Random.Range(3f, 7f));
        PickNewPatrolPoint();
        isWaiting = false;
    }

    private void PickNewPatrolPoint()//Select a new random patrol target
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        patrolTarget = transform.position + new Vector3(randomPoint.x, 0, randomPoint.y);
    }

    private void HuntingLogic()
    {
        //Search for prey if no current target exists
        if (targetPrey == null)
        {
            FindPrey();
            PatrolTerritory();
            return;
        }

        //Decide behavior based on distance to the target prey
        float dist = Vector3.Distance(transform.position, targetPrey.position);

        //Give up the chase if the prey is too far away
        if (dist > detectRange + 5f)
        {
            targetPrey = null;
        }
        //Stop and attack if distance is less than 1.5 meters
        else if (dist <= attackRange)
        {
            currentMoveDir = Vector3.zero;
            AttackPrey();
        }
        //Move quickly if within chasing range
        else if (dist <= chaseRange)
        {
            MoveTo(targetPrey.position, runSpeed, "run");
        }
        //Approach slowly if within sneaking range
        else if (dist <= sneakRange)
        {
            MoveTo(targetPrey.position, sneakSpeed, "sneak");
        }
        //Move at normal speed if prey is within sight but not in sneak range
        else
        {
            MoveTo(targetPrey.position, walkSpeed, "walk");
        }
    }

    private void FindPrey()
    {
        //Prey is only discovered if within detect range and not obstructed by obstacles
        Collider[] cols = Physics.OverlapSphere(transform.position, detectRange, preyLayer);
        foreach (var col in cols)
        {
            if (col == null) continue; //Prevent targeting ghost objects just eaten by other predators
            //Calculate direction to prey
            Vector3 dirToPrey = (col.transform.position - transform.position).normalized;
            //Line of sight detection
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPrey, detectRange, obstacleLayer))
            {
                targetPrey = col.transform;
                isWaiting = false;
                StopAllCoroutines();
                break;
            }
        }
    }

    //This method includes obstacle avoidance: randomizes direction if hitting walls during patrol, and slides along walls during hunting
    private void MoveTo(Vector3 pos, float speed, string anim)
    {
        Vector3 idealDir = (pos - transform.position).normalized;
        idealDir.y = 0;

        //Switch target point immediately if hitting a wall during patrol
        if (anim == "walk" && Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.4f, idealDir, out _, detectionDistance, obstacleLayer))
        {
            currentMoveDir = Vector3.zero;
            PickNewPatrolPoint();
            return;
        }

        //Use wall-sliding during hunting
        Vector3 targetDir = GetSlideDirection(idealDir);

        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;

        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 8f);
        currentMoveDir.y = 0;

        //Move using Transform displacement
        transform.position += currentMoveDir * speed * Time.deltaTime;

        //Smooth rotation
        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.15f);

        PlayAnimation(anim);
    }

    private Vector3 GetSlideDirection(Vector3 idealDir)//Calculates a sliding direction if an obstacle is ahead
    {
        //Emit a SphereCast slightly above the center to detect obstacles ahead
        if (Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.4f, idealDir, out RaycastHit hit, detectionDistance, obstacleLayer))
        {
            Vector3 slideDir = Vector3.ProjectOnPlane(idealDir, hit.normal).normalized;
            slideDir.y = 0;
            //If slide direction magnitude is too low (stuck), pick a random perpendicular direction to escape
            if (slideDir.sqrMagnitude < 0.01f)
            {
                return Quaternion.Euler(0, Random.value > 0.5f ? 90 : -90, 0) * idealDir;
            }
            return slideDir;
        }
        return idealDir;
    }

    private void AttackPrey()//Attack prey: destroy the prey object and enter eating state
    {
        if (targetPrey != null)
        {
            Destroy(targetPrey.gameObject);
            targetPrey = null;
            StartCoroutine(EatRoutine());
        }
    }

    private IEnumerator EatRoutine()//Eating coroutine: play eating animation, reset hunger timer, and restore state after a delay
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