using UnityEngine;

public class PassGate : MonoBehaviour {
  [Tooltip("The wall collider to pass through. Leave empty to auto-detect on Start.")]
  [SerializeField] private Collider mapCollider;

  [Tooltip("How far to raycast when searching for the wall.")]
  [SerializeField] private float wallDetectRange = 2f;

  void Start() {
    if (mapCollider == null) {
      mapCollider = AutoDetectWall();

      if (mapCollider != null)
        Debug.Log($"[PassGate] '{name}' auto-detected wall: '{mapCollider.name}'", this);
      else
        Debug.LogWarning($"[PassGate] '{name}' could not auto-detect a wall collider nearby!", this);
    }
  }

  // Shoots rays in 6 directions and returns the nearest solid (non-trigger) collider found.
  Collider AutoDetectWall() {
    Vector3[] directions = {
      transform.forward,
      -transform.forward,
      transform.right,
      -transform.right,
      transform.up,
      -transform.up
    };

    Collider best = null;
    float bestDist = float.MaxValue;

    foreach (var dir in directions) {
      if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallDetectRange, Physics.AllLayers, QueryTriggerInteraction.Ignore)) {
        // Skip ourselves and triggers
        if (hit.collider == GetComponent<Collider>()) continue;
        if (hit.distance < bestDist) {
          bestDist = hit.distance;
          best = hit.collider;
        }
      }
    }

    return best;
  }

  void OnTriggerEnter(Collider other) {
    if (mapCollider == null || !other.CompareTag("Player")) return;
    Physics.IgnoreCollision(other, mapCollider, true);
  }

  void OnTriggerExit(Collider other) {
    if (mapCollider == null || !other.CompareTag("Player")) return;
    Physics.IgnoreCollision(other, mapCollider, false);
  }

  // Visualise the detection rays in the Scene view
  void OnDrawGizmosSelected() {
    Gizmos.color = Color.cyan;
    Vector3[] directions = {
      transform.forward, -transform.forward,
      transform.right,   -transform.right,
      transform.up,      -transform.up
    };
    foreach (var dir in directions)
      Gizmos.DrawRay(transform.position, dir * wallDetectRange);

    if (mapCollider != null) {
      Gizmos.color = Color.green;
      Gizmos.DrawWireCube(mapCollider.bounds.center, mapCollider.bounds.size);
    }
  }
}

