using System.Collections;
using UnityEngine;

/// <summary>
/// Controls Act 1 Hallway Scare sequence:
/// 1. Player walks midway -> lightbulb snaps off with breaking sound.
/// 2. Player turns around -> behind scare object flashes for a split second, then disappears.
/// 3. Player turns back forward -> ahead scare object appears down the hallway with eerie sound/animation, then disappears.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HallwayDoubleScareSequence : MonoBehaviour {
  [Header("Step 1: Lightbulb Burst")]
  [Tooltip("Light fixture to turn off when the bulb breaks.")]
  [SerializeField] private Light lightToDisable;
  [Tooltip("AudioSource to play the bulb break sound.")]
  [SerializeField] private AudioSource lightbulbBreakSound;
  [Tooltip("Optional break clip. If assigned, will play with PlayOneShot.")]
  [SerializeField] private AudioClip bulbBreakClip;
  [Tooltip("Optional sparks or glass particle effect to activate.")]
  [SerializeField] private GameObject bulbBrokenParticles;

  [Header("Hallway Orientation")]
  [Tooltip("Direction facing forward down the hallway (towards the black room). Default (0, 0, -1).")]
  [SerializeField] private Vector3 hallwayForward = new Vector3(0f, 0f, -1f);

  [Header("Step 2: Behind Scare")]
  [Tooltip("Scare entity positioned behind the player in the hallway.")]
  [SerializeField] private GameObject behindScareObject;
  [Tooltip("Camera dot product threshold to detect looking back. <= -0.3 means looking mostly back.")]
  [Range(-1f, 0f)]
  [SerializeField] private float lookBehindThreshold = -0.3f;
  [Tooltip("How long the behind scare remains visible in seconds.")]
  [Range(0.1f, 2f)]
  [SerializeField] private float behindScareDuration = 0.35f;
  [Tooltip("Audio source for the behind scare.")]
  [SerializeField] private AudioSource behindAudioSource;
  [Tooltip("Optional audio clip for the behind scare (e.g. sharp sting or gasp).")]
  [SerializeField] private AudioClip behindScareClip;
  [Tooltip("Max seconds to wait for player to look back before proceeding forward.")]
  [SerializeField] private float lookBehindTimeout = 6f;

  [Header("Step 3: Ahead Scare")]
  [Tooltip("Scare entity positioned ahead down the hallway (e.g. in front of the next door).")]
  [SerializeField] private GameObject aheadScareObject;
  [Tooltip("Camera dot product threshold to detect looking back forward. >= 0.4 means looking forward again.")]
  [Range(0f, 1f)]
  [SerializeField] private float lookForwardThreshold = 0.4f;
  [Tooltip("How long the ahead scare remains visible in seconds.")]
  [Range(0.2f, 4f)]
  [SerializeField] private float aheadScareDuration = 1.0f;
  [Tooltip("Audio source for the ahead scare.")]
  [SerializeField] private AudioSource aheadAudioSource;
  [Tooltip("Audio clip for the ahead scare (e.g. bone crack, whisper, or violin screech).")]
  [SerializeField] private AudioClip aheadScareClip;
  [Tooltip("Optional animator on the ahead scare.")]
  [SerializeField] private Animator aheadAnimator;
  [Tooltip("Optional trigger parameter to fire on the animator.")]
  [SerializeField] private string aheadAnimationTrigger = "";

  [Header("References (Optional - Auto-resolved if empty)")]
  [SerializeField] private Transform playerCameraTransform;

  // Runtime state
  private bool _triggered = false;
  private Coroutine _sequenceCoroutine;

  public bool IsTriggered => _triggered;

  private void Awake() {
    // Ensure scare objects start disabled
    if (behindScareObject != null) behindScareObject.SetActive(false);
    if (aheadScareObject != null) aheadScareObject.SetActive(false);
    if (bulbBrokenParticles != null) bulbBrokenParticles.SetActive(false);

    // Verify collider is trigger
    Collider col = GetComponent<Collider>();
    if (col != null && !col.isTrigger) {
      col.isTrigger = true;
    }
  }

  private void OnTriggerEnter(Collider other) {
    if (_triggered || !other.CompareTag("Player")) return;
    _triggered = true;

    // Resolve player camera if not explicitly assigned
    if (playerCameraTransform == null) {
      ResolvePlayerCamera(other.gameObject);
    }

    _sequenceCoroutine = StartCoroutine(RunSequence());
  }

  private void ResolvePlayerCamera(GameObject playerObj) {
    Camera cam = playerObj.GetComponentInChildren<Camera>();
    if (cam != null) {
      playerCameraTransform = cam.transform;
      return;
    }

    if (Camera.main != null) {
      playerCameraTransform = Camera.main.transform;
      return;
    }

    // Fallback: look for child named Camera
    Transform camChild = playerObj.transform.Find("Camera");
    if (camChild != null) {
      playerCameraTransform = camChild;
      return;
    }

    playerCameraTransform = playerObj.transform;
  }

  private IEnumerator RunSequence() {
    // ── 1. Lightbulb Burst ──────────────────────────────────────────────────
    if (lightbulbBreakSound != null) {
      if (bulbBreakClip != null) {
        lightbulbBreakSound.PlayOneShot(bulbBreakClip);
      } else {
        lightbulbBreakSound.Play();
      }
    }

    if (lightToDisable != null) {
      lightToDisable.enabled = false;
    }

    if (bulbBrokenParticles != null) {
      bulbBrokenParticles.SetActive(true);
    }

    // ── 2. Wait for Player to Look Behind ───────────────────────────────────
    float behindTimer = 0f;
    bool lookedBehind = false;
    Vector3 fwdNormalized = hallwayForward.normalized;

    while (behindTimer < lookBehindTimeout) {
      if (playerCameraTransform != null) {
        Vector3 camHoriz = playerCameraTransform.forward;
        camHoriz.y = 0f;
        if (camHoriz.sqrMagnitude > 0.001f) {
          camHoriz.Normalize();
          float dot = Vector3.Dot(camHoriz, fwdNormalized);
          if (dot <= lookBehindThreshold) {
            lookedBehind = true;
            break;
          }
        }
      }

      behindTimer += Time.deltaTime;
      yield return null;
    }

    // If player turned around, show the behind scare for a split second!
    if (lookedBehind && behindScareObject != null) {
      behindScareObject.SetActive(true);

      if (behindAudioSource != null) {
        if (behindScareClip != null) {
          behindAudioSource.PlayOneShot(behindScareClip);
        } else {
          behindAudioSource.Play();
        }
      }

      yield return new WaitForSeconds(behindScareDuration);
      behindScareObject.SetActive(false);
    }

    // ── 3. Wait for Player to Turn Back Forward ─────────────────────────────
    float forwardTimer = 0f;
    while (forwardTimer < 8f) {
      if (playerCameraTransform != null) {
        Vector3 camHoriz = playerCameraTransform.forward;
        camHoriz.y = 0f;
        if (camHoriz.sqrMagnitude > 0.001f) {
          camHoriz.Normalize();
          float dot = Vector3.Dot(camHoriz, fwdNormalized);
          if (dot >= lookForwardThreshold) {
            break;
          }
        }
      }

      forwardTimer += Time.deltaTime;
      yield return null;
    }

    // Show Ahead Scare!
    if (aheadScareObject != null) {
      aheadScareObject.SetActive(true);

      if (aheadAnimator != null && !string.IsNullOrEmpty(aheadAnimationTrigger)) {
        aheadAnimator.SetTrigger(aheadAnimationTrigger);
      }

      if (aheadAudioSource != null) {
        if (aheadScareClip != null) {
          aheadAudioSource.PlayOneShot(aheadScareClip);
        } else {
          aheadAudioSource.Play();
        }
      }

      yield return new WaitForSeconds(aheadScareDuration);
      aheadScareObject.SetActive(false);
    }
  }

#if UNITY_EDITOR
  private void OnDrawGizmosSelected() {
    // Draw hallway forward direction arrow
    Gizmos.color = Color.cyan;
    Vector3 origin = transform.position;
    Vector3 target = origin + hallwayForward.normalized * 3f;
    Gizmos.DrawLine(origin, target);
    Gizmos.DrawWireSphere(target, 0.2f);

    // Draw lines to scare objects if linked
    if (behindScareObject != null) {
      Gizmos.color = Color.red;
      Gizmos.DrawLine(origin, behindScareObject.transform.position);
      Gizmos.DrawWireSphere(behindScareObject.transform.position, 0.3f);
    }

    if (aheadScareObject != null) {
      Gizmos.color = Color.yellow;
      Gizmos.DrawLine(origin, aheadScareObject.transform.position);
      Gizmos.DrawWireSphere(aheadScareObject.transform.position, 0.3f);
    }
  }
#endif
}
