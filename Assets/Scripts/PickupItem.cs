using UnityEngine;

public class PickupItem : MonoBehaviour {
  public FurnitureItem itemData;

  public void Collect() {
    // Try to add the item to the inventory
    bool wasPickedUp = Inventory.Instance.AddItem(itemData);

    if (wasPickedUp) {
      // Play a sound effect here if you want
      Destroy(gameObject); // Remove the furniture from the level
    }
  }
}
