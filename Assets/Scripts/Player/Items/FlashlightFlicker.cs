using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlashlightFlicker : MonoBehaviour {
  [Header("Angels")]
  [Tooltip("Drag angels here, or leave empty and enable Auto Find By Tag.")]
  public List<Transform> angels = new();
  public bool autoFindByTag = true;
  public string angelTag = "Angel";

  [Header("Flicker")]
  public float startDistance = 15f;
  public float minDistance = 3f;
  public AudioSource buzz; // optional

  private Light _light;
  private float _baseIntensity;
  private float _offUntil;

  private void Start() {
    _light = GetComponent<Light>();
    _baseIntensity = _light.intensity;

    if (autoFindByTag && angels.Count == 0) {
      try {
        foreach (var go in GameObject.FindGameObjectsWithTag(angelTag))
          angels.Add(go.transform);
      } catch (UnityException) {
        Debug.LogError($"FlashlightFlicker: tag '{angelTag}' is not defined in the Tag Manager.", this);
      }
    }

    if (angels.Count == 0)
      Debug.LogWarning("FlashlightFlicker: no angels assigned or found, the flashlight will never flicker.", this);
  }

  private float NearestAngelDistance() {
    float minSqr = float.MaxValue;
    Vector3 pos = transform.position;

    for (int i = angels.Count - 1; i >= 0; i--) {
      Transform a = angels[i];

      if (a == null) { angels.RemoveAt(i); continue; }   // destroyed: prune for good
      if (!a.gameObject.activeInHierarchy) continue;     // disabled: skip, keep in list

      float sqr = (a.position - pos).sqrMagnitude;
      if (sqr < minSqr) minSqr = sqr;
    }

    return minSqr == float.MaxValue ? float.PositiveInfinity : Mathf.Sqrt(minSqr);
  }

  private void Update() {
    float d = NearestAngelDistance();
    float t = Mathf.InverseLerp(startDistance, minDistance, d); // 0 far, 1 close

    if (t <= 0f) {
      _light.intensity = _baseIntensity;
      return;
    }

    if (Time.time < _offUntil) {
      _light.intensity = 0f;
      return;
    }

    // Blackout chance and length scale with proximity
    if (Random.value < t * t * 6f * Time.deltaTime) {
      _offUntil = Time.time + Random.Range(0.05f, 0.1f + 0.4f * t);
      if (buzz && !buzz.isPlaying) buzz.Play();
    }

    float noise = Mathf.PerlinNoise(Time.time * 20f, 0f);
    _light.intensity = _baseIntensity * Mathf.Lerp(1f, noise, t * 0.8f);
  }
}