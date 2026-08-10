using UnityEngine;

[CreateAssetMenu(fileName = "New Furniture", menuName = "Inventory/Furniture")]
public class FurnitureItem : ScriptableObject
{
  public string itemName;
  public GameObject prefab; // Useful if you want to drop it later
  public Sprite icon;       // Useful for your UI later
  [Min(0f)] public float weight;
  [Min(0)] public int saleValue;
}
