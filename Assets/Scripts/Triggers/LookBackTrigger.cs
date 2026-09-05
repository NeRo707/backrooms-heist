using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class LookBackTrigger : MonoBehaviour {
  [Header("References")]
  [Tooltip("The player's camera.")]
  [SerializeField] private Transform playerCamera;

  [Tooltip("The text on the wall. We use this to know where 'forward' is.")]
  [SerializeField] private Transform textOnWall;

  [Header("Events")]
  [Tooltip("What happens when they turn around and look behind?")]
  [SerializeField] private UnityEvent onLookedBehind;

  private bool _playerInZone = false;
  private bool _hasTriggered = false;

  private void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Player") && !_hasTriggered) {
      _playerInZone = true;
    }
  }

  private void OnTriggerExit(Collider other) {
    if (other.CompareTag("Player")) {
      _playerInZone = false;
    }
  }

  private void Update() {
    if (_playerInZone && !_hasTriggered) {
      Vector3 directionToText = (textOnWall.position - playerCamera.position).normalized;
      Vector3 cameraFacing = playerCamera.forward;

      // Dot Product checks the angle:
      //  1.0 = looking perfectly at text
      //  0.0 = looking 90 degrees sideways (or straight down at the floor)
      // -1.0 = looking perfectly away (behind)
      float lookMatch = Vector3.Dot(cameraFacing, directionToText);

      // -0.5 means they have rotated their camera heavily away from the text (looking behind)
      if (lookMatch < -0.5f) {
        _hasTriggered = true;
        onLookedBehind.Invoke();
      }
    }
  }
}
