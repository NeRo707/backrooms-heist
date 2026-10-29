using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Highly optimized LOD system for Lights.
/// Put this on the root of your map (e.g. Backrooms_Movie).
/// It will find all child lights and disable them if they are too far from the player.
/// </summary>
public class LightDistanceCuller : MonoBehaviour {

  [Header("References")]
  [Tooltip("The player transform. Auto-found via Player tag if left empty.")]
  [SerializeField] private Transform player;

  [Header("Settings")]
  [Tooltip("Lights beyond this distance will be turned off.")]
  [SerializeField] private float cullDistance = 30f;
  
  [Tooltip("How often to check (in seconds). Higher = better CPU performance.")]
  [SerializeField] private float checkInterval = 0.25f;

  // Cache the transform and light component to avoid slow GetComponent calls
  private struct LightData {
    public Transform transform;
    public Light lightComponent;
  }

  private List<LightData> _lights = new List<LightData>();
  private float _sqrCullDistance;

  private void Start() {
    if (player == null) {
      var p = GameObject.FindGameObjectWithTag("Player");
      if (p != null) player = p.transform;
    }

    _sqrCullDistance = cullDistance * cullDistance;

    // Find all lights in this map once at startup
    Light[] allLights = GetComponentsInChildren<Light>(true);
    foreach (var l in allLights) {
      // Ignore directional lights (sun) and already baked lights
      if (l.type != LightType.Directional && l.lightmapBakeType != LightmapBakeType.Baked) {
        _lights.Add(new LightData {
          transform = l.transform,
          lightComponent = l
        });
      }
    }

    Debug.Log($"[LightCuller] Found {_lights.Count} realtime lights to manage on {gameObject.name}");

    StartCoroutine(CullRoutine());
  }

  private IEnumerator CullRoutine() {
    WaitForSeconds wait = new WaitForSeconds(checkInterval);

    while (true) {
      if (player != null) {
        Vector3 playerPos = player.position;

        // We check a small batch of lights per frame to ensure ZERO frame drops
        int lightsPerFrame = 50; 
        int processedThisFrame = 0;

        for (int i = 0; i < _lights.Count; i++) {
          var data = _lights[i];
          if (data.transform == null) continue;

          // sqrMagnitude is MUCH faster for the CPU than Vector3.Distance
          float sqrDist = (data.transform.position - playerPos).sqrMagnitude;
          bool shouldBeOn = sqrDist <= _sqrCullDistance;

          if (data.lightComponent.enabled != shouldBeOn) {
            data.lightComponent.enabled = shouldBeOn;
          }

          processedThisFrame++;
          if (processedThisFrame >= lightsPerFrame) {
            processedThisFrame = 0;
            yield return null; // Wait until next frame to do the next batch
          }
        }
      }
      
      // Wait for the interval before scanning again
      yield return wait;
    }
  }
}
