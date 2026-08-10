using UnityEngine;

public class Player : MonoBehaviour {
  [SerializeField] private FlashLight flashLight;

  private void Update() {
    if (Input.GetKeyDown(KeyCode.F)) {
      flashLight.Switch();
    }
  }
}
