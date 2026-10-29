using System.Collections;
using UnityEngine;

/// <summary>
/// A world-space door that the player can open and close with [E].
/// Can be attached directly to ANY door GameObject — no parent 'Pivot' object required!
/// If the door model has its pivot in the center, you can optionally set 'hingeOffset'
/// to define where the hinge edge is.
/// Includes a configurable interaction range slider.
/// </summary>
public class DoorController : MonoBehaviour, IInteractable {
  // ── Inspector ─────────────────────────────────────────────────────────────
  [Header("Lock Settings")]
  [Tooltip("Whether the door starts locked. If true, requires the matching key in KeyRing to open.")]
  [SerializeField] private bool isLocked = false;

  [Tooltip("Key ID that must be in the player's KeyRing to unlock. Leave empty for an unlocked door.")]
  [SerializeField] private string requiredKeyId = "";

  [Tooltip("Remove the key from the player's KeyRing when the door is first unlocked.")]
  [SerializeField] private bool consumeKey = false;

  [Header("Interaction Range")]
  [Tooltip("Maximum distance from the player to interact with this door. Slide to adjust!")]
  [Range(1f, 8f)]
  [SerializeField] private float maxInteractDistance = 2.6f;

  [Header("Door Movement")]
  [Tooltip("Which transform to rotate. If left empty, rotates this Door GameObject directly (recommended!).")]
  [SerializeField] private Transform doorPivot;

  [Tooltip("Degrees to rotate around the hinge when opened (e.g. 90 or -90).")]
  [SerializeField] private float openAngle = 90f;

  [Tooltip("Local rotation axis (typically Vector3.up / Y-axis for standard doors).")]
  [SerializeField] private Vector3 rotationAxis = Vector3.up;

  [Tooltip(
    "Optional hinge offset relative to the door's position. Use this if your door model's pivot is in the center and you want it to swing from the edge, without needing to create an empty parent GameObject.")]
  [SerializeField] private Vector3 hingeOffset = Vector3.zero;

  [Tooltip("Movement speed — higher = snappier.")]
  [SerializeField] private float openSpeed = 3f;

  [Header("Audio")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip lockedSound; // rattling handle / locked thud
  [SerializeField] private AudioClip openSound; // door swing sound
  [SerializeField] private AudioClip creakSound; // played when closing

  // ── Private state ─────────────────────────────────────────────────────────
  private bool _isOpen;
  private bool _animating;
  private Quaternion _closedRot;
  private Vector3 _closedPos;
  private Vector3 _hingeInParent;
  private Coroutine _currentAnim;

  // ── IInteractable ─────────────────────────────────────────────────────────
  public string InteractPrompt {
    get {
      if (_isOpen)
        return "[E] Close Door";

      if (IsLockedWithoutKey())
        return "[E] Locked — need key";

      return "[E] Open Door";
    }
  }

  public bool CanInteract(GameObject interactor) {
    if (interactor == null) return true;

    // Measure distance to closest point on door collider (or transform position)
    Collider col = GetComponentInChildren<Collider>();
    float dist;
    if (col != null) {
      Vector3 closest = col.ClosestPoint(interactor.transform.position);
      dist = Vector3.Distance(interactor.transform.position, closest);
    } else {
      dist = Vector3.Distance(interactor.transform.position, transform.position);
    }

    return dist <= maxInteractDistance;
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  private void Start() {
    ResolvePivot();

    _closedRot = doorPivot.localRotation;
    _closedPos = doorPivot.localPosition;

    // Calculate hinge position in parent space
    _hingeInParent = _closedPos + _closedRot * hingeOffset;

    // Auto-grab AudioSource
    if (audioSource == null)
      audioSource = GetComponentInChildren<AudioSource>();

    // Ensure there is a Collider so the player's raycast/SphereCast can hit it
    if (GetComponentInChildren<Collider>() == null) {
      var box = gameObject.AddComponent<BoxCollider>();
      var mr = GetComponentInChildren<MeshRenderer>();
      if (mr != null) {
        box.center = transform.InverseTransformPoint(mr.bounds.center);
        box.size = mr.bounds.size;
      }
    }
  }

  private void ResolvePivot() {
    if (doorPivot == null) {
      doorPivot = transform;
      return;
    }

    // Safety check: if user assigned an empty child GameObject with no renderers/colliders,
    // rotating it would do nothing. Auto-fallback to this transform!
    if (doorPivot != transform && doorPivot.IsChildOf(transform)) {
      if (doorPivot.childCount == 0 && doorPivot.GetComponent<Renderer>() == null) {
        Debug.LogWarning(
          $"[DoorController] '{doorPivot.name}' is an empty child with no meshes. Using '{gameObject.name}' as the door pivot instead so the door can swing open.",
          this);
        doorPivot = transform;
      }
    }
  }

  // ── IInteractable Interaction ─────────────────────────────────────────────
  public void Interact(GameObject interactor) {
    if (_animating) return;

    // Double check range
    if (!CanInteract(interactor)) return;

    // ── Check Lock ────────────────────────────────────────────────────────
    if (IsLockedWithoutKey()) {
      PlaySound(lockedSound);
      HeistHUDManager.Instance?.ShowToastMessage("<color=#FF4444>Locked. Find the key.</color>");
      return;
    }

    // ── First-time unlock ─────────────────────────────────────────────────
    if (isLocked) {
      if (consumeKey && !string.IsNullOrEmpty(requiredKeyId))
        KeyRing.Instance?.RemoveKey(requiredKeyId);

      isLocked = false;
    }

    // ── Toggle Open / Close ───────────────────────────────────────────────
    _isOpen = !_isOpen;

    if (_currentAnim != null)
      StopCoroutine(_currentAnim);

    _currentAnim = StartCoroutine(AnimateDoor(_isOpen));
  }

  private bool IsLockedWithoutKey() {
    if (!isLocked) return false;
    if (string.IsNullOrEmpty(requiredKeyId)) return false;
    return KeyRing.Instance == null || !KeyRing.Instance.HasKey(requiredKeyId);
  }

  private void PlaySound(AudioClip clip) {
    if (audioSource != null && clip != null)
      audioSource.PlayOneShot(clip);
  }

  // ── Door Animation ────────────────────────────────────────────────────────
  private IEnumerator AnimateDoor(bool opening) {
    _animating = true;

    AudioClip sfx = opening ? openSound : (creakSound != null ? creakSound : openSound);
    PlaySound(sfx);

    Quaternion targetRot = opening
      ? _closedRot * Quaternion.AngleAxis(openAngle, rotationAxis)
      : _closedRot;

    while (Quaternion.Angle(doorPivot.localRotation, targetRot) > 0.5f) {
      doorPivot.localRotation = Quaternion.Slerp(
        doorPivot.localRotation, targetRot, openSpeed * Time.deltaTime);

      // Derive position FROM the current rotation so the hinge point
      // stays perfectly fixed — no independent lerp = no sliding.
      if (hingeOffset != Vector3.zero)
        doorPivot.localPosition = _hingeInParent - doorPivot.localRotation * hingeOffset;

      yield return null;
    }

    doorPivot.localRotation = targetRot;
    if (hingeOffset != Vector3.zero)
      doorPivot.localPosition = _hingeInParent - targetRot * hingeOffset;

    _animating = false;
  }

  // ── Editor Gizmo ──────────────────────────────────────────────────────────
#if UNITY_EDITOR
  private void OnDrawGizmosSelected() {
    Transform pivot = doorPivot != null ? doorPivot : transform;
    Vector3 hingeWorld = pivot.TransformPoint(hingeOffset);
    Vector3 worldAxis = pivot.TransformDirection(rotationAxis);

    // Hinge point & line
    Gizmos.color = isLocked ? Color.red : Color.green;
    Gizmos.DrawWireSphere(hingeWorld, 0.12f);
    Gizmos.DrawLine(hingeWorld - worldAxis * 1f, hingeWorld + worldAxis * 1f);

    // Swing arc
    UnityEditor.Handles.color = new Color(0.2f, 1f, 0.2f, 0.25f);
    Vector3 fromDir = Vector3.Cross(worldAxis, pivot.right);
    if (fromDir.sqrMagnitude < 0.01f) fromDir = pivot.forward;
    UnityEditor.Handles.DrawSolidArc(hingeWorld, worldAxis, fromDir, openAngle, 0.9f);

    // Interaction range sphere (cyan)
    Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
    Gizmos.DrawWireSphere(pivot.position, maxInteractDistance);
  }
#endif
}
