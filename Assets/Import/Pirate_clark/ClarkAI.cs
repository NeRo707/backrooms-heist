using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

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

    [Tooltip("Euler angles to rotate the vision cone (e.g. set Y to 90 if model is sideways)")]
    public Vector3 eyeRotationOffset = Vector3.zero;

    [Header("Patrol Settings")]
    [Tooltip("How far the AI is allowed to search for a new point")]
    public float patrolRadius = 40f;
    [Tooltip("The AI MUST pick a point at least this far away (prevents pacing in corners)")]
    public float minPatrolDistance = 15f;
    public float patrolWaitTime = 2f;

    [Header("Current State")]
    [SerializeField] private AIState currentState = AIState.Patrol;

    private float waitTimer;
    private bool isWaiting;

    [Header("Animation Settings")]
    public Animator animator;
    public string speedParamName = "Speed";
    public string isChasingParamName = "IsChasing";

    [Header("Catch / Jumpscare Settings")]
    public float catchDistance = 1.8f;
    public AudioSource entityAudioSource;
    public VideoClip bitingVideoClip;
    private bool isCatching = false;

    void Start()
    {
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Ensure NavMesh has control of rotation for smooth, native movement
        navMeshAgent.updateRotation = true;
        SetNewPatrolDestination();
    }

    void Update()
    {
        if (target == null || isCatching) return; // Stop thinking if catching the player

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

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float currentSpeed = navMeshAgent.velocity.magnitude;
        bool isStationary = isWaiting || currentSpeed < 0.05f || (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending);

        float animSpeed = isStationary ? 0f : (currentSpeed > 0.1f ? currentSpeed : 1.0f);
        bool isChasing = (currentState == AIState.Chase);

        animator.SetFloat(speedParamName, animSpeed);
        animator.SetBool(isChasingParamName, isChasing);
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
        float distToPlayer = Vector3.Distance(transform.position, target.position);

        // 1. Check if we reached catch range
        if (distToPlayer <= catchDistance)
        {
            CatchPlayer();
            return;
        }

        // 2. If player breaks line of sight or gets out of range
        if (!canSeePlayer)
        {
            currentState = AIState.Patrol;
            SetNewPatrolDestination();
            return;
        }

        // 3. Keep chasing player while in sight
        navMeshAgent.SetDestination(target.position);
    }

    private void CatchPlayer()
    {
        if (isCatching) return;
        isCatching = true;

        // Stop the agent from sliding when it catches the player
        navMeshAgent.isStopped = true;
        navMeshAgent.velocity = Vector3.zero;

        Debug.Log("[ClarkAI] Entity caught the player! Stopping all entity audio & triggering biting video.");

        AudioSource[] sources = GetComponentsInChildren<AudioSource>();
        foreach (var src in sources)
        {
            if (src != null)
            {
                src.Stop();
                src.enabled = false;
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSound();
        }

        JumpscareVideoManager.Instance.PlayJumpscare(bitingVideoClip, entityAudioSource, OnJumpscareFinished);
    }

    private void OnJumpscareFinished()
    {
        if (Inventory.Instance != null) Inventory.Instance.ClearCurrentRun();
        if (HeistHUDManager.Instance != null) HeistHUDManager.Instance.ShowToastMessage("<color=#FF4444>BONKED! Caught by Entity — lost all carried loot!</color>");

        var loadingController = FindFirstObjectByType<LoadingScene>();
        if (loadingController != null) loadingController.LoadScene(0);
        else UnityEngine.SceneManagement.SceneManager.LoadScene("Store");
    }

    // Calculates the true forward direction of the eyes based on your offset
    private Vector3 GetEyeForward()
    {
        return transform.rotation * Quaternion.Euler(eyeRotationOffset) * Vector3.forward;
    }

    private bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + eyeOffset;
        Vector3 targetPosition = target.position + Vector3.up; // Aim at player's torso
        Vector3 eyeForward = GetEyeForward();

        Vector3 dirToTarget = (targetPosition - eyePosition).normalized;
        float distToTarget = Vector3.Distance(eyePosition, targetPosition);

        // Check 1: Distance limit
        if (distToTarget > viewDistance) return false;

        // Check 2: Field of view angle limit
        if (Vector3.Angle(eyeForward, dirToTarget) > viewAngle / 2f) return false;

        // Check 3: Raycast to ensure no walls/obstacles block line of sight
        if (Physics.Raycast(eyePosition, dirToTarget, distToTarget, obstacleMask))
        {
            return false;
        }

        return true;
    }

    // --- UPDATED PATROL LOGIC ---
    private void SetNewPatrolDestination()
    {
        // Try up to 10 times to find a wide, distant, reachable point
        for (int i = 0; i < 10; i++)
        {
            // Pick a random direction
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position;

            // Find nearest point on the NavMesh
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                // Check 1: Is this point far enough away? (Prevents him from pacing in corners)
                if (Vector3.Distance(transform.position, hit.position) < minPatrolDistance) continue;

                // Check 2: Can he ACTUALLY walk there? (Prevents choosing points on roofs or behind locked doors)
                NavMeshPath path = new NavMeshPath();
                if (navMeshAgent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    navMeshAgent.SetDestination(hit.position);
                    return; // Found a good point! Break out of the loop.
                }
            }
        }

        // FALLBACK: If he is trapped in a tiny room and cannot find a point far away after 10 tries,
        // just pick any nearby valid point so he doesn't completely freeze.
        Vector3 fallbackDirection = Random.insideUnitSphere * 5f + transform.position;
        if (NavMesh.SamplePosition(fallbackDirection, out NavMeshHit fallbackHit, 5f, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(fallbackHit.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 eyePos = transform.position + eyeOffset;
        Vector3 eyeForward = GetEyeForward();

        // Calculate the left and right edges of the vision cone
        Vector3 leftRay = Quaternion.Euler(0, -viewAngle / 2f, 0) * eyeForward;
        Vector3 rightRay = Quaternion.Euler(0, viewAngle / 2f, 0) * eyeForward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(eyePos, leftRay * viewDistance);
        Gizmos.DrawRay(eyePos, rightRay * viewDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(eyePos, eyeForward * viewDistance);
    }
}
