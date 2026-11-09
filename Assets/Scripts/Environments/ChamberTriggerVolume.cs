using UnityEngine;

/// <summary>
/// Simple trigger volume that notifies the BlackChamberSequence controller when the player enters.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChamberTriggerVolume : MonoBehaviour {
  public enum TriggerType {
    EnterChamber,
    ApproachRedDoor
  }

  [SerializeField] private BlackChamberSequence sequenceController;
  [SerializeField] private TriggerType triggerType = TriggerType.EnterChamber;

  private bool _triggered = false;

  private void OnTriggerEnter(Collider other) {
    if (_triggered || !other.CompareTag("Player")) return;

    if (sequenceController == null) {
      sequenceController = GetComponentInParent<BlackChamberSequence>();
    }

    if (sequenceController == null) return;

    _triggered = true;

    if (triggerType == TriggerType.EnterChamber) {
      sequenceController.OnPlayerEnterChamber();
    } else if (triggerType == TriggerType.ApproachRedDoor) {
      sequenceController.OnPlayerApproachRedDoor();
    }
  }
}
