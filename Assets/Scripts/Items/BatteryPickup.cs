using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A world-space battery pickup that the player can collect by pressing E while nearby.
/// Calls <see cref="FlashlightBattery.Recharge"/> on the Player's flashlight,
/// plays a sound, shows a HUD toast, and destroys itself.
///
/// Setup:
///   • Add a Trigger Collider (e.g. Sphere, radius ≈ 1.5 m) to this GameObject.
///   • Assign an AudioSource (auto-grabbed) and an optional AudioClip.
///   • The Player GameObject must be tagged "Player" and have a FlashlightBattery component.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BatteryPickup : MonoBehaviour, IInteractable {
  // -- Inspector -------------------------------------------------------------
  [Header("Pickup Settings")]
  [Tooltip("How many battery units to restore (base max is 100).")]
  [SerializeField] private float rechargeAmount = 50f;
  [SerializeField] private AudioClip pickupSound;

  public string InteractPrompt => $"[E] Pick up Battery (+{Mathf.RoundToInt(rechargeAmount)}%)";
  public bool CanInteract(GameObject interactor) => !_collected;
  public void Interact(GameObject interactor) => Collect();

  [Header("Visuals")]
  [Tooltip("Amplitude of the vertical bob in metres.")]
  [SerializeField] private float bobAmplitude = 0.15f;
  [Tooltip("Oscillation speed (radians per second).")]
  [SerializeField] private float bobSpeed = 2f;

  // -- Private ---------------------------------------------------------------
  private AudioSource _audioSource;
  // private Vector3 _startPos;
  private bool _playerInRange;
  private bool _collected;

  // -- Lifecycle -------------------------------------------------------------
  private void Awake() {
    _audioSource = GetComponent<AudioSource>();
    if (_audioSource == null)
      _audioSource = gameObject.AddComponent<AudioSource>();

    // Ensure the collider is a trigger.
    GetComponent<Collider>().isTrigger = true;
  }

  // private void Start() {
  //   _startPos = transform.position;
  // }

  private void Update() {
    if (_collected) return;

    // // -- Vertical bob ------------------------------------------------------
    // transform.position = _startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;

    // -- Collection input --------------------------------------------------
    if (_playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
      Collect();
  }

  // -- Trigger Detection -----------------------------------------------------
  private void OnTriggerEnter(Collider other) {
    if (!other.CompareTag("Player")) return;
    _playerInRange = true;

    // Optional: nudge the HUD to show a pickup prompt.
    if (HeistHUDManager.Instance != null)
      HeistHUDManager.Instance.ShowToastMessage("[E] Pick up Battery");
  }

  private void OnTriggerExit(Collider other) {
    if (!other.CompareTag("Player")) return;
    _playerInRange = false;
  }

  private void OnTriggerStay(Collider other) {
    // Redundant safety: re-flag if somehow missed the Enter event.
    if (other.CompareTag("Player"))
      _playerInRange = true;
  }

  // -- Collection Logic ------------------------------------------------------
  private void Collect() {
    _collected = true; // Prevent double-collection.

    // Find the battery component on the player.
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null) {
      FlashlightBattery battery = player.GetComponentInChildren<FlashlightBattery>();
      if (battery != null) {
        battery.Recharge(rechargeAmount);

        // Show the percentage added as a HUD toast.
        int pct = Mathf.RoundToInt((rechargeAmount / 100f) * 100f);
        if (HeistHUDManager.Instance != null)
          HeistHUDManager.Instance.ShowToastMessage($"Battery +{pct}%");
      } else {
        Debug.LogWarning("[BatteryPickup] Player has no FlashlightBattery component.");
      }
    }

    // Play the pickup sound and destroy after it finishes.
    if (pickupSound != null && _audioSource != null) {
      // Detach the AudioSource so it survives the GameObject being destroyed.
      _audioSource.transform.parent = null;
      _audioSource.PlayOneShot(pickupSound);
      Destroy(_audioSource.gameObject, pickupSound.length + 0.1f);
    }

    // Hide visuals immediately and destroy this pickup.
    foreach (Renderer r in GetComponentsInChildren<Renderer>())
      r.enabled = false;

    Destroy(gameObject);
  }
}
