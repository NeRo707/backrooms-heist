using UnityEngine;

/// <summary>
/// A physical key pickup in the world.
/// The player can collect it by pressing [E] while looking at it.
/// The key ID is registered in KeyRing and matched against DoorController.requiredKeyId.
/// </summary>
public class KeyItem : MonoBehaviour, IInteractable {
  [Header("Key Settings")]
  [Tooltip("Must exactly match the requiredKeyId on the door this key unlocks.")]
  [SerializeField] private string keyId = "key_01";
  [SerializeField] private AudioClip pickupSound;

  // [Header("Idle Animation")]
  // [SerializeField] private float bobSpeed  = 2f;
  // [SerializeField] private float bobHeight = 0.15f;

  // ── IInteractable ────────────────────────────────────────────────────────
  public string InteractPrompt => "[E] Pick up Key";
  public bool CanInteract(GameObject interactor) => true;

  // ── Exposed ──────────────────────────────────────────────────────────────
  /// <summary>The key's unique ID — read by KeyRing and DoorController.</summary>
  public string KeyId => keyId;

  // // ── Private state ────────────────────────────────────────────────────────
  // private Vector3 _startPos;
  private bool _collected;
  //
  // // ── Unity messages ───────────────────────────────────────────────────────
  // private void Start() => _startPos = transform.position;

  private void Update() {
    if (_collected) return;

    // // Gentle bob + slow Y-rotation to make key visible in world space
    // transform.position = _startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    // transform.Rotate(0f, 90f * Time.deltaTime, 0f);
  }

  // ── IInteractable ────────────────────────────────────────────────────────
  public void Interact(GameObject interactor) {
    if (_collected) return;
    _collected = true;

    // Register key in the player's ring
    KeyRing.Instance?.AddKey(keyId);

    // Pickup audio (spatial, one-shot)
    if (pickupSound)
      AudioSource.PlayClipAtPoint(pickupSound, transform.position);

    // HUD notification
    HeistHUDManager.Instance?.ShowToastMessage("<color=#FFD700>Key collected!</color>");

    Destroy(gameObject);
  }
}
