using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
  [SerializeField] private FlashLight flashLight;

  private void Update() {
    bool togglePressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
    if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) {
      togglePressed = true;
    }

    if (togglePressed && flashLight != null) {
      flashLight.Switch();
    }
  }
}

