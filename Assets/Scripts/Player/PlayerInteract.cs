using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour {
  [Header("Settings")]
  public float interactRange = 3.5f;
  [Tooltip("Radius of the sphere tip on the cast. Bigger = more forgiving.")]
  public float interactRadius = 0.3f;
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

    // SphereCast = raycast with a sphere tip — forgiving circular detection
    if (Physics.SphereCast(playerCamera.transform.position, interactRadius, playerCamera.transform.forward, out var hit, interactRange, Physics.AllLayers, QueryTriggerInteraction.Collide)) {
      CurrentHoverItem = hit.collider.GetComponentInParent<PickupItem>();
    } else {
      CurrentHoverItem = null;
    }
  }

  private void OnDrawGizmosSelected() {
    if (playerCamera == null) return;
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(playerCamera.transform.position + playerCamera.transform.forward * interactRange, interactRadius);
    Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactRange);
  }
}

