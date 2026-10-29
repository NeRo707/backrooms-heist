using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player interaction with world items (PickupItem) and interactables (IInteractable, e.g. Doors, Keys, Batteries).
/// Uses SphereCastAll to penetrate through ambient/trigger volumes without being blocked.
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Global maximum raycast reach for detecting items. Slide to adjust!")]
    [Range(1f, 8f)]
    [SerializeField] private float interactRange = 3.5f;

    [Tooltip("Radius of the sphere tip on the cast. Bigger = more forgiving.")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float interactRadius = 0.25f;

    [SerializeField] private Camera playerCamera;
    public Camera PlayerCamera { get => playerCamera; set => playerCamera = value; }

    public PickupItem CurrentHoverItem { get; private set; }
    public IInteractable CurrentInteractable { get; private set; }

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
        }
    }

    private void Update()
    {
        UpdateHoverItem();

        bool interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            interactPressed = true;
        }

        if (interactPressed)
        {
            if (CurrentHoverItem != null)
            {
                CurrentHoverItem.Collect();
            }
            else if (CurrentInteractable != null && CurrentInteractable.CanInteract(gameObject))
            {
                CurrentInteractable.Interact(gameObject);
            }
        }
    }

    private void UpdateHoverItem()
    {
        if (playerCamera == null)
        {
            CurrentHoverItem = null;
            CurrentInteractable = null;
            return;
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        // SphereCastAll so non-interactable trigger zones (e.g. ambient triggers, room bounds)
        // do not block line-of-sight to the door or pickup behind them
        var hits = Physics.SphereCastAll(origin, interactRadius, direction, interactRange, Physics.AllLayers, QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
        {
            CurrentHoverItem = null;
            CurrentInteractable = null;
            return;
        }

        // Sort by distance from player
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        PickupItem foundItem = null;
        IInteractable foundInteractable = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            var col = hit.collider;
            if (col == null) continue;

            // Don't interact with self (player's own character controller or colliders)
            if (col.transform.root == transform.root) continue;

            var item = col.GetComponentInParent<PickupItem>();
            if (item != null)
            {
                foundItem = item;
                break;
            }

            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                // Only consider this interactable if it allows interaction from current distance
                if (interactable.CanInteract(gameObject))
                {
                    foundInteractable = interactable;
                    break;
                }
            }

            // If we hit a solid obstacle (NOT a trigger) that isn't interactable,
            // it genuinely blocks line-of-sight (e.g. looking at a door through a solid wall)
            if (!col.isTrigger)
            {
                break;
            }
        }

        CurrentHoverItem = foundItem;
        CurrentInteractable = foundInteractable;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerCamera.transform.position + playerCamera.transform.forward * interactRange, interactRadius);
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactRange);
    }
}
