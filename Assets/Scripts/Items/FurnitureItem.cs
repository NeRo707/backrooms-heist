using UnityEngine;

[CreateAssetMenu(fileName = "New Furniture", menuName = "Inventory/Furniture")]
public class FurnitureItem : ScriptableObject
{
  [SerializeField] private string itemName;
  public string ItemName { get => itemName; set => itemName = value; }
  [SerializeField] private GameObject prefab;
  public GameObject Prefab { get => prefab; set => prefab = value; } // Useful if you want to drop it later
  [SerializeField] private Sprite icon;       // Useful for your UI later
  [Min(0f)] [SerializeField] private float weight;
  public float Weight { get => weight; set => weight = value; }
  [Min(0)] [SerializeField] private int saleValue;
  public int SaleValue { get => saleValue; set => saleValue = value; }
}
