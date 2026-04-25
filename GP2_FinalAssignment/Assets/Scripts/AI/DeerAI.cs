using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeerAI : MonoBehaviour
{
    [Header("Basic Movement")]
    public float moveSpeed = 3f;
    public float wanderRadius = 10f;
    public float eatDistance = 1.5f;

    [Header("Survival Attributes")]
    public float hungerTimer = 0f;//Hunger timer; starts searching for food when it reaches the limit
    public float hungerTimerMax = 20f;//Starts searching for food after 20 seconds

    [Header("Sensory Range")]
    public float playerDetectRange = 3f;//Starts fleeing when the player enters this range
    public float predatorDetectRange = 12f;//Starts fleeing when a lion enters this range
    public float fleeSpeedMultiplier = 2.5f;//Speed multiplier during panic fleeing
    public LayerMask threatLayer;//Layer mask for detecting players and lions

    [Header("References")]
    public Transform herdGroupAnchor;//Herd center point used as the origin for random wander points
    public Animator animator;//Animator component

    private Vector3 currentTargetPos;//Current target position for wandering
    private Transform targetGrass;//Current target grass transform
    private bool isWaiting = false;//Whether in waiting state (entered after eating or reaching a wander point)
    private string currentAnimState = "";//Current animation state to avoid redundant calls

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;//Layer mask for obstacle detection
    public float detectionDistance = 2.0f;//Forward detection distance

    //Internal variable for smooth turning
    private Vector3 currentMoveDir;

    private void Update()
    {
        hungerTimer += Time.deltaTime;//Hunger timer continuously increases

        if (targetGrass == null) targetGrass = null;//Ensure targetGrass is null when eaten to trigger search logic
        if (herdGroupAnchor == null) herdGroupAnchor = null;//Ensure anchor is null if destroyed to trigger new wander point generation

        HandleAIBehavior();//Process AI behavior logic
    }

    private void HandleAIBehavior()
    {
        if (isWaiting)
        {
            if (CheckPredatorThreat())//If a threat is detected while waiting, interrupt immediately and flee
            {
                StopAllCoroutines();//Interrupt the waiting coroutine
                isWaiting = false;//Exit waiting state
            }
            else return;
        }

        //Prioritize predator threat; flee immediately if a lion is near
        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        //If an object with the "Predator" tag is detected, execute FleeFrom immediately
        foreach (var t in threats)
        {
            if (t.CompareTag("Predator"))
            {
                FleeFrom(t.transform.position, true);
                return;
            }
        }
        //If no lion threat, check for player threat; flee without panic state if player is close
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

        HandleBasicNeeds();//Handle basic needs (food or wandering) when no threat is present
    }

    private bool CheckPredatorThreat()//Specifically detects predator threats; called during waiting state
    {
        //Only detect predators; player proximity does not interrupt waiting
        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        //Returns true if an object tagged "Predator" is found
        foreach (var t in threats) if (t.CompareTag("Predator")) return true;
        return false;
    }

    private void HandleBasicNeeds()//Handles basic needs, deciding whether to search for food or wander based on hunger
    {
        if (hungerTimer >= hungerTimerMax)//Start searching for food if hunger reaches the limit
        {
            if (targetGrass == null) FindNearestGrass();//Find nearest grass if no target exists
            else MoveTo(targetGrass.position, true, moveSpeed);//Move towards the target grass to eat
        }
        else
        {
            //Perform wandering when there is no hunger pressure
            if (currentTargetPos == Vector3.zero || Vector3.Distance(transform.position, currentTargetPos) < 0.5f)
            {
                PickNewWanderPos();//Generate new wander point if no target exists or current target is reached
            }
            else
            {
                MoveTo(currentTargetPos, false, moveSpeed);//Move towards the wander target
            }
        }
    }

    private void FleeFrom(Vector3 dangerPos, bool isPanic)//Handles fleeing logic; accepts danger position and panic state
    {
        Vector3 idealDir = (transform.position - dangerPos).normalized;//Calculate ideal fleeing direction (away from threat)
        idealDir.y = 0;//Lock Y-axis to ensure movement stays on the horizontal plane

        //Calculate the direction for sliding along walls
        Vector3 targetDir = GetSlideDirection(idealDir);

        //If starting to flee, set direction directly to avoid initial jitter
        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;

        //Smooth turning buffer to prevent twitching
        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 8f);
        currentMoveDir.y = 0; //Strictly lock Y-axis to prevent flying

        //Adjust speed based on panic state; panic speed is faster
        float speed = isPanic ? moveSpeed * fleeSpeedMultiplier : moveSpeed * 1.3f;

        //Move using Transform displacement
        transform.position += currentMoveDir * speed * Time.deltaTime;

        //Play "run" if a lion is near, "walk" if it's a player
        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.15f);

        PlayAnimation(isPanic ? "run" : "walk");
    }

    private void MoveTo(Vector3 target, bool isEating, float speed)//Handles movement logic; accepts target position, purpose, and speed
    {
        //Check if target is reached; if within eating distance, stop and execute logic
        float dist = Vector3.Distance(transform.position, target);
        //If distance is within eating range, stop movement and eat or wait
        if (dist <= eatDistance)
        {
            currentMoveDir = Vector3.zero;//Stop moving upon reaching target
            if (isEating) Eat();//Execute eating logic if purpose is food
            else StartCoroutine(WaitAtDestination());//Enter waiting state if purpose was wandering
            return;
        }

        Vector3 idealDir = (target - transform.position).normalized;//Calculate ideal move direction
        idealDir.y = 0;

        //Change target immediately if hitting a wall while wandering
        if (!isEating && IsObstacleInFront(idealDir))
        {
            //Reset direction and pick a new wander point if an obstacle is ahead to avoid getting stuck
            currentMoveDir = Vector3.zero;
            PickNewWanderPos();
            return;
        }

        //Use wall sliding for normal movement
        Vector3 targetDir = GetSlideDirection(idealDir);
        //Set initial direction to avoid jitter
        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;
        //Smooth turning buffer
        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 8f);
        currentMoveDir.y = 0;

        //Move using Transform displacement
        transform.position += currentMoveDir * speed * Time.deltaTime;

        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.1f);

        PlayAnimation("walk");
    }

    private bool IsObstacleInFront(Vector3 dir)//Detects if an obstacle is ahead using a direction vector
    {
        //Use SphereCast for a wider detection range, suitable for small obstacles
        return Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.4f, dir, out _, detectionDistance, obstacleLayer);
    }

    //Calculates sliding direction; takes ideal direction and returns adjusted direction along the obstacle
    private Vector3 GetSlideDirection(Vector3 idealDir)
    {
        //Use SphereCast to detect obstacles; if hit, calculate a sliding vector along the wall surface
        if (Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.4f, idealDir, out RaycastHit hit, detectionDistance, obstacleLayer))
        {
            //Project the ideal direction onto the plane of the obstacle's surface normal
            Vector3 slideDir = Vector3.ProjectOnPlane(idealDir, hit.normal).normalized;
            slideDir.y = 0;
            if (slideDir.sqrMagnitude < 0.01f)
            {
                //If sliding magnitude is too small (facing wall directly), pick a random perpendicular direction to escape
                return Quaternion.Euler(0, Random.value > 0.5f ? 90 : -90, 0) * idealDir;
            }
            return slideDir;
        }
        return idealDir;
    }

    //Handles eating logic; destroys the grass object and resets state
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

    private void PickNewWanderPos()//Generates a new wander target within a circular area around the herd anchor
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

    //Animation player: uses CrossFade for smooth transitions
    private void PlayAnimation(string animName)
    {
        if (animator != null && currentAnimState != animName)
        {
            currentAnimState = animName;
            animator.CrossFade(animName, 0.2f);
        }
    }

    //Draws perception gizmos in the Scene window for debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerDetectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, predatorDetectRange);
    }
}