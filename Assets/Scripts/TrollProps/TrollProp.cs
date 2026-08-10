using UnityEngine;

namespace TrollProps {
  public class TrollProp : MonoBehaviour {
    [Header("Target Settings")]
    public Transform propToRotate; // The object that will look at the player
    public bool lockYAxis = true; // Keeps the prop upright instead of tilting up/down
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other) {
      if (hasTriggered && triggerOnce) return;

      if (other.CompareTag("Player")) {
        if (propToRotate != null) {
          if (lockYAxis) {
            // Calculate direction to player, ignoring height differences
            Vector3 direction = other.transform.position - propToRotate.position;
            direction.y = 0;
            if (direction != Vector3.zero) {
              propToRotate.rotation = Quaternion.LookRotation(direction);
            }
          } else {
            propToRotate.LookAt(other.transform);
          }
        }

        hasTriggered = true;
      }
    }
  }
}
