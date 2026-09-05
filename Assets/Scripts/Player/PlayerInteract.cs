using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour {
  [Header("Settings")]
  [SerializeField] private float interactRange = 3.5f;
  [Tooltip("Radius of the sphere tip on the cast. Bigger = more forgiving.")]
  [SerializeField] private float interactRadius = 0.3f;
  [SerializeField] private Camera playerCamera;
  public Camera PlayerCamera { get => playerCamera; set => playerCamera = value; }

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

