using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour {
  public float interactRange = 3f;
  public Camera playerCamera;

  public PickupItem CurrentHoverItem { get; private set; }

  private void Start() {
    if (playerCamera == null) {
      playerCamera = Camera.main;
    }
  }

  private void Update() {
    UpdateHoverItem();

    bool interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) {
      interactPressed = true;
    }

    if (interactPressed && CurrentHoverItem != null) {
      CurrentHoverItem.Collect();
    }
  }

  private void UpdateHoverItem() {
    if (playerCamera == null) {
      CurrentHoverItem = null;
      return;
    }

    RaycastHit hit;
    if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactRange)) {
      CurrentHoverItem = hit.collider.GetComponentInParent<PickupItem>();
    } else {
      CurrentHoverItem = null;
    }
  }
}

