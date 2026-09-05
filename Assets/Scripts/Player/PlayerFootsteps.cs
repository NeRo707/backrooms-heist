using UnityEngine;
using AZE.AdvancedFirstPerson;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour {
  [Header("Footstep Clips")]
  [SerializeField] private AudioClip[] woodClips;
  [SerializeField] private AudioClip[] tileClips;
  [SerializeField] private AudioClip[] waterClips;

  [Header("Surface Layers")]
  [Tooltip("If the ground matches this layer, water footsteps will play.")]
  [SerializeField] private LayerMask waterLayer;
  [Tooltip("If the ground matches this layer, tile footsteps will play.")]
  [SerializeField] private LayerMask tileLayer;
  [Tooltip("If it doesn't match water or tile (or matches this explicitly), wood footsteps will play.")]
  [SerializeField] private LayerMask woodLayer;

  [Header("Settings")]
  [SerializeField] private float baseStepInterval = 0.5f;
  [SerializeField] private float sprintStepInterval = 0.3f;
  [SerializeField] private float crouchStepInterval = 0.7f;

  private AudioSource audioSource;
  private CharacterController controller;
  private PlayerMovementStateMachine movement;
  private float stepTimer;

  void Awake() {
    audioSource = GetComponent<AudioSource>();
    audioSource.playOnAwake = false;

    controller = GetComponent<CharacterController>();
    movement = GetComponent<PlayerMovementStateMachine>();
  }

  void Update() {
    // Calculate horizontal velocity to ignore vertical drops
    Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);

    if (controller.isGrounded && horizontalVelocity.magnitude > 0.1f) {
      stepTimer -= Time.deltaTime;
      if (stepTimer <= 0f) {
        PlayFootstep();

        // Adjust interval based on current movement state (speed percentage)
        float speedPercent = movement != null ? movement.CurrentSpeedPercentage : 0.5f;

        if (speedPercent > 0.8f)
          stepTimer = sprintStepInterval;
        else if (speedPercent < 0.4f)
          stepTimer = crouchStepInterval;
        else
          stepTimer = baseStepInterval;
      }
    } else {
      // Reset timer so the first step plays immediately when starting to move
      stepTimer = 0f;
    }
  }

  private void PlayFootstep() {
    AudioClip[] clipsToPlay = woodClips; // Default to wood

    // Raycast down to detect the floor layer
    if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 2.0f)) {
      int hitLayer = hit.collider.gameObject.layer;

      // Check which LayerMask contains the hit layer
      if (waterLayer == (waterLayer | (1 << hitLayer))) {
        clipsToPlay = waterClips;
      } else if (tileLayer == (tileLayer | (1 << hitLayer))) {
        clipsToPlay = tileClips;
      } else if (woodLayer == (woodLayer | (1 << hitLayer))) {
        clipsToPlay = woodClips;
      }
    }

    if (clipsToPlay != null && clipsToPlay.Length > 0) {
      // Play a random clip from the selected array
      AudioClip clip = clipsToPlay[Random.Range(0, clipsToPlay.Length)];

      // Randomize pitch slightly for variety
      // audioSource.pitch = Random.Range(0.9f, 1.1f);
      audioSource.PlayOneShot(clip);
    }
  }
}
