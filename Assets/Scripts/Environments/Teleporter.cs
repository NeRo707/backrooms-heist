using UnityEngine;
using UnityEngine.Rendering;

public class Teleporter : MonoBehaviour {
  [SerializeField] private Transform teleportPoint;

  [Header("Map Loading")]
  [Tooltip(
    "The map root that this teleporter sends the player to. MapManager will enable this map and disable all others.")]
  [SerializeField] private GameObject destinationMap;

  [Header("Enable/Disable something?")]
  [Tooltip("Enables some game object")]
  [SerializeField] private bool enable;
  [SerializeField] private GameObject obj;

  private void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Player")) {
      CharacterController controller = other.GetComponent<CharacterController>();

      if (controller != null) {
        // Enable destination map BEFORE teleporting so it's ready
        if (destinationMap != null && MapManager.Instance != null) {
          MapManager.Instance.SetActiveMap(destinationMap);
        }

        // Disable the controller so it stops overriding position
        controller.enabled = false;

        // Teleport
        other.transform.position = teleportPoint.position;
        other.transform.rotation = teleportPoint.rotation;
        if (obj != null) {
          obj.SetActive(enable);
        }

        // Re-enable the controller
        controller.enabled = true;

        RenderSettings.ambientLight = new Color32(127, 127, 127, 255);
      }
    }
  }
}
