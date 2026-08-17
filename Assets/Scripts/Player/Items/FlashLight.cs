using UnityEngine;

public enum LightMode {
  Off,
  Flashlight,
  NightVision
}

public class FlashLight : MonoBehaviour {
  [Header("Current Mode")]
  [SerializeField] private LightMode currentMode = LightMode.Off;
  [SerializeField] private Light _light;

  [Header("Flashlight Beam")]
  [SerializeField] private Color flashlightColor = new Color(1f, 0.95f, 0.85f);
  [SerializeField] private float flashlightIntensity = 2.5f;
  [SerializeField] private float flashlightRange = 25f;
  [SerializeField] private float flashlightSpotAngle = 55f;

  [Header("Night Vision Beam")]
  [SerializeField] private Color nvgColor = new Color(0.15f, 1f, 0.25f);
  [SerializeField] private float nvgIntensity = 4.0f;
  [SerializeField] private float nvgRange = 35f;
  [SerializeField] private float nvgSpotAngle = 95f;

  [Header("Audio")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip switchSound;

  private void Start() {
    if (_light == null) {
      _light = GetComponent<Light>();
    }
    ApplyMode();
  }

  public void Switch() {
    CycleMode();
  }

  public void CycleMode() {
    switch (currentMode) {
      case LightMode.Off:
        currentMode = LightMode.Flashlight;
        break;
      case LightMode.Flashlight:
        currentMode = LightMode.NightVision;
        break;
      case LightMode.NightVision:
        currentMode = LightMode.Off;
        break;
    }

    if (audioSource != null && switchSound != null) {
      audioSource.PlayOneShot(switchSound);
    }

    ApplyMode();
  }

  private void ApplyMode() {
    if (_light == null) return;

    switch (currentMode) {
      case LightMode.Off:
        _light.enabled = false;
        break;

      case LightMode.Flashlight:
        _light.enabled = true;
        _light.color = flashlightColor;
        _light.intensity = flashlightIntensity;
        _light.range = flashlightRange;
        _light.spotAngle = flashlightSpotAngle;
        break;

      case LightMode.NightVision:
        _light.enabled = true;
        _light.color = nvgColor;
        _light.intensity = nvgIntensity;
        _light.range = nvgRange;
        _light.spotAngle = nvgSpotAngle;
        break;
    }
  }
}

