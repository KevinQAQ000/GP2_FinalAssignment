using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RabbitAI : MonoBehaviour
{
    [Header("Basic Movement")]
    public float moveSpeed = 1.2f; //Normal movement speed for walking/grazing
    public float wanderRadius = 6f;//Random wandering range, expanded for higher activity
    public float eatDistance = 0.8f;//Distance to trigger eating, adjusted for smoother action

    [Header("Survival Attributes")]
    public float hungerTimer = 0f;
    public float hungerTimerMax = 20f;

    [Header("Sensory Range")]
    public float playerDetectRange = 4f;//Alert range for players, making escape behavior easier to trigger
    public float predatorDetectRange = 12f;//Alert range for predators, expanded to detect danger earlier
    public float fleeSpeedMultiplier = 3.5f; //High speed burst during escape
    public LayerMask threatLayer;

    [Header("Boids Behavior - Grouping Logic")]
    public bool enableBoids = true;
    public float boidRadius = 8.0f;       //Detection range for neighbors to prevent group disconnection
    public float separationWeight = 1.5f; //Repulsion force to prevent overlapping
    public float alignmentWeight = 3.0f;  //Alignment force for moving in the same direction
    public float cohesionWeight = 2.0f;   //Cohesion force to stay near the group center
    public float targetWeight = 1.0f;     //Weight of the individual's own target

    [Header("References")]
    public Transform herdGroupAnchor; //Shared group anchor
    public Animator animator;

    private Vector3 currentTargetPos;//Current target: grass position or random wander point
    private Transform targetGrass;//Currently locked grass target
    private bool isWaiting = false;//Waiting state while eating to prevent frequent target switching
    private string currentAnimState = "";//Current animation state to avoid redundant triggers

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;//Layer for obstacles
    public float detectionDistance = 1.5f;//Forward detection range to discover obstacles earlier
    private float raycastHeight = 0.2f;//Raycast height to ensure small ground obstacles are detected

    [HideInInspector] public Vector3 currentMoveDir; //Public intent for neighbors to read

    private void Update()
    {
        //Hunger timer increases; searches for grass when threshold is met
        hungerTimer += Time.deltaTime;
        if (targetGrass == null) targetGrass = null;

        HandleAIBehavior();
    }

    private void HandleAIBehavior()//Makes decisions based on state and environment
    {
        if (isWaiting)//Priority check for predators while eating
        {
            if (CheckPredatorThreat())
            {
                StopAllCoroutines();
                isWaiting = false;
            }
            else return;
        }

        //Check for predator threats first
        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        //Check predators then players to prioritize survival responses
        foreach (var t in threats)
        {
            if (t == null) continue; //Safe lock for destroyed objects
            if (t.CompareTag("Predator"))
            {
                FleeFrom(t.transform.position, true);
                return;
            }
        }
        //Check for player threats next; player proximity may not trigger full panic
        foreach (var t in threats)
        {
            if (t == null) continue; //Safe lock
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

    private bool CheckPredatorThreat()//Specifically checks for predators during grazing
    {
        Collider[] threats = Physics.OverlapSphere(transform.position, predatorDetectRange, threatLayer);
        foreach (var t in threats)
        {
            //Ignore destroyed ghost objects
            if (t == null) continue;
            if (t.CompareTag("Predator")) return true;
        }
        return false;
    }

    private void HandleBasicNeeds()//Decides between grazing and wandering based on hunger
    {
        if (hungerTimer >= hungerTimerMax)//Prioritize food when hungry
        {
            if (targetGrass == null) FindNearestGrass();//Find nearest grass if no target
            else MoveTo(targetGrass.position, true, moveSpeed);//Move to target; less frequent target switching during eating
        }
        else
        {
            //Wander when not hungry for environmental interaction
            if (currentTargetPos == Vector3.zero || Vector3.Distance(transform.position, currentTargetPos) < 1.0f)
            {
                PickNewWanderPos();//Pick new point when close to target
            }
            else
            {
                MoveTo(currentTargetPos, false, moveSpeed);//Move towards random point with normal avoidance
            }
        }
    }

    private void FleeFrom(Vector3 dangerPos, bool isPanic)//Escape from threat; isPanic determines speed and animation
    {
        //Ideal direction is directly away from danger
        Vector3 idealDir = (transform.position - dangerPos).normalized;
        //Maintain a consistent direction for escape
        idealDir.y = 0;

        //Consider neighbor influence but prioritize the escape vector
        Vector3 boidsDir = GetBoidsDirection(idealDir, true);
        Vector3 targetDir = GetSlideDirection(boidsDir);
        //If no current direction, use target direction immediately to prevent getting stuck
        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;

        //Slower interpolation for smoother, more fluid escape turns
        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 10f);
        currentMoveDir.y = 0;

        //Speed increase based on panic; full panic only for predators
        float speed = isPanic ? moveSpeed * fleeSpeedMultiplier : moveSpeed * 1.5f;
        transform.position += currentMoveDir * speed * Time.deltaTime;

        //Faster rotation during escape
        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.2f);

        PlayAnimation(isPanic ? "run" : "walk");
        currentTargetPos = Vector3.zero;
    }

    private void MoveTo(Vector3 target, bool isEating, float speed)//Move towards target; isEating affects avoidance and animation
    {
        //Check distance to target; stop to eat or pick new wander point if close enough
        float dist = Vector3.Distance(transform.position, target);
        if (dist <= eatDistance)
        {
            currentMoveDir = Vector3.zero;
            if (isEating)
                Eat();
            else
                PickNewWanderPos();
            return;
        }

        //Calculate ideal direction towards target
        Vector3 idealDir = (target - transform.position).normalized;
        idealDir.y = 0;

        //If wandering and hit a wall, immediately pick a new point
        if (!isEating && IsObstacleInFront(idealDir))
        {
            currentMoveDir = Vector3.zero;
            PickNewWanderPos();
            return;
        }

        //Apply Boids and sliding avoidance
        Vector3 boidsDir = GetBoidsDirection(idealDir, false);
        Vector3 targetDir = GetSlideDirection(boidsDir);
        if (currentMoveDir == Vector3.zero) currentMoveDir = targetDir;

        //Smooth turning for group movement
        currentMoveDir = Vector3.Slerp(currentMoveDir, targetDir, Time.deltaTime * 3.5f);
        currentMoveDir.y = 0;

        //Apply movement
        transform.position += currentMoveDir * speed * Time.deltaTime;


        if (currentMoveDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), 0.1f);

        PlayAnimation("walk");
    }

    //Calculate final Boids direction; isFleeing adjusts weights to prioritize escape over group cohesion
    private Vector3 GetBoidsDirection(Vector3 idealDir, bool isFleeing)
    {
        //Return ideal direction if Boids is disabled
        if (!enableBoids) return idealDir;

        //Calculate separation, alignment, and cohesion
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int neighborCount = 0;

        //Detect neighbors within radius
        Collider[] neighbors = Physics.OverlapSphere(transform.position + Vector3.up * raycastHeight, boidRadius);
        foreach (var col in neighbors)
        {
            //Ignore destroyed objects and self
            if (col == null) continue;
            if (col.gameObject == this.gameObject) continue;

            //Only consider companions from the same herd
            RabbitAI otherRabbit = col.GetComponent<RabbitAI>();
            if (otherRabbit != null && otherRabbit.herdGroupAnchor == this.herdGroupAnchor)
            {
                //Only consider neighbors with clear movement to maintain group activity
                if (otherRabbit.currentMoveDir.sqrMagnitude < 0.1f) continue;

                neighborCount++;
                Vector3 diff = transform.position - otherRabbit.transform.position;
                diff.y = 0;

                float dist = diff.magnitude;
                if (dist > 0)
                {
                    //Apply separation with a minimum distance clamp to prevent explosive repulsion
                    separation += diff.normalized / Mathf.Max(dist, 0.2f);
                }

                alignment += otherRabbit.currentMoveDir.normalized;//Average neighbor direction
                cohesion += otherRabbit.transform.position;//Group center calculation
            }
        }

        //Average neighbors vectors
        if (neighborCount > 0)
        {
            alignment = (alignment / neighborCount).normalized;
            Vector3 centerOfMass = cohesion / neighborCount;
            cohesion = (centerOfMass - transform.position).normalized;
            cohesion.y = 0;
        }

        float currentTargetWeight = isFleeing ? targetWeight * 4f : targetWeight * 0.2f;//Prioritize target when fleeing

        //Blend all Boids forces with the ideal direction
        Vector3 finalDir = (idealDir * currentTargetWeight) +
                           (separation * separationWeight) +
                           (alignment * alignmentWeight) +
                           (cohesion * cohesionWeight);

        finalDir.y = 0;
        return finalDir.normalized;
    }

    private bool IsObstacleInFront(Vector3 direction)//SphereCast to detect ground obstacles more reliably
    {
        return Physics.SphereCast(transform.position + Vector3.up * raycastHeight, 0.3f, direction, out _, detectionDistance, obstacleLayer);
    }

    private Vector3 GetSlideDirection(Vector3 idealDir)
    {
        //Calculate slide vector along obstacle surfaces
        if (Physics.SphereCast(transform.position + Vector3.up * raycastHeight, 0.3f, idealDir, out RaycastHit hit, detectionDistance, obstacleLayer))
        {
            Vector3 slideDir = Vector3.ProjectOnPlane(idealDir, hit.normal).normalized;
            slideDir.y = 0;
            if (slideDir.sqrMagnitude < 0.01f)
            {
                //If direct collision, pick a perpendicular escape direction
                return Quaternion.Euler(0, Random.value > 0.5f ? 90 : -90, 0) * idealDir;
            }
            return slideDir;
        }
        return idealDir;
    }

    private void Eat()//Eating logic: destroy target, reset hunger, and start waiting
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

    private void FindNearestGrass()//Query AIManager for the closest food target
    {
        if (AIManager.Instance != null)
            targetGrass = AIManager.Instance.GetNearestGrass(transform.position);
    }

    //Pick a new wander target based on group anchor to ensure the herd migrates together
    private void PickNewWanderPos()
    {
        Vector3 origin = transform.position;

        //Move the group anchor forward to create a fluid migration effect
        if (herdGroupAnchor != null)
        {
            Vector3 pushTarget = transform.position + transform.forward * 4f;
            herdGroupAnchor.position = Vector3.Lerp(herdGroupAnchor.position, pushTarget, 0.2f);

            //All rabbits find points relative to this moving center
            origin = herdGroupAnchor.position;
        }

        //Randomize new target within 3 meters
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