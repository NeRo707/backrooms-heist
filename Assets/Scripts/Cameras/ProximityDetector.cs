using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class ProximityDetector : MonoBehaviour {
  [Header("Detection Settings")]
  [SerializeField] private float maxDistance = 20f;
  [SerializeField] private float minDistance = 2f;
  [SerializeField] private float scanInterval = 1f; // How often we search the scene for targets

  [Header("Beep Settings")]
  [SerializeField] private float slowBeepInterval = 1.5f;
  [SerializeField] private float fastBeepInterval = 0.15f;
  [SerializeField] private AudioClip beepSound;
  [SerializeField] private AudioSource audioSource;

  [Header("UI Feedback")]
  [Tooltip("If left empty, a default UI text will be generated in the center of the screen.")]
  [SerializeField] private TextMeshProUGUI warningText;
  [Tooltip("Optional: Assign a custom font for the warning text. Useful if the text is auto-generated.")]
  [SerializeField] private TMP_FontAsset warningFont;
  [SerializeField] private Color farColor = Color.yellow;
  [SerializeField] private Color mediumColor = new Color(1f, 0.5f, 0f); // Orange
  [SerializeField] private Color closeColor = Color.red;

  private List<Transform> activeTargets = new List<Transform>();
  private float beepTimer = 0f;
  private float scanTimer = 0f;
  private Coroutine flashCoroutine;

  private void Start() {
    audioSource.playOnAwake = false;

    EnsureUI();

    // Make sure the GameObject itself is active so component enabling works
    if (warningText != null && !warningText.gameObject.activeSelf) {
      warningText.gameObject.SetActive(true);
      warningText.enabled = false;
    }

    ScanForTargets();
  }

  private void Update() {
    scanTimer -= Time.deltaTime;
    if (scanTimer <= 0f) {
      ScanForTargets();
      scanTimer = scanInterval;
    }

    Transform closestTarget = GetClosestTarget();

    if (closestTarget != null) {
      float dist = Vector3.Distance(transform.position, closestTarget.position);

      if (dist <= maxDistance) {
        // Calculate distance factor (0 = far, 1 = close)
        float t = 1f - Mathf.Clamp01((dist - minDistance) / (maxDistance - minDistance));

        // Determine beep frequency
        float currentBeepInterval = Mathf.Lerp(slowBeepInterval, fastBeepInterval, t);

        // Update beep timer
        beepTimer -= Time.deltaTime;
        if (beepTimer <= 0f) {
          PlayBeep(t);
          beepTimer = currentBeepInterval;
        }

        return; // We are in range, skip the hide logic
      }
    }

    // If nothing is in range, reset timer and hide UI
    beepTimer = 0f;
    if (warningText != null && warningText.enabled) {
      warningText.enabled = false;
    }
  }

  private void ScanForTargets() {
    activeTargets.Clear();

    // Find all monsters
    ClarkAI[] monsters = FindObjectsByType<ClarkAI>(FindObjectsSortMode.None);
    foreach (var m in monsters) {
      if (!m.IsCatching) {
        activeTargets.Add(m.transform);
      }
    }

    // Find all triggers that haven't been triggered yet
    AnimTrigger[] triggers = FindObjectsByType<AnimTrigger>(FindObjectsSortMode.None);
    foreach (var t in triggers) {
      if (!t.HasPlayed && t.MakeBeep) {
        activeTargets.Add(t.transform);
      }
    }
  }

  private Transform GetClosestTarget() {
    Transform closest = null;
    float minDist = float.MaxValue;

    // Clean up nulls from destroyed objects or triggered events
    activeTargets.RemoveAll(t => t == null);

    // Also check if any AnimTrigger in the list has played since last scan and ignore it
    foreach (Transform target in activeTargets) {
      AnimTrigger animTrigger = target.GetComponent<AnimTrigger>();
      if (animTrigger != null && (animTrigger.HasPlayed || !animTrigger.MakeBeep))
        continue; // Skip played or non-beeping triggers

      ClarkAI clark = target.GetComponent<ClarkAI>();
      if (clark != null && clark.IsCatching)
        continue; // Skip monster if it's currently catching player

      float dist = Vector3.Distance(transform.position, target.position);
      if (dist < minDist) {
        minDist = dist;
        closest = target;
      }
    }

    return closest;
  }

  private void PlayBeep(float closenessFactor) {
    // Play audio
    if (beepSound != null) {
      audioSource.PlayOneShot(beepSound);
    }

    // Update UI Color based on closeness
    Color targetColor = farColor;
    if (closenessFactor > 0.66f) targetColor = closeColor;
    else if (closenessFactor > 0.33f) targetColor = mediumColor;

    if (warningText != null) {
      warningText.color = targetColor;

      if (flashCoroutine != null) StopCoroutine(flashCoroutine);
      flashCoroutine = StartCoroutine(FlashUI());
    }
  }

  private IEnumerator FlashUI() {
    warningText.enabled = true;

    // Show for a brief moment
    yield return new WaitForSeconds(0.1f);

    // Fade out quickly
    float fadeTime = 0.1f;
    float elapsed = 0f;
    Color c = warningText.color;

    while (elapsed < fadeTime) {
      elapsed += Time.deltaTime;
      warningText.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, elapsed / fadeTime));
      yield return null;
    }

    warningText.enabled = false;
  }

  private void EnsureUI() {
    if (warningText == null) {
      // Try to find the HUD Canvas
      Canvas canvas = FindFirstObjectByType<Canvas>();
      if (canvas != null) {
        GameObject textObj = new GameObject("ProximityWarningText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(canvas.transform, false);

        warningText = textObj.GetComponent<TextMeshProUGUI>();
        warningText.text = "WARNING";
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.fontSize = 24;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -60); // Slightly below center crosshair
        rect.sizeDelta = new Vector2(400, 50);

        warningText.enabled = false;
      } else {
        Debug.LogWarning("ProximityDetector: Could not find a Canvas to create default UI.");
      }
    }

    if (warningText != null && warningFont != null) {
      warningText.font = warningFont;
    }
  }
}
