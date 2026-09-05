using System;
using UnityEngine;

public class LookTriggerAudio : MonoBehaviour {
  [Header("Settings")]
  [SerializeField] private Camera playerCamera;
  [SerializeField] private AudioSource audioSource;

  [Header("Range")]
  [SerializeField] private float triggerRadius = 3f;

  [Header("Line of Sight (Optional)")]
  [SerializeField] private bool checkLineOfSight = true;
  [SerializeField] private LayerMask obstacleLayers;

  [Header("Viewport Margins")]
  [Range(0f, 0.5f)] [SerializeField] private float margin = 0.05f;

  private Renderer objectRenderer;

  void Start() {
    if (playerCamera == null) playerCamera = Camera.main;
    if (playerCamera == null)
      throw new System.Exception($"{name}: LookTriggerAudio requires a camera (none assigned, no Camera.main).");

    if (audioSource == null) audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
      throw new System.Exception($"{name}: LookTriggerAudio requires an AudioSource component.");

    objectRenderer = GetComponent<Renderer>();
    if (objectRenderer == null)
      throw new System.Exception($"{name}: LookTriggerAudio requires a Renderer component for visibility bounds.");
  }

  void Update() {
    if (IsInCameraView()) {
      if (!audioSource.isPlaying) audioSource.Play();
    } else {
      if (audioSource.isPlaying) audioSource.Stop();
    }
  }

  private bool IsInCameraView() {
    Vector3 targetPoint = objectRenderer.bounds.center;
    Vector3 direction = targetPoint - playerCamera.transform.position;

    if (direction.magnitude > triggerRadius) return false;

    Vector3 viewPos = playerCamera.WorldToViewportPoint(targetPoint);
    bool insideScreen = viewPos.z > 0 &&
                        viewPos.x >= margin && viewPos.x <= (1f - margin) &&
                        viewPos.y >= margin && viewPos.y <= (1f - margin);
    if (!insideScreen) return false;

    if (checkLineOfSight) {
      if (Physics.Raycast(playerCamera.transform.position, direction.normalized, out RaycastHit hit,
            direction.magnitude, obstacleLayers)) {
        if (hit.transform != transform && !hit.transform.IsChildOf(transform))
          return false;
      }
    }

    return true;
  }

  private void OnDrawGizmos() {
    Gizmos.DrawWireSphere(transform.position, triggerRadius);
  }
}
