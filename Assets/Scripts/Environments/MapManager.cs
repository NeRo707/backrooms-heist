using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages which map is active. Only the current map the player is in stays enabled.
/// All others are fully disabled (zero rendering, zero physics, zero scripts).
///
/// Event-driven — only runs when a teleporter calls SetActiveMap().
/// No polling, no Update loop, no CPU cost between teleports.
/// </summary>
public class MapManager : MonoBehaviour {

  public static MapManager Instance { get; private set; }

  [Header("Map Roots")]
  [Tooltip("Drag all your map root GameObjects here (e.g. Poolrooms, backrooms_v4, Backrooms_Movie).")]
  [SerializeField] private List<GameObject> mapRoots = new List<GameObject>();

  [Header("Starting Map")]
  [Tooltip("Which map should be active when the scene loads. Leave empty to keep all as-is.")]
  [SerializeField] private GameObject startingMap;

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;

    if (startingMap != null) {
      SetActiveMap(startingMap);
    }
  }

  /// <summary>
  /// Activates the target map and disables all others. Called by Teleporter on teleport.
  /// </summary>
  public void SetActiveMap(GameObject targetMap) {
    foreach (var map in mapRoots) {
      if (map == null) continue;
      bool shouldBeActive = (map == targetMap);

      if (map.activeSelf != shouldBeActive) {
        map.SetActive(shouldBeActive);

        #if UNITY_EDITOR
        Debug.Log($"[MapManager] {map.name} → {(shouldBeActive ? "ENABLED" : "DISABLED")}");
        #endif
      }
    }
  }

  private void OnDestroy() {
    if (Instance == this) Instance = null;
  }
}
