using UnityEngine;

public class PassGate : MonoBehaviour {
  public Collider mapCollider;

  void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Player")) {
      // Ignores collision between Player and Map Collider
      Physics.IgnoreCollision(other, mapCollider, true);
    }
  }

  void OnTriggerExit(Collider other) {
    if (other.CompareTag("Player")) {
      // Re-enables collision when leaving
      Physics.IgnoreCollision(other, mapCollider, false);
    }
  }
}
