using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

[RequireComponent(typeof(NavMeshAgent))]
public class WeepingAngelAI : MonoBehaviour {
  private enum State { Wander, Chase, Grace }

  [Header("References")]
  public Transform player;
  public Camera playerCamera;
  [Tooltip("Layers that block line of sight (e.g., Walls, Floors)")]
  public LayerMask obstacleMask;

  [Header("Catch Settings")]
  public float catchDistance = 1.8f;
  public AudioSource entityAudioSource;
  public VideoClip bitingVideoClip;

  [Header("Detection")]
  public float aggroRadius = 25f;   // starts chasing inside this
  public float loseRadius = 35f;    // gives up chase beyond this

  [Header("Grace (static after being seen)")]
  public float graceMin = 10f;
  public float graceMax = 15f;

  [Header("Wander")]
  public float wanderMin = 8f;      // min time before it may chase again
  public float wanderMax = 15f;
  public float wanderRadius = 12f;
  public float wanderSpeed = 1.5f;

  [Header("Chase")]
  public float chaseSpeed = 3.5f;

  public bool IsCatching { get; private set; } = false;

  private NavMeshAgent _agent;
  private Collider _collider;
  private State _state;
  private float _stateEndTime;

  private void Start() {
    _agent = GetComponent<NavMeshAgent>();
    _collider = GetComponent<Collider>();
    if (playerCamera == null) playerCamera = Camera.main;
    EnterWander();
  }

  private void Update() {
    if (player == null || IsCatching) return;

    float dist = Vector3.Distance(transform.position, player.position);
    bool seen = IsSeenByPlayer();

    switch (_state) {
      case State.Grace:
        Freeze();
        if (Time.time >= _stateEndTime) EnterWander();
        break;

      case State.Wander:
        if (seen) { Freeze(); break; } // still obeys the angel rule
        _agent.isStopped = false;

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f)
          PickWanderTarget();

        if (Time.time >= _stateEndTime) {
          if (dist <= aggroRadius) EnterChase();
          else _stateEndTime = Time.time + Random.Range(wanderMin, wanderMax);
        }
        break;

      case State.Chase:
        if (dist <= catchDistance) { CatchPlayer(); break; }
        if (dist > loseRadius) { EnterWander(); break; }
        if (seen) { EnterGrace(); break; }

        _agent.isStopped = false;
        _agent.SetDestination(player.position);
        break;
    }
  }

  private void EnterWander() {
    _state = State.Wander;
    _stateEndTime = Time.time + Random.Range(wanderMin, wanderMax);
    _agent.speed = wanderSpeed;
    _agent.isStopped = false;
    PickWanderTarget();
  }

  private void EnterChase() {
    _state = State.Chase;
    _agent.speed = chaseSpeed;
    _agent.isStopped = false;
  }

  private void EnterGrace() {
    _state = State.Grace;
    _stateEndTime = Time.time + Random.Range(graceMin, graceMax);
    Freeze();
  }

  private void Freeze() {
    _agent.isStopped = true;
    _agent.velocity = Vector3.zero;
  }

  private void PickWanderTarget() {
    Vector3 random = transform.position + Random.insideUnitSphere * wanderRadius;
    if (NavMesh.SamplePosition(random, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
      _agent.SetDestination(hit.position);
  }

  private void CatchPlayer() {
    if (IsCatching) return;
    IsCatching = true;
    Freeze();
    Debug.Log("[WeepingAngelAI] Caught the player! Stopping all entity audio & triggering biting video.");

    AudioSource[] sources = GetComponentsInChildren<AudioSource>();
    foreach (var src in sources) {
      if (src != null) {
        src.Stop();
        src.enabled = false;
      }
    }

    if (AudioManager.Instance != null) {
      AudioManager.Instance.StopSound();
    }

    JumpscareVideoManager.Instance.PlayJumpscare(bitingVideoClip, entityAudioSource, OnJumpscareFinished);
  }

  private void OnJumpscareFinished() {
    if (Inventory.Instance != null) Inventory.Instance.ClearCurrentRun();
    if (HeistHUDManager.Instance != null) HeistHUDManager.Instance.ShowToastMessage("<color=#FF4444>BONKED! Caught by Entity — lost all carried loot!</color>");

    var loadingController = FindFirstObjectByType<LoadingScene>();
    if (loadingController != null) loadingController.LoadScene(0);
    else UnityEngine.SceneManagement.SceneManager.LoadScene("Store");
  }

  private bool IsSeenByPlayer() {
    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
    if (!GeometryUtility.TestPlanesAABB(planes, _collider.bounds)) return false;

    Vector3 camPosition = playerCamera.transform.position;
    Vector3 directionToAngel = _collider.bounds.center - camPosition;
    float distance = directionToAngel.magnitude;

    if (Physics.Raycast(camPosition, directionToAngel, out RaycastHit hit, distance, obstacleMask)) {
      if (hit.transform != transform) return false;
    }
    return true;
  }
}
