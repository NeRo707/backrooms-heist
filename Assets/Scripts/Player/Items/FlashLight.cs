using UnityEngine;

public class FlashLight : MonoBehaviour {
  [SerializeField] private bool isOn = false;
  [SerializeField] private Light _light;

  public void Switch() {
    isOn = !isOn;
    _light.enabled = isOn;
  }
}
