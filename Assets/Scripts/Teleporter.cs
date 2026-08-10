using UnityEngine;
using UnityEngine.Rendering;

public class Teleporter : MonoBehaviour {
  [SerializeField] private Transform teleportPoint;

  private void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Player")) {
      CharacterController controller = other.GetComponent<CharacterController>();

      if (controller != null) {
        // Disable the controller so it stops overriding position
        controller.enabled = false;

        // Teleport
        other.transform.position = teleportPoint.position;
        other.transform.rotation = teleportPoint.rotation;

        // Re-enable the controller
        controller.enabled = true;

        RenderSettings.ambientLight = new Color32(127, 127, 127, 255);
      }
    }
  }
}
