using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlashlightFlicker : MonoBehaviour {
  public Transform angel;
  public float startDistance = 15f;
  public float minDistance = 3f;
  public AudioSource buzz; // optional

  private Light _light;
  private float _baseIntensity;
  private float _offUntil;

  private void Start() {
    _light = GetComponent<Light>();
    _baseIntensity = _light.intensity;
  }

  private void Update() {
    float d = Vector3.Distance(angel.position, transform.position);
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
      if (buzz) buzz.Play();
    }

    float noise = Mathf.PerlinNoise(Time.time * 20f, 0f);
    _light.intensity = _baseIntensity * Mathf.Lerp(1f, noise, t * 0.8f);
  }
}
