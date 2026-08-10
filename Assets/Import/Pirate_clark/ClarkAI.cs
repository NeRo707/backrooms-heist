using UnityEngine;
using UnityEngine.AI;

public class ClarkAI : MonoBehaviour
{
    public enum AIState { Patrol, Chase }

    [Header("References")]
    public NavMeshAgent navMeshAgent;
    public Transform target;

    [Header("Vision Settings")]
    public float viewDistance = 12f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask obstacleMask; // Select layers that block vision (e.g., Default, Environment)
    public Vector3 eyeOffset = new Vector3(0, 1.5f, 0); // Height of eyes above origin

    [Header("Patrol Settings")]
    public float patrolRadius = 15f;
    public float patrolWaitTime = 2f;

    [Header("Current State")]
    [SerializeField] private AIState currentState = AIState.Patrol;

    private float waitTimer;
    private bool isWaiting;

    void Start()
    {
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        SetNewPatrolDestination();
    }

    void Update()
    {
        if (target == null) return;

        bool canSeePlayer = CanSeePlayer();

        switch (currentState)
        {
            case AIState.Patrol:
                HandlePatrolState(canSeePlayer);
                break;

            case AIState.Chase:
                HandleChaseState(canSeePlayer);
                break;
        }
    }

    private void HandlePatrolState(bool canSeePlayer)
    {
        // 1. Check if we spot the player
        if (canSeePlayer)
        {
            currentState = AIState.Chase;
            isWaiting = false;
            return;
        }

        // 2. Otherwise, stroll around
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = patrolWaitTime;
            }

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                SetNewPatrolDestination();
            }
        }
    }

    private void HandleChaseState(bool canSeePlayer)
    {
        // 1. If player breaks line of sight or gets out of range
        if (!canSeePlayer)
        {
            currentState = AIState.Patrol;
            SetNewPatrolDestination();
            return;
        }

        // 2. Keep chasing player while in sight
        navMeshAgent.SetDestination(target.position);
    }

    private bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + eyeOffset;
        Vector3 targetPosition = target.position + Vector3.up; // Aim at player's torso
        Vector3 dirToTarget = (targetPosition - eyePosition).normalized;
        float distToTarget = Vector3.Distance(eyePosition, targetPosition);

        // Check 1: Distance limit
        if (distToTarget > viewDistance) return false;

        // Check 2: Field of view angle limit
        if (Vector3.Angle(transform.forward, dirToTarget) > viewAngle / 2f) return false;

        // Check 3: Raycast to ensure no walls/obstacles block line of sight
        if (Physics.Raycast(eyePosition, dirToTarget, distToTarget, obstacleMask))
        {
            return false; // Vision blocked by wall/cover
        }

        return true; // All vision conditions passed
    }

    private void SetNewPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        // Finds the closest valid spot on the NavMesh near the random direction
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
    }

    // Visualizes the vision cone in the Unity Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 eyePos = transform.position + eyeOffset;
        Vector3 leftRay = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(eyePos, leftRay * viewDistance);
        Gizmos.DrawRay(eyePos, rightRay * viewDistance);
    }
}
